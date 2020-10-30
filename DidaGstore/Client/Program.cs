using GstoreClient.Models;
using GstoreClient.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GstoreClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // Read the configuration file generated on the server
            ConfigParser config_parser = new ConfigParser();

            GstoreClient client = new GstoreClient(config_parser.Servers, config_parser.Partitions);

            CommandParser parser = new CommandParser(client);
            string path = Regex.Replace(Path.GetFullPath(args[2]), "PuppetMaster", "Client");

            if (args.Length == 3 && File.Exists(path)) {
                StreamReader file = new StreamReader(path);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    parser.parse(line);
                }
                file.Close();
            }
            
            while(true) {

            }
        }
    }
}
