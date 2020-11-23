using System;
using System.Collections.Generic;
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
        public int CurrentWriteId { get; set; }

        // WriteId, PartitionId, ObjectId, Value
        public List<Update> Updates { get; }

        public string Id { get; }
        public string Master { get; set; }
        public List<string> Servers { get; }

        public List<string> FailedServer { get; }

        public ManualResetEvent Mre { get; }


        public Partition(string id, string master, List<string> servers)
        {
            this.CurrentWriteId = 0;
            this.Id = id;
            this.Master = master;
            this.Updates = new List<Update>();
            this.Servers = new List<string>(servers);
            this.FailedServer = new List<string>();
            this.Mre = new ManualResetEvent(false);
        }

        public void AddUpdate(int writeId, string partitionId, string objectId, string value)
        {
            CurrentWriteId = writeId;
            this.Updates.Add(new Update(writeId, partitionId, objectId, value));
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

        public void IncrementWriteId()
        {
            this.CurrentWriteId++;
        }
    }
}
