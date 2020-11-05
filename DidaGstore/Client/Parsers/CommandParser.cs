using System;
using System.Collections.Generic;
using System.Text;

namespace GstoreClient
{
    class CommandParser
    {
        private readonly GstoreClient Client = null;
        private int Iterations = 0;
        private bool Repeat = false;
        List<string> Cmds = null;

        public CommandParser(GstoreClient client)
        {
            Client = client;
            Cmds = new List<string>();
        }

        public void Parse(string cmd)
        {
            string[] args = cmd.Split(" ");
            if (Repeat && !args[0].Equals("end-repeat"))
            {
                Cmds.Add(cmd);
                Console.WriteLine("Add command: " + Cmds.Count);
                return;
            }

            switch (args[0])
            {
                case "read":
                    if (args.Length != 4)
                    {
                        Console.WriteLine("Invalid number of arguments: Read partitionId objectId serverId");
                        return;
                    }
                    Console.WriteLine($"Client Read {args[1]} {args[2]} {args[3]}");
                    string value = Client.Read(args[1], args[2], args[3]);
                    Console.WriteLine(value);
                    break;
                case "write":
                    Console.WriteLine($"Client Write {args[1]} {args[2]} {cmd.Split("\"")[1]}");
                    bool ok = Client.Write(args[1], args[2], cmd.Split("\"")[1]);
                    Console.WriteLine(ok);
                    break;
                case "listServer":
                    Console.WriteLine("Client ListServer");
                    Client.ListServer(args[1]);
                    break;
                case "listGlobal":
                    Console.WriteLine("Client ListGlobal");
                    Client.ListGlobal();
                    break;
                case "wait":
                    Client.Wait(Int32.Parse(args[1]));
                    Console.WriteLine("Client Wait");
                    break;
                case "begin-repeat":
                    Console.WriteLine("begin-repeat");
                    Iterations = Int32.Parse(args[1]);
                    Repeat = true;
                    break;
                case "end-repeat":
                    Repeat = false;
                    Console.WriteLine("end-repeat");
                    for (int i = 0; i < Iterations; i++)
                    {
                        foreach(string command in Cmds)
                        {
                            Parse(command.Replace("$i", $"{i + 1}"));
                        }
                    }
                    Cmds.Clear();
                    break;
            }
        }
    }
}
