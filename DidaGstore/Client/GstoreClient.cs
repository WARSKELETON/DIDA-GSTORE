using Grpc.Net.Client;
using GstoreClient.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GstoreClient
{
    class GstoreClient
    {
        // ServerId, ServerConnection<GstoreService>
        private Dictionary<string, GstoreService.GstoreServiceClient> servers = new Dictionary<string, GstoreService.GstoreServiceClient>();
        // PartitionId, Partition
        private Dictionary<string, Partition> partitions = new Dictionary<string, Partition>();

        private string attachedServer;

        public GstoreClient(Dictionary<string, string> serversList, List<Partition> partitionsList)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            foreach (KeyValuePair<string, string> item in serversList)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(item.Value);
                servers.Add(item.Key, new GstoreService.GstoreServiceClient(channel));
            }
            foreach (Partition partition in partitionsList)
            {
                partitions.Add(partition.Id, partition);
            }
            foreach (var server in servers)
            {
                Console.WriteLine("Existe server: " + server.Key);
            }
            foreach (var partition in partitions)
            {
                Console.WriteLine("Existe partition: " + partition.Key);
            }
        }

        public string Read(string partitionId, string objectId, string serverId)
        {
            /*if (attachedServer.Item1 != partitionId || (!ServerExists(partitionId, serverId) && serverId == "-1"))
            {
                Attach(partitionId);
            }*/
            Console.WriteLine(serverId);

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
            List<string> availableServers = getAvailableServers(partitionId);
            int index = (new Random()).Next(0, availableServers.Count);

            attachedServer = availableServers[index];
        }
        private string getMasterId(string partitionId)
        {
            Partition partition = null;
            if (!partitions.TryGetValue(partitionId, out partition))
            {
                throw new Exception("Partition doesn't exist.");
            }
            if (partition.Master != null)
            {
                return partition.Master;
            }
            throw new Exception("There is no master for the given partition.");
        }

        private List<string> getAvailableServers(string partitionId)
        {
            Partition partition = null;
            if (!partitions.TryGetValue(partitionId, out partition))
            {
                return null;
            }
            return partition.Servers;
        }

        private bool ServerExists(string partitionId, string serverId)
        {
            foreach (string server in getAvailableServers(partitionId))
            {
                if (server == serverId)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
