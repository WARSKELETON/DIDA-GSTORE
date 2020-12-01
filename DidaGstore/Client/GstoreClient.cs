using Grpc.Net.Client;
using GstoreClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GstoreClient
{
    class GstoreClient
    {
        // ServerId, ServerConnection<GstoreService>
        private readonly Dictionary<string, GstoreService.GstoreServiceClient> Servers = new Dictionary<string, GstoreService.GstoreServiceClient>();
        // PartitionId, Partition
        private readonly Dictionary<string, Partition> Partitions = new Dictionary<string, Partition>();

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
                    if (Partitions[partitionId].LastAttached != null && ServerExists(partitionId, Partitions[partitionId].LastAttached))
                        AttachToServer(Partitions[partitionId].LastAttached, partitionId);
                    else
                        AttachToRandomServer(partitionId);
                }
                else if (serverId != "-1" && attachedServer == "-1")
                {
                    AttachToServer(serverId, partitionId);
                }

                reply = Servers[attachedServer].Read(new ReadRequest() {
                    PartitionId = partitionId,
                    ObjectId = objectId
                });

            } catch(Exception ex) {
                RemoveServer(attachedServer);
                AttachToRandomServer(partitionId);
                return Read(partitionId, objectId, "-1");
            }

            if (reply.Value.Equals("N/A") && !serverId.Equals("-1"))
            {
                AttachToServer(serverId, partitionId);
                return Read(partitionId, objectId, "-1");
            }

            return reply.Value;
        }

        public bool Write(string partitionId, string objectId, string value)
        {
            string masterId = GetMasterId(partitionId);
            AttachToServer(masterId, partitionId);
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
                Console.WriteLine(ex.Message);
                RemoveServer(attachedServer);
                Console.WriteLine("Remove on write");
                return Write(partitionId, objectId, value);
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

        public void ListServer(string serverId)
        {
            try
            {
                ListServerReply reply = Servers[serverId].ListServer(new ListServerRequest());
                foreach (StoredObject obj in reply.Objects.ToList())
                {
                    string frmt = $"Object {obj.ObjectId} with the value {obj.Value} of partition {obj.PartitionId} is stored in server {obj.ServerId}";
                    if (obj.Master)
                    {
                        frmt += " which is the master of the partition";
                    }
                    Console.WriteLine(frmt);
                }
            }
            catch (Exception ex) {
                RemoveServer(serverId);
            }
        }

        public void ListGlobal()
        {
            foreach (KeyValuePair<string, GstoreService.GstoreServiceClient> server in Servers)
            {
                ListServer(server.Key);
            }
        }

        public void Wait(int milis)
        {
            Console.WriteLine("Starting to sleep...");
            Thread.Sleep(milis);
        }

        private void AttachToRandomServer(string partitionId)
        {
            List<string> availableServers = GetAvailableServers(partitionId);
            int index = (new Random()).Next(0, availableServers.Count);

            attachedServer = availableServers[index];
            Partitions[partitionId].LastAttached = attachedServer;
        }

        private void AttachToServer(string serverId, string partitionId)
        {
            attachedServer = serverId;
            Partitions[partitionId].LastAttached = attachedServer;   
        }

        private string GetMasterId(string partitionId)
        {
            if (!Partitions.TryGetValue(partitionId, out Partition partition))
            {
                throw new Exception("Partition doesn't exist.");
            }
            if (partition.Master != null)
            {
                return partition.Master;
            }
            throw new Exception("There is no master for the given partition.");
        }

        private List<string> GetAvailableServers(string partitionId)
        {
            if (!Partitions.TryGetValue(partitionId, out Partition partition))
            {
                return null;
            }
            return partition.Servers;
        }

        private bool ServerExists(string partitionId, string serverId)
        {
            foreach (string server in GetAvailableServers(partitionId))
            {
                if (server == serverId)
                {
                    return true;
                }
            }
            return false;
        }
        
        private void RemoveServer(string serverId) {
            Servers.Remove(serverId);
            Console.WriteLine("Removed " + serverId);
            foreach(Partition partition in Partitions.Values) {
                if (partition.Servers.Contains(serverId)) {
                    if (serverId == partition.Master) {
                        partition.Master = partition.Servers[(partition.Servers.IndexOf(partition.Master) + 1) % partition.Servers.Count];
                        Console.WriteLine("New Master: " + partition.Master);
                    }
                    partition.Servers.Remove(serverId);
                    partition.FailedServer.Add(serverId);
                }
            }
        }
    }
}
