using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PuppetMaster {
    class PuppetMaster {

        private Dictionary<string, PuppetMasterService.PuppetMasterServiceClient> Servers = new Dictionary<string, PuppetMasterService.PuppetMasterServiceClient>();
        private List<PuppetMasterService.PuppetMasterServiceClient> Clients = new List<PuppetMasterService.PuppetMasterServiceClient>();

        public PuppetMaster() {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        public void CreateServer(string serverId, string url, string minDelay, string maxDelay) {
            AddServerConnection(serverId, url);
            string args = serverId + " " + url + " " + minDelay + " " + maxDelay;
            CreateProcess("Server", args);
        }

        public void CreateClient(string username, string clientUrl, string scriptFile) {
            AddClientConnection(clientUrl);
            string args = username + " " + clientUrl + " " + scriptFile;
            CreateProcess("Client", args);
        }

        public void Crash(string serverId)
        {
            try
            {
                Servers[serverId].Crash(new CrashRequest());
            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            } finally
            {
                Servers.Remove(serverId);
            }
        }

        public void Freeze(string serverId)
        {
            try
            {
                Servers[serverId].Freeze(new FreezeRequest());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Unfreeze(string serverId)
        {
            try
            {
                Servers[serverId].Unfreeze(new UnfreezeRequest());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Wait(int milis)
        {
            Thread.Sleep(milis);
        }

        public void Status()
        {
            List<string> serversToRemove = new List<string>();
            List<PuppetMasterService.PuppetMasterServiceClient> clientsToRemove = new List<PuppetMasterService.PuppetMasterServiceClient>();
            foreach (KeyValuePair<string, PuppetMasterService.PuppetMasterServiceClient> serverItem in Servers)
            {
                try
                {
                    serverItem.Value.PrintStatus(new StatusRequest());
                }
                catch (Exception ex)
                {
                    serversToRemove.Add(serverItem.Key);
                }
            }

            foreach (PuppetMasterService.PuppetMasterServiceClient client in Clients)
            {
                try
                {
                    client.PrintStatus(new StatusRequest());
                }
                catch (Exception ex)
                {
                    clientsToRemove.Add(client);
                }
            }

            foreach (string server in serversToRemove)
            {
                Servers.Remove(server);
            }


            foreach (PuppetMasterService.PuppetMasterServiceClient client in clientsToRemove)
            {
                Clients.Remove(client);
            }
        }

        private void AddServerConnection(string serverId, string url)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(url);
            Servers.Add(serverId, new PuppetMasterService.PuppetMasterServiceClient(channel)); 
        }

        private void AddClientConnection(string url)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(url);
            Clients.Add(new PuppetMasterService.PuppetMasterServiceClient(channel));
        }

        private void CreateProcess(string type, string args) {
            Process process = new Process();
            string[] current_path = System.AppDomain.CurrentDomain.BaseDirectory.Split(new[] { "\\PuppetMaster\\bin\\Debug" }, StringSplitOptions.None);
            string path = current_path[0] + $"\\{type}\\bin\\Debug\\netcoreapp3.1\\Gstore{type}.exe";

            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = args;
            process.Start();
        }
    }
}
