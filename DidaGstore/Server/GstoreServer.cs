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
        private string Id;
        private string Url;
        private int MinDelay;
        private int MaxDelay;
        private IGstoreRepository GstoreRepository;
        private ArrayList ReadRequests;
        private Partition MyPartition;
        private Dictionary<string, GstoreReplicaService.GstoreReplicaServiceClient> Replicas;
        private Dictionary<string, string> AllServerIdsServerUrls;
        private bool freezed;

        static ManualResetEvent mre = new ManualResetEvent(false);

        public GstoreServer(string serverId, string url, int minDelay, int maxDelay, Partition partition, Dictionary<string, string> allServerIdsServerUrls)
        {
            Id = serverId;
            Url = url;
            MinDelay = minDelay;
            MaxDelay = maxDelay;
            GstoreRepository = new GstoreRepository();
            ReadRequests = new ArrayList();
            MyPartition = partition; // CONTAINS THE MASTER (MYSELF) AND THE SERVERS LIST
            Replicas = new Dictionary<string, GstoreReplicaService.GstoreReplicaServiceClient>();
            AllServerIdsServerUrls = allServerIdsServerUrls;
        }

        public ReadReply Read(string partitionId, string objectId)
        {
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

            return new ReadReply
            {
                Value = value
            };
        }

        public WriteReply Write(string partitionId, string objectId, string value)
        {
            DelayIncomingMessage();

            Tuple<string, string> key = GstoreRepository.GetKey(partitionId, objectId);

            if (key == null)
            {
                GstoreRepository.Write(partitionId, objectId, null);
                key = GstoreRepository.GetKey(partitionId, objectId);
            }

            lock (key)
            {
                foreach (string serverId in MyPartition.Servers)
                {
                    // Verificar se deu
                    Replicas[serverId].Lock(new LockRequest
                    {
                        PartitionId = partitionId,
                        ObjectId = objectId
                    });
                }

                foreach (string serverId in MyPartition.Servers)
                {
                    // Verificar se deu
                    Replicas[serverId].Update(new UpdateRequest
                    {
                        PartitionId = partitionId,
                        ObjectId = objectId,
                        Value = value
                    });
                }

                GstoreRepository.Write(partitionId, objectId, value);
            }

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

        public StatusReply PrintStatus() {

            DelayIncomingMessage();

            // TODO
            //Console.WriteLine();

            return new StatusReply {
                Ok = true
            };
        }

        public CrashReply Crash() {
            DelayIncomingMessage();

            // Mandar para as outras o aviso de que a replica crashou?
            Process.GetCurrentProcess().Kill();
            
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
            foreach (string replicaId in MyPartition.Servers)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(AllServerIdsServerUrls[replicaId]);
                GstoreReplicaService.GstoreReplicaServiceClient replicaClient = new GstoreReplicaService.GstoreReplicaServiceClient(channel);
                Replicas.Add(replicaId, replicaClient);
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

            if (MyPartition != null)
            {
                AddReplicaChannels();
            }

            Server server = new Server
            {
                Services = { GstoreService.BindService(new GstoreServiceImpl(this)), GstoreReplicaService.BindService(new GstoreReplicaServiceImpl(this)),
                             PuppetMasterService.BindService(new PuppetMasterServiceImpl(this))},
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();
            while (true) ;
        }
    }
}
