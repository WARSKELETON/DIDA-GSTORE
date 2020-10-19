﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Grpc.Core;
using Grpc.Net.Client;

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
        private ArrayList ReplicasIds;

        static ManualResetEvent mre = new ManualResetEvent(false);

        public GstoreServer(string serverId, string url, int minDelay, int maxDelay)
        {
            Id = serverId;
            Url = url;
            MinDelay = minDelay;
            MaxDelay = maxDelay;
            GstoreRepository = new GstoreRepository();
            ReadRequests = new ArrayList();
        }

        public ReadReply Read(string partitionId, string objectId)
        {
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
            Tuple<string, string> key = GstoreRepository.GetKey(partitionId, objectId);

            lock (key)
            {
                foreach (string serverId in ReplicasIds)
                {

                }
            }

            return new WriteReply
            {
                Ok = true
            };
        }

        public LockReply Lock(string partitionId, string objectId)
        {
            Tuple<string, string> key = GstoreRepository.GetKey(partitionId, objectId);

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
            GstoreRepository.Write(partitionId, objectId, value);

            mre.Set();
            mre.Reset();

            return new UpdateReply
            {
                Ack = true
            };
        }

        public void Run()
        {
            int port = 1001;
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress($"http://localhost:{port}");
            GstoreReplicaService.GstoreReplicaServiceClient client = new GstoreReplicaService.GstoreReplicaServiceClient(channel);

            Server server = new Server
            {
                Services = { GstoreService.BindService(new GstoreServiceImpl(this)), GstoreReplicaService.BindService(new GstoreReplicaServiceImpl(this)) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();
        }

    }
}
