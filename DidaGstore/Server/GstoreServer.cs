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
        private readonly HashSet<string> FailedServers;
        static private readonly Dictionary<string, Partition> Partitions = new Dictionary<string, Partition>();
        private readonly Dictionary<string, string> AllServerIdsServerUrls;
        private readonly Object freezed = new Object();

        static ManualResetEvent mreFreezed = new ManualResetEvent(false);

        public GstoreServer(string serverId, string url, int minDelay, int maxDelay, List<Partition> partitions, Dictionary<string, string> allServerIdsServerUrls, int replicationFactor)
        {
            Id = serverId;
            Url = url;
            MinDelay = minDelay;
            MaxDelay = maxDelay;
            GstoreRepository = new GstoreRepository();
            Replicas = new Dictionary<string, GstoreReplicaService.GstoreReplicaServiceClient>();
            FailedServers = new HashSet<string>();
            AllServerIdsServerUrls = allServerIdsServerUrls;
            MaxFaults = (int)(Math.Ceiling(replicationFactor / 2.0) - 1.0);

            PrintStatus();

            foreach (Partition partition in partitions)
            {
                Partitions.Add(partition.Id, partition);
            } // CONTAINS THE MASTER (MYSELF) AND THE SERVERS LIST

            AdvancedInitiatePingLoop();
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

        public WriteReply AdvancedWrite(string partitionId, string objectId, string value)
        {
            Console.WriteLine("Received advanced write: " + partitionId + ", " + objectId);
            lock (freezed) { }
            Console.WriteLine("Going to advanced write: " + partitionId + ", " + objectId + ", " + value);
            DelayIncomingMessage();

            Tuple<string, string> key = GstoreRepository.GetKey(partitionId, objectId);
            if (key == null)
            {
                GstoreRepository.Write(partitionId, objectId, null);
                key = GstoreRepository.GetKey(partitionId, objectId);
            }

            int writeId;
            lock (Partitions[partitionId])
            {
                writeId = Partitions[partitionId].IncrementWriteId();
            }
            UpdateRequest updateRequest = new UpdateRequest
            {
                PartitionId = partitionId,
                ObjectId = objectId,
                Value = value,
                WriteId = writeId,
                InSync = false
            };
            Partitions[partitionId].AddUpdate(writeId, partitionId, objectId, value);
            GstoreRepository.Write(partitionId, objectId, value);
            Console.WriteLine("Finished write: " + partitionId + ", " + objectId + ", " + value);

            int attempts = 0;
            foreach (string serverId in Partitions[partitionId].Servers)
            {
                if (serverId == Id || FailedServers.Contains(serverId) || Partitions[partitionId].Servers.IndexOf(serverId) < Partitions[partitionId].Servers.IndexOf(Id)) continue;
                try
                {
                    if (attempts >= MaxFaults)
                    {
                        Console.WriteLine("Updating server async: " + serverId);
                        if (writeId <= 5 || writeId >= 10)
                        {
                            Replicas[serverId].UpdateAsync(updateRequest);
                        }

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
                    addFailedServer(partitionId, serverId);
                    continue;
                }
            }
            return new WriteReply { Ok = true };
        }


        public UpdateReply AdvancedUpdate(string partitionId, string objectId, string value, int writeId, bool inSync)
        {
            lock (freezed) { }

            DelayIncomingMessage();

            Console.WriteLine("Going to update: " + partitionId + " " + objectId + " " + value + " write:" + writeId);

            int writeIdInPartition;
            if (!inSync)
            {
                lock (Partitions[partitionId])
                {
                    writeIdInPartition = Partitions[partitionId].getWriteId();
                }
            }
            else
            {
                writeIdInPartition = Partitions[partitionId].getWriteId();
            }

            if (writeId > writeIdInPartition)
            {
                GstoreRepository.Write(partitionId, objectId, value);
            }
            else
            {
                if (!Partitions[partitionId].checkHigherExistence(writeId, objectId))
                {
                    GstoreRepository.Write(partitionId, objectId, value);
                }
            }

            Partitions[partitionId].AddUpdate(writeId, partitionId, objectId, value);
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
            DelayIncomingMessage();

            return new PingReplicaReply
            {
                Ok = true
            };
        }

        public StatusReply PrintStatus() {
            lock (freezed) { }

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
            lock (freezed) { }

            DelayIncomingMessage();

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

        private void addFailedServer(string failedServerPartitionId, string failedServerId)
        {
            FailedServers.Add(failedServerId);
            Partitions[failedServerPartitionId].FailedServers.Add(failedServerId);
        }

        private void AdvancedInitiatePingLoop()
        {
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(PING_TIMEOUT);

                    foreach (KeyValuePair<string, GstoreReplicaService.GstoreReplicaServiceClient> replica in Replicas)
                    {
                        try
                        {
                            // Console.WriteLine("Initiate Ping Request to: " + replica.Key);
                            if (FailedServers.Contains(replica.Key)) continue;
                            PingReplicaReply reply = replica.Value.PingReplica(new PingReplicaRequest());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Detected Failure on server: " + replica.Key);

                            foreach (Partition partition in Partitions.Values)
                            {
                                if (partition.Servers.Contains(replica.Key))
                                {
                                    lock (Partitions[partition.Id])
                                    {
                                        if (replica.Key == partition.Master)
                                        {
                                            partition.Mre.Set();
                                            partition.Mre.Reset();
                                            partition.Master = partition.Servers[(partition.Servers.IndexOf(partition.Master) + 1) % partition.Servers.Count];
                                            // I'm the new master, initiating sync
                                            if (partition.Master == Id)
                                            {
                                                SyncPartition(partition.Id);
                                            }
                                        }
                                    }
                                    addFailedServer(partition.Id, replica.Key);
                                }
                            }
                        }
                    }
                }
            });
            thread.Start();
        }

        private void SyncPartition(string partitionId)
        {
            lock (freezed) { }

            foreach (string serverId in Partitions[partitionId].Servers)
            {
                if (serverId == Id || FailedServers.Contains(serverId)) continue;
                try
                {
                    SyncLockReply reply = Replicas[serverId].SyncLock(new SyncLockRequest{ PartitionId = partitionId });
                    Console.WriteLine("Received sync reply in partition " + partitionId + " by server " + serverId + " with write id " + reply.WriteId);
                    for (int i = reply.WriteId + 1; i < Partitions[partitionId].getWriteId(); i++)
                    {
                        Update update = Partitions[partitionId].getUpdate(i);
                        Replicas[serverId].Update(new UpdateRequest
                        {
                            PartitionId = partitionId,
                            ObjectId = update == null ? "" : update.ObjectId,
                            Value = update == null ? "" :update.Value,
                            WriteId = i,
                            InSync = true
                        });
                    }
                    Replicas[serverId].FinishedSync(new FinishedSyncRequest { PartitionId = partitionId });
                    Console.WriteLine("Finished sync in partition " + partitionId + " by server " + serverId);
                }
                catch (Exception ex)
                {
                    addFailedServer(partitionId, serverId);
                    continue;
                }
            }
        }

        public SyncLockReply SyncLock(string partitionId)
        {
            lock (freezed) { }

            DelayIncomingMessage();

            Console.WriteLine("Initiating sync in partition " + partitionId);

            Thread thread = new Thread(() =>
            {
                lock (Partitions[partitionId])
                {
                    Partitions[partitionId].Mre.WaitOne();
                }
            });

            thread.Start();

            return new SyncLockReply
            {
                WriteId = Partitions[partitionId].getOldWriteId()
            };
        }

        public FinishedSyncReply FinishedSync(string partitionId)
        {
            lock (freezed) { }

            DelayIncomingMessage();

            Console.WriteLine("Finished sync in partition " + partitionId);

            Partitions[partitionId].Mre.Set();
            Partitions[partitionId].Mre.Reset();

            return new FinishedSyncReply
            {
                Ok = true
            };
        }
    }
}
