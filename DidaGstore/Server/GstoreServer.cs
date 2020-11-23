using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using Grpc.Core;
using Grpc.Net.Client;
using GstoreServer.Models;

namespace GstoreServer
{
    class GstoreServer
    {
        private const int PING_TIMEOUT = 3000;

        private readonly string Id;
        private readonly string Url;
        private readonly int MaxFaults;
        private readonly int MinDelay;
        private readonly int MaxDelay;
        private readonly IGstoreRepository GstoreRepository;
        private readonly Dictionary<string, GstoreReplicaService.GstoreReplicaServiceClient> Replicas;
        private readonly Dictionary<string, Partition> Partitions;
        private readonly Dictionary<string, string> AllServerIdsServerUrls;
        private readonly Object freezed = new Object();

        static ManualResetEvent mreFreezed = new ManualResetEvent(false);

        public GstoreServer(string serverId, string url, int minDelay, int maxDelay, List<Partition> partitions, Dictionary<string, string> allServerIdsServerUrls)
        {
            Id = serverId;
            Url = url;
            MinDelay = minDelay;
            MaxDelay = maxDelay;
            GstoreRepository = new GstoreRepository();
            Replicas = new Dictionary<string, GstoreReplicaService.GstoreReplicaServiceClient>();
            Partitions = new Dictionary<string, Partition>();
            AllServerIdsServerUrls = allServerIdsServerUrls;

            MaxFaults = allServerIdsServerUrls.Count / 2;

            foreach (Partition partition in partitions)
            {
                Partitions.Add(partition.Id, partition);
            } // CONTAINS THE MASTER (MYSELF) AND THE SERVERS LIST

            InitiatePingLoop();
        }

        public ReadReply Read(string partitionId, string objectId)
        {
            Console.WriteLine("Received read: " + partitionId + ", " + objectId);

            lock (freezed) {}

            Console.WriteLine("Going to read: " + partitionId + ", " + objectId);

            DelayIncomingMessage();

            string value;
            Tuple<string, string> key = GstoreRepository.GetKey(partitionId, objectId);

            if (key == null)
            {
                return new ReadReply
                {
                    Value = "N/A"
                };
            }

            lock (key)
            {
                value = GstoreRepository.Read(partitionId, objectId);
            }
            Console.WriteLine("Finish Reading.");
            return new ReadReply
            {
                Value = value
            };
        }

        public WriteReply Write(string partitionId, string objectId, string value)
        {
            Console.WriteLine("Received write: " + partitionId + ", " + objectId);
            lock (freezed) { }
            Console.WriteLine("Going to write: " + partitionId + ", " + objectId + ", " + value);

            DelayIncomingMessage();

            Tuple<string, string> key = GstoreRepository.GetKey(partitionId, objectId);
            if (key == null)
            {
                GstoreRepository.Write(partitionId, objectId, null);
                key = GstoreRepository.GetKey(partitionId, objectId);
            }
            lock (key)
            {
                lock (Partitions[partitionId]) {

                    foreach (string serverId in Partitions[partitionId].Servers) {
                        if (serverId == Id) continue;
                        try
                        {
                            Replicas[serverId].Lock(new LockRequest {
                                PartitionId = partitionId,
                                ObjectId = objectId
                            });
                        } catch (Exception ex)
                        {
                            continue;
                        }
                    }

                    Console.WriteLine("Locked Servers");

                    foreach (string serverId in Partitions[partitionId].Servers) {
                        if (serverId == Id) continue;
                        try
                        {
                            Replicas[serverId].Update(new UpdateRequest
                            {
                                PartitionId = partitionId,
                                ObjectId = objectId,
                                Value = value
                            });
                        }
                        catch (Exception ex)
                        {
                            continue;
                        }
                    }
                }
                Console.WriteLine("Updated Servers");

                GstoreRepository.Write(partitionId, objectId, value);
            }
            Console.WriteLine("Finished Writing.");
            return new WriteReply
            {
                Ok = true
            };
        }

        public WriteReply AdvancedWrite(string partitionId, string objectId, string value)
        {
            lock (freezed) { }
            DelayIncomingMessage();
            Tuple<string, string> key = GstoreRepository.GetKey(partitionId, objectId);
            if (key == null)
            {
                GstoreRepository.Write(partitionId, objectId, null);
                key = GstoreRepository.GetKey(partitionId, objectId);
            }
            
            int attempts = 0;
            UpdateRequest updateRequest = new UpdateRequest
            {
                PartitionId = partitionId,
                ObjectId = objectId,
                Value = value,
                WriteId = Partitions[partitionId].CurrentWriteId
            };
            Partitions[partitionId].AddUpdate(Partitions[partitionId].CurrentWriteId, partitionId, objectId, value);
            GstoreRepository.Write(partitionId, objectId, value);
            lock (Partitions[partitionId])
            {
                foreach (string serverId in Partitions[partitionId].Servers)
                {
                    if (serverId == Id) continue;
                    try
                    {
                        if (attempts >= MaxFaults)
                        {
                            // Se nao funcionar, criar thread
                            Console.WriteLine("Updating server async: " + serverId);
                            // the code that you want to measure comes here
                            Replicas[serverId].UpdateAsync(updateRequest);

                        }
                        else
                        {
                            // TODO: Se o server ficar freezed isto fica bloqueado 
                            Replicas[serverId].Update(updateRequest);
                            attempts++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                }
            }
            Partitions[partitionId].IncrementWriteId();
            return new WriteReply { Ok = true };
        }

        public LockReply Lock(string partitionId, string objectId)
        {
            lock (freezed) { }

            DelayIncomingMessage();

            Tuple<string, string> key = GstoreRepository.GetKey(partitionId, objectId);

            if (key == null)
            {
                GstoreRepository.Write(partitionId, objectId, null);
                key = GstoreRepository.GetKey(partitionId, objectId);
            }

            Thread thread = new Thread(() =>
            {
                lock (key)
                {
                    Partitions[partitionId].Mre.WaitOne();
                }
            });

            thread.Start();

            return new LockReply
            {
                Ack = true
            };
        }

        public UpdateReply AdvancedUpdate(string partitionId, string objectId, string value, int writeId)
        {
            Console.WriteLine("Going to update: " + partitionId + " " + objectId + " " + value + " write:" + writeId);
            lock (freezed) { }

            DelayIncomingMessage();

            if(writeId > Partitions[partitionId].CurrentWriteId)
            {
                Partitions[partitionId].AddUpdate(writeId, partitionId, objectId, value);
                GstoreRepository.Write(partitionId, objectId, value);
            }

            return new UpdateReply
            {
                Ack = true
            };
        }

        public UpdateReply Update(string partitionId, string objectId, string value)
        {
            lock (freezed) { }

            DelayIncomingMessage();

            GstoreRepository.Write(partitionId, objectId, value);

            Partitions[partitionId].Mre.Set();
            Partitions[partitionId].Mre.Reset();

            return new UpdateReply
            {
                Ack = true
            };
        }

        public PingReply Ping()
        {
            lock (freezed) { }

            return new PingReply
            {
                Ok = true
            };
        }

        public PingReplicaReply PingReplica()
        {
            // Console.WriteLine("Received Ping.");

            lock (freezed) { }

            return new PingReplicaReply
            {
                Ok = true
            };
        }

        public StatusReply PrintStatus() {
            DelayIncomingMessage();

            foreach (Partition partition in Partitions.Values)
            {
                Console.WriteLine(partition.ToString());
            }

            return new StatusReply { 
                Ok = true
            };
        }

        public CrashReply Crash() {
            Console.WriteLine("Going to Crash.");
            DelayIncomingMessage();
            Process.GetCurrentProcess().Kill();
            Console.WriteLine("Finished Crashing, failed.");
            return new CrashReply {
                Ok = true
            };
        }

        public ListServerReply ListServer()
        {
            ListServerReply reply = new ListServerReply();
            foreach (StoredObject obj in GstoreRepository.GetAllObjects())
            {
                obj.Master = Partitions[obj.PartitionId].Master == Id;
                obj.ServerId = Id;
                reply.Objects.Add(obj);
            }
            return reply;
        }

        public FreezeReply Freeze() {
            DelayIncomingMessage();

            Console.WriteLine("Freezing " + Id);

            Thread thread = new Thread(() =>
            {
                lock (freezed)
                {
                    mreFreezed.WaitOne();
                }
            });

            thread.Start();

            return new FreezeReply {
                Ok = true
            };
        }

        public UnfreezeReply Unfreeze() {
            DelayIncomingMessage();

            Console.WriteLine("Unfreezing " + Id);

            mreFreezed.Set();
            mreFreezed.Reset();

            return new UnfreezeReply {
                Ok = true
            };
        }

        private void AddReplicaChannels()
        {
            foreach (KeyValuePair<string, Partition> item in Partitions)
            {
                foreach (string replicaId in item.Value.Servers)
                {
                    if (replicaId == Id || Replicas.ContainsKey(replicaId))
                    {
                        continue;
                    }
                    GrpcChannel channel = GrpcChannel.ForAddress(AllServerIdsServerUrls[replicaId]);
                    GstoreReplicaService.GstoreReplicaServiceClient replicaClient = new GstoreReplicaService.GstoreReplicaServiceClient(channel);
                    Replicas.Add(replicaId, replicaClient);
                }
            }
        }

        private void DelayIncomingMessage()
        {
            if (MinDelay != 0 || MaxDelay != 0)
            {
                Random rand = new Random();
                Thread.Sleep(rand.Next(MinDelay, MaxDelay + 1)); // + 1 because its exclusive
            }
        }

        public void Run()
        {
            Regex r = new Regex(@"^(?<proto>\w+):\/\/[^\/]+?:(?<port>\d+)?", RegexOptions.None, TimeSpan.FromMilliseconds(150));
            Match m = r.Match(Url);
            int port = Int32.Parse(m.Groups["port"].Value);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            AddReplicaChannels();

            Server server = new Server
            {
                Services = { GstoreService.BindService(new GstoreServiceImpl(this)), GstoreReplicaService.BindService(new GstoreReplicaServiceImpl(this)),
                             PuppetMasterService.BindService(new PuppetMasterServiceImpl(this))},
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();
            while (true) ;
        }

        private void InitiatePingLoop()
        {
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(PING_TIMEOUT);

                    List<string> failedServers = new List<string>();
                    foreach (KeyValuePair<string, GstoreReplicaService.GstoreReplicaServiceClient> replica in Replicas)
                    {
                        try
                        {
                            // Console.WriteLine("Initiate Ping Request to: " + replica.Key);
                            PingReplicaReply reply = replica.Value.PingReplica(new PingReplicaRequest());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Detected Failure on server: " + replica.Key);

                            foreach (Partition partition in Partitions.Values)
                            {
                                lock(Partitions[partition.Id]) {
                                    if (partition.Servers.Contains(replica.Key)) {
                                        if (replica.Key == partition.Master) {
                                            partition.Mre.Set();
                                            partition.Mre.Reset();
                                            partition.Master = partition.Servers[(partition.Servers.IndexOf(partition.Master) + 1) % partition.Servers.Count];
                                        }
                                        partition.Servers.Remove(replica.Key);
                                        partition.FailedServer.Add(replica.Key);
                                    }
                                }
                            }
                            failedServers.Add(replica.Key);
                        }
                    }
                    foreach (string server in failedServers)
                    {
                        Replicas.Remove(server);
                    }
                }
            });
            thread.Start();
        }
    }
}
