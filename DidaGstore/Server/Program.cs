using GstoreServer.Parsers;
using System;

namespace GstoreServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigParser config_parser = new ConfigParser(args[0]); // args[0] is my server id, used to check my partition
            GstoreServer server = new GstoreServer(args[0], args[1], Int32.Parse(args[2]), Int32.Parse(args[3]), config_parser.Partition);
            server.Run();
        }
    }
}
