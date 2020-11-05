using GstoreClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GstoreClient.Parsers
{
    class ConfigParser
    {
        public List<Partition> Partitions { get; }
        public Dictionary<string, string> Servers { get; }

        public ConfigParser()
        {
            Partitions = new List<Partition>();
            Servers = new Dictionary<string, string>();
            try
            {
                string system_config_path = Regex.Replace(Path.GetFullPath("./system-config.txt"), "PuppetMaster", "Client");
                StreamReader fileConfig = new StreamReader(system_config_path);
                string line;
                while ((line = fileConfig.ReadLine()) != null)
                {
                    string[] args = line.Split(" ");
                    switch (args[0])
                    {
                        case "Partition":
                            List<string> partitionServers = new List<string>();
                            for (int i = 3; i < args.Length; i++)
                            {
                                partitionServers.Add(args[i]);
                            }
                            Partitions.Add(new Partition(args[2], args[3], partitionServers));
                            break;
                        default:
                            Servers.Add(args[0], args[1]);
                            break;
                    }
                }
                fileConfig.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when opening system-config file. " + ex.StackTrace);
            }


        }
    }
}
