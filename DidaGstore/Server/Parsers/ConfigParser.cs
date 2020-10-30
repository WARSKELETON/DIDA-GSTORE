using GstoreServer.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GstoreServer.Parsers
{
    class ConfigParser
    {
        public Partition Partition { get; }
        public Dictionary<string, string> Servers { get; }

        public ConfigParser(string server_id)
        {
            Partition = null;
            Servers = new Dictionary<string, string>(); 
            try
            {
                List<string> partitionServers = new List<string>();
                string system_config_path = Regex.Replace(Path.GetFullPath("./system-config.txt"), "PuppetMaster", "Server");
                StreamReader fileConfig = new StreamReader(system_config_path);
                string line;
                while ((line = fileConfig.ReadLine()) != null)
                {
                    string[] args = line.Split(" ");
                    switch (args[0])
                    {
                        case "Partition":
                            // SUGESTAO: MASTER SER O PRIMEIRO DA LISTA NA PARTITION FORNECIDA
                            if (args[3] == server_id) // If im the master then this is the partition i need to save
                            {
                                for (int i = 4; i < args.Length; i++) // Start after the master
                                {
                                    partitionServers.Add(args[i]);
                                }
                                Partition = new Partition(args[2], server_id, partitionServers);
                            }
                            break;
                        case "Master":
                            // UNUSED
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
                Console.WriteLine("Error when opening system-config file. " + ex.Message);
            } 
        }
    }
}
