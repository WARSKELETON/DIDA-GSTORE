using System;
using System.Collections.Generic;
using System.Text;

namespace GstoreClient
{
    class Parser
    {
        private GstoreClient Client = null;
        private int Iterations = 0;
        private bool Repeat = false;
        List<string> Cmds = null;

        public Parser(GstoreClient client)
        {
            Client = client;
            Cmds = new List<string>();
        }

        public void parse(string cmd)
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
                    string value = Client.Read(args[1], args[2], args[3]);
                    Console.WriteLine($"Client Read {args[1]} {args[2]} {args[3]}");
                    Console.WriteLine(value);
                    break;
                case "write":
                    bool ok = Client.Write(args[1], args[2], args[3]);
                    Console.WriteLine($"Client Write {args[1]} {args[2]} {args[3]}");
                    Console.WriteLine(ok);
                    break;
                case "listServer":
                    //Client.ListServer(args[1]);
                    Console.WriteLine("Client ListServer");
                    break;
                case "listGlobal":
                    //Client.ListGlobal();
                    Console.WriteLine("Client ListGlobal");
                    break;
                case "wait":
                    //Client.Wait(Int32.Parse(args[1]));
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
                            parse(command.Replace("$i", $"{i + 1}"));
                        }
                    }
                    Cmds.Clear();
                    break;
            }
        }
    }
}
