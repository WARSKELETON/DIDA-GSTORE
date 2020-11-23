using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GstoreServer.Models
{
    // PartitionId
    // Master S1
    // S1 
    // S2
    // S3 
    class Partition
    {
        private int CurrentWriteId { get; set; }

        // (WriteId, ObjectId), Update(PartitionId, ObjectId, Value)
        private readonly Dictionary<Tuple<int, string>, Update> Updates;

        public string Id { get; }
        public string Master { get; set; }
        public List<string> Servers { get; }

        public HashSet<string> FailedServer { get; }

        public ManualResetEvent Mre { get; }


        public Partition(string id, string master, List<string> servers)
        {
            this.CurrentWriteId = 0;
            this.Id = id;
            this.Master = master;
            this.Updates = new Dictionary<Tuple<int, string>, Update>();
            this.Servers = new List<string>(servers);
            this.FailedServer = new HashSet<string>();
            this.Mre = new ManualResetEvent(false);
        }

        public bool checkHigherExistence(int writeId, string objectId)
        {
            for (int i = writeId; i <= CurrentWriteId; i++)
            {
                if (Updates.Keys.FirstOrDefault(key => key.Item1 == i && key.Item2 == objectId) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public void AddUpdate(int writeId, string partitionId, string objectId, string value)
        {
            if (writeId > CurrentWriteId)
            {
                CurrentWriteId = writeId;
            }
            Updates[new Tuple<int, string>(writeId, objectId)] = new Update(writeId, partitionId, objectId, value);
        }

        public override string ToString()
        {
            string activeServers = "Active Servers:";
            string failedServers = "Failed Servers:";
            foreach (string server in Servers)
            {
                activeServers += " " + server;
            }

            foreach (string failedServer in FailedServer)
            {
                failedServers += " " + failedServer;
            }

            return $"Partition {Id} has {Servers.Count} active servers and {FailedServer.Count} failed servers\r\nMaster: {Master}\r\n{activeServers}\r\n{failedServers}\r\n";
        }

        public int getWriteId()
        {
            return CurrentWriteId;
        }

        public int IncrementWriteId()
        {
            CurrentWriteId++;
            return CurrentWriteId;
        }
    }
}
