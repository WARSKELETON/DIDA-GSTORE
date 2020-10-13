using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;

namespace GstoreServer
{
    class GstoreServer
    {
        private string Id;
        private string Url;
        private int MinDelay;
        private int MaxDelay;
        
        public GstoreServer(string serverId, string url, int minDelay, int maxDelay)
        {
            Id = serverId;
            Url = url;
            MinDelay = minDelay;
            MaxDelay = maxDelay;
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
