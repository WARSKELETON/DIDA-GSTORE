using Grpc.Core;
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
        private Dictionary<string, GstoreService.GstoreServiceClient> Servers = new Dictionary<string, GstoreService.GstoreServiceClient>();
        // PartitionId, Partition
        private Dictionary<string, Partition> Partitions = new Dictionary<string, Partition>();

        private string attachedServer = "-1";

        public GstoreClient(Dictionary<string, string> serversList, List<Partition> partitionsList)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            foreach (KeyValuePair<string, string> item in serversList)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(item.Value);
                Servers.Add(item.Key, new GstoreService.GstoreServiceClient(channel));
            }
            foreach (Partition partition in partitionsList)
            {
                Partitions.Add(partition.Id, partition);
            }
            foreach (var server in Servers)
            {
                Console.WriteLine("Existe server: " + server.Key);
            }
            foreach (var partition in Partitions)
            {
                Console.WriteLine("Existe partition: " + partition.Key);
            }
        }

        public string Read(string partitionId, string objectId, string serverId)
        {
            ReadReply reply;

            try {

                if (!ServerExists(partitionId, attachedServer) && serverId == "-1")
                {
                    AttachToRandomServer(partitionId);
                }
                else if (serverId != "-1" && attachedServer == "-1")
                {
                    AttachToServer(serverId);
                }

                reply = Servers[attachedServer].Read(new ReadRequest() {
                    PartitionId = partitionId,
                    ObjectId = objectId
                });

            } catch(Exception ex) {
                RemoveServer();
                Console.WriteLine(Servers.Count);
                Console.WriteLine(ex.Message);
                AttachToRandomServer(partitionId);
                return Read(partitionId, objectId, "-1");
            }

            if (reply.Value.Equals("N/A") && !serverId.Equals("-1"))
            {
                AttachToServer(serverId);
                return Read(partitionId, objectId, "-1");
            }

            return reply.Value;
        }

        public bool Write(string partitionId, string objectId, string value)
        {
            string masterId = getMasterId(partitionId);
            AttachToServer(masterId);
            try { 
                WriteReply reply = Servers[masterId].Write(new WriteRequest()
                {
                    PartitionId = partitionId,
                    ObjectId = objectId,
                    Value = value
                });

                if (!reply.Ok) {
                    Console.WriteLine("outra vez");
                    Write(partitionId, objectId, value);
                }
                return reply.Ok;
            } catch(Exception ex)
            {
                RemoveServer();
                Console.WriteLine("Remove on write");
                Console.WriteLine(ex.Message);
                Write(partitionId, objectId, value);
                return false;
            }
        }

        public StatusReply PrintStatus()
        {
            foreach (Partition partition in Partitions.Values)
            {
                Console.WriteLine(partition.ToString());
            }

            return new StatusReply { Ok = true };
        }

        public List<Object> ListServer(string serverId)
        {
            ListServerReply reply = Servers[serverId].ListServer(new ListServerRequest());
            return reply.Objects.ToList();
        }

        public List<Object> ListGlobal()
        {
            List<Object> globalList = new List<Object>();
            foreach (KeyValuePair<string, GstoreService.GstoreServiceClient> server in Servers)
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
            Console.WriteLine("Starting to sleep...");
            Thread.Sleep(milis);
        }

        private void AttachToRandomServer(string partitionId)
        {
            List<string> availableServers = getAvailableServers(partitionId);
            int index = (new Random()).Next(0, availableServers.Count);

            attachedServer = availableServers[index];
        }

        private void AttachToServer(string serverId)
        {
            attachedServer = serverId;
        }

        private string getMasterId(string partitionId)
        {
            Partition partition = null;
            if (!Partitions.TryGetValue(partitionId, out partition))
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
            if (!Partitions.TryGetValue(partitionId, out partition))
            {
                return null;
            }
            // Verificar quais servers estao em baixo
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

        private void RemoveServer() {
            Servers.Remove(attachedServer);
            Console.WriteLine("Removed " + attachedServer);
            foreach(Partition partition in Partitions.Values) {
                if (partition.Servers.Contains(attachedServer)) {
                    if (attachedServer == partition.Master) {
                        partition.Master = partition.Servers[(partition.Servers.IndexOf(partition.Master) + 1) % partition.Servers.Count];
                        Console.WriteLine("New Master: " + partition.Master);
                    }
                    partition.Servers.Remove(attachedServer);
                    partition.FailedServer.Add(attachedServer);
                }
            }
        }
    }
}
