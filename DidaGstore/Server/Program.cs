using System;

namespace GstoreServer
{
    class Program
    {
        static void Main(string[] args)
        {
            GstoreServer server = new GstoreServer(args[0], args[1], Int32.Parse(args[2]), Int32.Parse(args[3]));
            server.Run();
        }
    }
}
