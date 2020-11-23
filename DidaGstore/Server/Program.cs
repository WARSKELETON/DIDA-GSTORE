using GstoreServer.Parsers;
using System;

namespace GstoreServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Im server " + args[0] + " ready to serve.");
            ConfigParser config_parser = new ConfigParser(args[0]); // args[0] is my server id, used to check my partition
            GstoreServer server = new GstoreServer(args[0], args[1], Int32.Parse(args[2]), Int32.Parse(args[3]), config_parser.Partitions, config_parser.Servers, config_parser.ReplicationFactor);
            server.Run();
            Console.ReadKey();
            Console.ReadKey();
        }
    }
}
