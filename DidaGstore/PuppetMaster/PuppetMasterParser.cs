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

        public PuppetMasterParser() {
            PuppetMaster = new PuppetMaster();
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
            Regex r = new Regex(@"Server\s(?<server_id>\d+)\s(?<server_url>\w+:\/\/[^\/]+?:\d+)", RegexOptions.None, TimeSpan.FromMilliseconds(150));
            MatchCollection matches = r.Matches(fileString);

            FileStream stream = new FileStream("system-config.txt", FileMode.OpenOrCreate);
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                foreach (Match match in matches)
                {
                    writer.WriteLine(match.Groups["server_id"].Value + " " + match.Groups["server_url"].Value);
                }
            }
            stream.Close();
        }
    }
}
