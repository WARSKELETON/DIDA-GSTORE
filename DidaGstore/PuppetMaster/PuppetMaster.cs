using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace PuppetMaster {
    class PuppetMaster {

        private Dictionary<string, PuppetMasterService.PuppetMasterServiceClient> servers = new Dictionary<string, PuppetMasterService.PuppetMasterServiceClient>();
        private List<PuppetMasterService.PuppetMasterServiceClient> clients = new List<PuppetMasterService.PuppetMasterServiceClient>();

        public PuppetMaster() {

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

        private void AddServerConnection(string serverId, string url)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(url);
            servers.Add(serverId, new PuppetMasterService.PuppetMasterServiceClient(channel)); 
        }

        private void AddClientConnection(string url)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(url);
            clients.Add(new PuppetMasterService.PuppetMasterServiceClient(channel));
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
