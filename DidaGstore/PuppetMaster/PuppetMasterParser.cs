using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PuppetMaster
{
    class PuppetMasterParser
    {
        PuppetMaster PuppetMaster;

        public PuppetMasterParser(PuppetMaster puppet) {
            PuppetMaster = puppet;
        }
        public void parse(string cmd)
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
                    break;
                case "Crash":
                    PuppetMaster.Crash(args[1]);
                    break;
                case "Freeze":
                    break;
                case "Unfreeze":
                    break;
                case "Wait":
                    break;
            }
        }

        public void generateConfig(string fileString)
        {
            Regex serverRegex = new Regex(@"Server\s(?<server_id>[^\s]+)\s(?<server_url>\w+:\/\/[^\/]+?:\d+)", RegexOptions.None, TimeSpan.FromMilliseconds(150));
            Regex partitionRegex = new Regex(@"Partition\s(?<r_factor>\d)\s(?<partition_name>[^\s]+)(?<servers_ids>(\s[^\s]+)+)", RegexOptions.None, TimeSpan.FromMilliseconds(150));
            MatchCollection serverMatches = serverRegex.Matches(fileString);
            MatchCollection partitionMatches = partitionRegex.Matches(fileString);

            string clientConfigPath = Regex.Replace(AppDomain.CurrentDomain.BaseDirectory, "PuppetMaster", "Client") + "\\system-config.txt";
            string serverConfigPath = Regex.Replace(AppDomain.CurrentDomain.BaseDirectory, "PuppetMaster", "Server") + "\\system-config.txt";
            FileStream clientConfigStream = new FileStream(clientConfigPath, FileMode.Create);
            FileStream serverConfigStream = new FileStream(serverConfigPath, FileMode.Create);
            using StreamWriter clientConfigWriter = new StreamWriter(clientConfigStream, Encoding.UTF8),
                serverConfigWriter = new StreamWriter(serverConfigStream, Encoding.UTF8);

            foreach (Match match in serverMatches)
            {
                string serverLine = $"{match.Groups["server_id"].Value} {match.Groups["server_url"].Value}";
                clientConfigWriter.WriteLine(serverLine);
                serverConfigWriter.WriteLine(serverLine);
            }

            foreach (Match match in partitionMatches)
            {
                string partitionLine = match.Value;
                string[] serversIds = match.Groups["servers_ids"].Value.Trim().Split(" ");
                int randomServerIndex = new Random().Next(serversIds.Length);
                string masterLine = $"Master {match.Groups["partition_name"]} {serversIds[randomServerIndex]}";
                clientConfigWriter.WriteLine(partitionLine);
                serverConfigWriter.WriteLine(partitionLine);
                clientConfigWriter.WriteLine(masterLine);
                serverConfigWriter.WriteLine(masterLine);
            }
        }
    }
}
