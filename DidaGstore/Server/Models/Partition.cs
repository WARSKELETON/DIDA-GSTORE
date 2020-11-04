using System.Collections.Generic;

namespace GstoreServer.Models
{
    // PartitionId
    // Master S1
    // S1 
    // S2
    // S3 
    class Partition
    {
        public string Id { get; }
        public string Master { get; set; }
        public List<string> Servers { get; }

        public List<string> FailedServer { get; }

        public Partition(string id, string master, List<string> servers)
        {
            this.Id = id;
            this.Master = master;
            this.Servers = new List<string>(servers);
            this.FailedServer = new List<string>();
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
    }
}
