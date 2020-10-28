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
            Dictionary<string, string> servers = new Dictionary<string, string>();
            servers.Add("1", "http://localhost:1001");
            servers.Add("2", "http://localhost:1002");
            GstoreClient client = new GstoreClient(servers);

            Parser parser = new Parser(client);
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
