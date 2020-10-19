using System;
using System.IO;

namespace GstoreClient
{
    class Program
    {
        static void Main(string[] args)
        {
            GstoreClient client = new GstoreClient(null);

            Parser parser = new Parser(client);
            if(args.Length == 1 && File.Exists(args[0])) {
                StreamReader file = new StreamReader(args[0]);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    parser.parse(line);
                }
                file.Close();
            }
        }
    }
}
