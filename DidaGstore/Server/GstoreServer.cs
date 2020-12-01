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

            int writeId;
            lock (Partitions[partitionId])
            {
                writeId = Partitions[partitionId].IncrementWriteId();
                Partitions[partitionId].AddUpdate(writeId, partitionId, objectId, value);
                GstoreRepository.Write(partitionId, objectId, value);
            }
            Console.WriteLine("Finished write: " + partitionId + ", " + objectId + ", " + value + ", " + writeId);

            UpdateRequest updateRequest = new UpdateRequest
            {
                PartitionId = partitionId,
                ObjectId = objectId,
                Value = value,
                WriteId = writeId,
                InSync = false
            };

            int quorum = 0;
            object quorumObj = new Object();
            foreach (string serverId in Partitions[partitionId].Servers)
            {
                if (serverId == Id || FailedServers.Contains(serverId) || Partitions[partitionId].Servers.IndexOf(serverId) < Partitions[partitionId].Servers.IndexOf(Id)) continue;
                try
                {
                    Replicas[serverId].UpdateAsync(updateRequest).GetAwaiter().OnCompleted(() => { 
                        lock (quorumObj)
                        {
                            quorum++;
                        }
                    });
                }
                catch (Exception ex)
                {
                    addFailedServer(partitionId, serverId);
                    continue;
                }
            }
            while (quorum < MaxFaults) ;
            Console.WriteLine(quorum);
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
                    WriteUpdate(partitionId, objectId, value, writeId, writeIdInPartition);
                }
            }
            else
            {
                writeIdInPartition = Partitions[partitionId].getWriteId();
                WriteUpdate(partitionId, objectId, value, writeId, writeIdInPartition);
            }

            Partitions[partitionId].AddUpdate(writeId, partitionId, objectId, value);
            return new UpdateReply
            {
                Ack = true
            };
        }

        private void WriteUpdate(string partitionId, string objectId, string value, int writeId, int writeIdInPartition)
        {

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
                                    if (replica.Key == partition.Master)
                                    {
                                        partition.Mre.Set();
                                        partition.Mre.Reset();
                                        partition.Master = partition.Servers[(partition.Servers.IndexOf(partition.Master) + 1) % partition.Servers.Count];
                                        // I'm the new master, initiating sync
                                        if (partition.Master == Id)
                                        {
                                            lock (Partitions[partition.Id])
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

            Dictionary<string, int> serverIdsOldWriteIds = new Dictionary<string, int>();

            serverIdsOldWriteIds.Add(Id, Partitions[partitionId].getOldWriteId());
            foreach (string serverId in Partitions[partitionId].Servers)
            {
                if (serverId == Id || FailedServers.Contains(serverId)) continue;
                try
                {
                    Console.WriteLine("Sync Locking: " + serverId);
                    SyncLockReply reply = Replicas[serverId].SyncLock(new SyncLockRequest { PartitionId = partitionId });
                    serverIdsOldWriteIds.Add(serverId, reply.WriteId);
                }
                catch (Exception ex)
                {
                    addFailedServer(partitionId, serverId);
                    continue;
                }
            }

            foreach (string serverId in Partitions[partitionId].Servers)
            {
                if (FailedServers.Contains(serverId)) continue;
                try
                {
                    foreach (string serverIdToSync in Partitions[partitionId].Servers)
                    {
                        if (serverIdToSync == serverId || FailedServers.Contains(serverIdToSync)) continue;
                        if (serverIdsOldWriteIds[serverId] < serverIdsOldWriteIds[serverIdToSync])
                        {
                            Console.WriteLine($"{serverId} with {serverIdsOldWriteIds[serverId]} initiating sync with {serverIdToSync} with {serverIdsOldWriteIds[serverIdToSync]}");
                            InitiateSyncReply reply;
                            if (serverIdToSync == Id)
                            {
                                reply = InitiateSync(partitionId, serverId, serverIdsOldWriteIds[serverId]);
                            }
                            else
                            {
                                reply = Replicas[serverIdToSync].InitiateSync(new InitiateSyncRequest { 
                                    PartitionId = partitionId,
                                    ServerId = serverId,
                                    WriteId = serverIdsOldWriteIds[serverId]
                                });
                            }
                            serverIdsOldWriteIds[serverId] = reply.NewWriteId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    addFailedServer(partitionId, serverId);
                    continue;
                }
            }

            foreach (string serverId in Partitions[partitionId].Servers)
            {
                if (serverId == Id || FailedServers.Contains(serverId)) continue;
                try
                {
                    Console.WriteLine("Sync Finishing: " + serverId);
                    Replicas[serverId].FinishedSync(new FinishedSyncRequest { PartitionId = partitionId });
                }
                catch (Exception ex)
                {
                    addFailedServer(partitionId, serverId);
                    continue;
                }
            }
        }

        public InitiateSyncReply InitiateSync(string partitionId, string serverId, int writeId)
        {
            lock (freezed) { }

            Console.WriteLine("Received Sync Request by: " + serverId);

            if (FailedServers.Contains(serverId)) {
                return new InitiateSyncReply
                {
                    Ok = false,
                    NewWriteId = writeId
                };
            }

            Console.WriteLine("Received Sync Request by: " + serverId);

            try
            {
                for (int i = writeId + 1; i < Partitions[partitionId].getWriteId(); i++)
                {
                    Update update = Partitions[partitionId].getUpdate(i);
                    Replicas[serverId].Update(new UpdateRequest
                    {
                        PartitionId = partitionId,
                        ObjectId = update == null ? "" : update.ObjectId,
                        Value = update == null ? "" : update.Value,
                        WriteId = i,
                        InSync = true
                    });
                }
            }
            catch (Exception ex)
            {
                addFailedServer(partitionId, serverId);
            }

            return new InitiateSyncReply
            {
                Ok = true,
                NewWriteId = Partitions[partitionId].getWriteId()
            };
        }

        public SyncLockReply SyncLock(string partitionId)
        {
            lock (freezed) { }

            DelayIncomingMessage();

            Console.WriteLine("Sync locked in partition " + partitionId);

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
