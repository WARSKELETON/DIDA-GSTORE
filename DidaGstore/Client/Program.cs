using Grpc.Core;
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
            // Read the system configuration file
            ConfigParser config_parser = new ConfigParser();
            GstoreClient client = new GstoreClient(config_parser.Servers, config_parser.Partitions);

            CommandParser parser = new CommandParser(client);
            string path = Regex.Replace(Path.GetFullPath(args[2]), "PuppetMaster", "Client");

            string Url = args[1];
            Regex r = new Regex(@"^(?<proto>\w+):\/\/[^\/]+?:(?<port>\d+)?", RegexOptions.None, TimeSpan.FromMilliseconds(150));
            Match m = r.Match(Url);
            int port = Int32.Parse(m.Groups["port"].Value);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            Server server = new Server
            {
                Services = { PuppetMasterService.BindService(new PuppetMasterServiceImpl(client))},
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();

            if (args.Length == 3 && File.Exists(path)) {
                StreamReader file = new StreamReader(path);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    parser.Parse(line);
                }
                file.Close();
            }

            while (true) ;
        }
    }
}
