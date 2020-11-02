using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Grpc.Core;
using Grpc.Net.Client;
using GstoreServer.Models;

namespace GstoreServer
{
    class GstoreServer
    {
        private int PING_TIMEOUT = 3000;

        private string Id;
        private string Url;
        private int MinDelay;
        private int MaxDelay;
        private IGstoreRepository GstoreRepository;
        private ArrayList ReadRequests;
        private Dictionary<string, GstoreReplicaService.GstoreReplicaServiceClient> Replicas;
        private Dictionary<string, Partition> MyPartitions;
        private Dictionary<string, string> AllServerIdsServerUrls;
        private bool freezed;

        static ManualResetEvent mre = new ManualResetEvent(false);

        public GstoreServer(string serverId, string url, int minDelay, int maxDelay, List<Partition> partitions, Dictionary<string, string> allServerIdsServerUrls)
        {
            Id = serverId;
            Url = url;
            MinDelay = minDelay;
            MaxDelay = maxDelay;
            GstoreRepository = new GstoreRepository();
            ReadRequests = new ArrayList();
            Replicas = new Dictionary<string, GstoreReplicaService.GstoreReplicaServiceClient>();
            MyPartitions = new Dictionary<string, Partition>();
            AllServerIdsServerUrls = allServerIdsServerUrls;

            foreach (Partition partition in partitions)
            {
                MyPartitions.Add(partition.Id, partition);
            } // CONTAINS THE MASTER (MYSELF) AND THE SERVERS LIST

            InitiatePingLoop();
        }

        public ReadReply Read(string partitionId, string objectId)
        {
            Console.WriteLine("Going to read: " + partitionId + ", " + objectId);

            // Delay for read and write or every message
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
            Console.WriteLine("Going to WRITE: " + partitionId + ", " + objectId + ", " + value);

            DelayIncomingMessage();

            Tuple<string, string> key = GstoreRepository.GetKey(partitionId, objectId);
            if (key == null)
            {
                GstoreRepository.Write(partitionId, objectId, null);
                key = GstoreRepository.GetKey(partitionId, objectId);
            }
            lock (key)
            {
                lock (MyPartitions[partitionId]) {

                    foreach (string serverId in MyPartitions[partitionId].Servers) {
                        if (serverId == Id) continue;
                        // Verificar se deu
                        Replicas[serverId].Lock(new LockRequest {
                            PartitionId = partitionId,
                            ObjectId = objectId
                        });
                    }

                    Console.WriteLine("Locked Servers");

                    foreach (string serverId in MyPartitions[partitionId].Servers) {
                        // Verificar se deu
                        if (serverId == Id) continue;
                        Replicas[serverId].Update(new UpdateRequest {
                            PartitionId = partitionId,
                            ObjectId = objectId,
                            Value = value
                        });
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

        public LockReply Lock(string partitionId, string objectId)
        {
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
                    mre.WaitOne();
                }
            });

            thread.Start();

            return new LockReply
            {
                Ack = true
            };
        }

        public UpdateReply Update(string partitionId, string objectId, string value)
        {
            DelayIncomingMessage();

            GstoreRepository.Write(partitionId, objectId, value);

            mre.Set();
            mre.Reset();

            return new UpdateReply
            {
                Ack = true
            };
        }

        public PingReply Ping()
        {
            return new PingReply
            {
                Ok = true
            };
        }

        public PingReplicaReply PingReplica()
        {
            Console.WriteLine("Received Ping.");
            return new PingReplicaReply
            {
                Ok = true
            };
        }

        public StatusReply PrintStatus() {

            DelayIncomingMessage();

            // TODO
            //Console.WriteLine();

            return new StatusReply {
                Ok = true
            };
        }

        public CrashReply Crash() {
            Console.WriteLine("Going to Crash.");
            DelayIncomingMessage();

            // Mandar para as outras o aviso de que a replica crashou?
            Process.GetCurrentProcess().Kill();
            Console.WriteLine("Finished Crashing, failed.");
            return new CrashReply {
                Ok = true
            };
        }

        public FreezeReply Freeze() {
            DelayIncomingMessage();

            // TODO: adicionar flag freezed aos metodos
            freezed = true;

            return new FreezeReply {
                Ok = true
            };
        }

        public UnfreezeReply Unfreeze() {
            DelayIncomingMessage();

            // TODO: adicionar flag freezed aos metodos
            freezed = false;

            return new UnfreezeReply {
                Ok = true
            };
        }

        private void AddReplicaChannels()
        {
            foreach (KeyValuePair<string, Partition> item in MyPartitions)
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
                    // TODO ADD LOCKS
                    Thread.Sleep(PING_TIMEOUT);

                    List<string> failedServers = new List<string>();
                    foreach (KeyValuePair<string, GstoreReplicaService.GstoreReplicaServiceClient> replica in Replicas)
                    {
                        try
                        {
                            Console.WriteLine("Initiate Ping to: " + replica.Key);
                            PingReplicaReply reply = replica.Value.PingReplica(new PingReplicaRequest());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Detected Failure on server: " + replica.Key);

                            foreach (Partition partition in MyPartitions.Values)
                            {
                                lock(MyPartitions[partition.Id]) {
                                    if (partition.Servers.Contains(replica.Key)) {
                                        if (replica.Key == partition.Master) {
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
