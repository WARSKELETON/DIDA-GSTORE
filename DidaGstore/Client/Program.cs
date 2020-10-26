using System;
using System.IO;
using System.Text.RegularExpressions;

namespace GstoreClient
{
    class Program
    {
        static void Main(string[] args)
        {
            GstoreClient client = new GstoreClient(null);

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
