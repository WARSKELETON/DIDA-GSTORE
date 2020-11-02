using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GstoreServer {
    class PuppetMasterServiceImpl : PuppetMasterService.PuppetMasterServiceBase {

        private GstoreServer GstoreServer;

        public PuppetMasterServiceImpl(GstoreServer server) {
            GstoreServer = server;
        }

        public override Task<StatusReply> PrintStatus(StatusRequest request, ServerCallContext context) {
            StatusReply reply = GstoreServer.PrintStatus();

            return Task.FromResult(reply);
        }

        public override Task<CrashReply> Crash(CrashRequest request, ServerCallContext context) {
            Console.WriteLine("Server: Task crash");
            CrashReply reply = GstoreServer.Crash();
            return Task.FromResult(reply);
        }

        public override Task<FreezeReply> Freeze(FreezeRequest request, ServerCallContext context) {
            FreezeReply reply = GstoreServer.Freeze();

            return Task.FromResult(reply);
        }

        public override Task<UnfreezeReply> Unfreeze(UnfreezeRequest request, ServerCallContext context) {
            UnfreezeReply reply = GstoreServer.Unfreeze();

            return Task.FromResult(reply);
        }
    }
}
