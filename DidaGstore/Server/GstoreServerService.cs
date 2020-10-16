using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace GstoreServer
{
    class GstoreServerService : GstoreService.GstoreServiceBase
    {
        private GstoreServer GstoreServer;

        public GstoreServerService(GstoreServer server)
        {
            GstoreServer = server;
        }

        public override Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
        {
            ReadReply reply = GstoreServer.Read(request.PartitionId, request.ObjectId);

            return Task.FromResult(reply);
        }
    }
}
