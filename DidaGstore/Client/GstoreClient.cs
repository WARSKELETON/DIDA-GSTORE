using Grpc.Net.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GstoreClient
{
    class GstoreClient
    {
        private Dictionary<string, GstoreService.GstoreServiceClient> servers = new Dictionary<string, GstoreService.GstoreServiceClient>();
        // PartitionId
            // S1 (master)
            // S2
            // S3 
        private Dictionary<string, List<Tuple<string, bool>>> partitionsSet = new Dictionary<string, List<Tuple<string, bool>>>();

        private Tuple<string, string> attachedServer;

        public GstoreClient(Dictionary<string, string> serversList)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            foreach (KeyValuePair<string, string> item in serversList)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(item.Value);
                servers.Add(item.Key, new GstoreService.GstoreServiceClient(channel));
            }
            // Retirar esta
            List<Tuple<string, bool>> partitionServers = new List<Tuple<string, bool>>();
            List<Tuple<string, bool>> partition2Servers = new List<Tuple<string, bool>>();
            partitionServers.Add(new Tuple<string, bool>("1", true));
            partition2Servers.Add(new Tuple<string, bool>("2", true));
            partitionsSet.Add("1", partitionServers);
            partitionsSet.Add("2", partition2Servers);
        }

        public string Read(string partitionId, string objectId, string serverId)
        {
            /*if (attachedServer.Item1 != partitionId || (!ServerExists(partitionId, serverId) && serverId == "-1"))
            {
                Attach(partitionId);
            }*/

            ReadReply reply = servers[serverId].Read(new ReadRequest() {  // Change server id to attached server id
                PartitionId = partitionId,
                ObjectId = objectId
            });
            
            if (reply.Value.Equals("N/A") && !serverId.Equals("-1"))
            {
                // TODO: Attach para o serverID
                return Read(partitionId, objectId, "-1");
                // Perguntar se é o enduser a fazer o attach ou se é automatico
            }
            return reply.Value;
        }

        public bool Write(string partitionId, string objectId, string value)
        {
            string masterId = getMasterId(partitionId);
            WriteReply reply = servers[masterId].Write(new WriteRequest()
            {  
                PartitionId = partitionId,
                ObjectId = objectId,
                Value = value
            });

            return reply.Ok;
        }

        public List<Object> ListServer(string serverId)
        {
            ListServerReply reply = servers[serverId].ListServer(new ListServerRequest());
            return reply.Objects.ToList();
        }

        public List<Object> ListGlobal()
        {
            List<Object> globalList = new List<Object>();
            foreach (KeyValuePair<string, GstoreService.GstoreServiceClient> server in servers)
            {
                ListServerReply reply = server.Value.ListServer(new ListServerRequest());
                foreach(Object obj in reply.Objects)
                {
                    globalList.Add(obj);
                }
            }
            return globalList;
        }

        public void Wait(int milis)
        {
            Thread.Sleep(milis);
        }

        private void Attach(string partitionId)
        {
            List<Tuple<string, bool>> availableServers = getAvailableServers(partitionId);
            int index = (new Random()).Next(0, availableServers.Count);
            attachedServer = new Tuple<string, string>(partitionId, availableServers[index].Item1);
        }
        private string getMasterId(string partitionId)
        {
            foreach (Tuple<string, bool> server in getAvailableServers(partitionId))
            {
                if (server.Item2)
                {
                    return server.Item1;
                }
            }
            throw new Exception("Não há master.");
        }

        private List<Tuple<string, bool>> getAvailableServers(string partitionId)
        {
            List<Tuple<string, bool>> availableServers = null;
            if (!partitionsSet.TryGetValue(partitionId, out availableServers))
            {
                return null;
            }
            return availableServers;
        }

        private bool ServerExists(string partitionId, string serverId)
        {
            foreach (Tuple<string, bool> server in getAvailableServers(partitionId))
            {
                if (server.Item1 == serverId)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
