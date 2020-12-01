using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PuppetMaster
{
    class PuppetMasterParser
    {
        PuppetMaster PuppetMaster;
        const string SYSTEM_CONFIG_NAME = "system-config.txt";
        enum NodeType
        {
            Client,
            Server
        };

        public PuppetMasterParser(PuppetMaster puppet) {
            PuppetMaster = puppet;
        }
        public void Parse(string cmd)
        {
            string[] args = cmd.Split(" ");

            // TODO: Verificacoes de args

            switch (args[0])
            {
                case "ReplicationFactor":
                    break;
                case "Server":
                    PuppetMaster.CreateServer(args[1], args[2], args[3], args[4]);
                    break;
                case "Partition":
                    break;
                case "Client":
                    PuppetMaster.CreateClient(args[1], args[2], args[3]);
                    break;
                case "Status":
                    PuppetMaster.Status();
                    break;
                case "Crash":
                    PuppetMaster.Crash(args[1]);
                    break;
                case "Freeze":
                    PuppetMaster.Freeze(args[1]);
                    break;
                case "Unfreeze":
                    PuppetMaster.Unfreeze(args[1]);
                    break;
                case "Wait":
                    PuppetMaster.Wait(Int32.Parse(args[1]));
                    break;
            }
        }

        private void GenerateConfigFile(NodeType nodeType, MatchCollection serverMatches, MatchCollection partitionMatches, Match replicationFactorMatch)
        {
            string configPath = Regex.Replace(AppDomain.CurrentDomain.BaseDirectory, "PuppetMaster", nodeType.ToString()) + $"\\{SYSTEM_CONFIG_NAME}";
            FileStream configStream = new FileStream(configPath, FileMode.Create);
            using StreamWriter configWriter = new StreamWriter(configStream, Encoding.UTF8);

            configWriter.WriteLine($"ReplicationFactor {replicationFactorMatch.Groups["replication_factor"].Value}");

            foreach (Match match in serverMatches)
            {
                string serverLine = $"{match.Groups["server_id"].Value} {match.Groups["server_url"].Value}";
                configWriter.WriteLine(serverLine);
            }

            foreach (Match match in partitionMatches)
            {
                string partitionLine = match.Value;
                configWriter.WriteLine(partitionLine);
            }
        }

        public void GenerateSystemConfig(string fileString)
        {
            Regex serverRegex = new Regex(@"Server\s(?<server_id>[^\s]+)\s(?<server_url>\w+:\/\/[^\/]+?:\d+)", RegexOptions.None, TimeSpan.FromMilliseconds(150));
            Regex partitionRegex = new Regex(@"Partition\s(?<r_factor>[\d]+)\s(?<partition_name>[^\s]+)(?<servers_ids>(\s[^\s]+)+)", RegexOptions.None, TimeSpan.FromMilliseconds(150));
            Regex replicationFactorRegex = new Regex(@"ReplicationFactor\s(?<replication_factor>[\d]+)", RegexOptions.None, TimeSpan.FromMilliseconds(150));
            MatchCollection serverMatches = serverRegex.Matches(fileString);
            MatchCollection partitionMatches = partitionRegex.Matches(fileString);
            Match replicationFactorMatch = replicationFactorRegex.Match(fileString);

            GenerateConfigFile(NodeType.Server, serverMatches, partitionMatches, replicationFactorMatch);
            GenerateConfigFile(NodeType.Client, serverMatches, partitionMatches, replicationFactorMatch);
        }
    }
}
