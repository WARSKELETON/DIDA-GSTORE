using System;
using System.Collections.Generic;
using System.Text;

namespace PuppetMaster
{
    class PuppetMasterParser
    {
        PuppetMaster PuppetMaster;

        public PuppetMasterParser() {
            PuppetMaster = new PuppetMaster();
        }
        public void parse(string cmd)
        {
            string[] args = cmd.Split(" ");

            // TODO: Verificacoes de args

            switch (args[0])
            {
                case "ReplicationFactor":
                    break;
                case "Server":
                    PuppetMaster.CreateServer(args[1], args[2], args[3], args[4]);
                    break;
                case "Partition":
                    break;
                case "Client":
                    PuppetMaster.CreateClient(args[1], args[2], args[3]);
                    break;
                case "Status":
                    break;
                case "Crash":
                    break;
                case "Freeze":
                    break;
                case "Unfreeze":
                    break;
                case "Wait":
                    break;
            }
        }
    }
}
