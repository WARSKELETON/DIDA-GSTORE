using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Grpc.Core;

namespace GstoreServer
{
    class GstoreServer
    {
        private string Id;
        private string Url;
        private int MinDelay;
        private int MaxDelay;
        private IGstoreRepository GstoreRepository;
        private ArrayList Requests;

        public delegate string ReadDelegate(string partitionId, string objectId);

        public GstoreServer(string serverId, string url, int minDelay, int maxDelay)
        {
            Id = serverId;
            Url = url;
            MinDelay = minDelay;
            MaxDelay = maxDelay;
            GstoreRepository = new GstoreRepository();
            Requests = new ArrayList();
        }

        public ReadReply Read(string partitionId, string objectId)
        {
            ReadDelegate readCall = new ReadDelegate(GstoreRepository.Read);
            lock (GstoreRepository)
            {
                Thread thread = new Thread(() =>
                {
                    IAsyncResult asyncResult = readCall.BeginInvoke(partitionId, objectId, null, null);
                    string value = readCall.EndInvoke(asyncResult);
                });
            }
            return new ReadReply
            {
                Value = value
            };
        }

        public void Run()
        {
            /* Server server = new Server
            {
                Services = { GstoreServerService.BindService(new ServerService()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();*/
        }

    }
}
