using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace GstoreServer
{
    class GstoreServiceImpl : GstoreService.GstoreServiceBase
    {
        private readonly GstoreServer GstoreServer;

        public GstoreServiceImpl(GstoreServer server)
        {
            GstoreServer = server;
        }

        public override Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
        {
            ReadReply reply = GstoreServer.Read(request.PartitionId, request.ObjectId);

            return Task.FromResult(reply);
        }

        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            WriteReply reply = GstoreServer.Write(request.PartitionId, request.ObjectId, request.Value);

            return Task.FromResult(reply);
        }

        public override Task<ListServerReply> ListServer(ListServerRequest request, ServerCallContext context)
        {
            ListServerReply reply = GstoreServer.ListServer();
            return Task.FromResult(reply);
        }

        public override Task<PingReply> Ping(PingRequest request, ServerCallContext context)
        {
            PingReply reply = GstoreServer.Ping();

            return Task.FromResult(reply);
        }
    }
}
