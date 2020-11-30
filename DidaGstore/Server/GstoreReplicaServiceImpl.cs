using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace GstoreServer
{
    class GstoreReplicaServiceImpl : GstoreReplicaService.GstoreReplicaServiceBase
    {
        private readonly GstoreServer GstoreServer;

        public GstoreReplicaServiceImpl(GstoreServer server)
        {
            GstoreServer = server;
        }

        public override Task<UpdateReply> Update(UpdateRequest request, ServerCallContext context)
        {
            UpdateReply reply = GstoreServer.AdvancedUpdate(request.PartitionId, request.ObjectId, request.Value, request.WriteId);

            return Task.FromResult(reply);
        }

        public override Task<SyncLockReply> SyncLock(SyncLockRequest request, ServerCallContext context)
        {
            SyncLockReply reply = GstoreServer.SyncLock(request.PartitionId);

            return Task.FromResult(reply);
        }

        public override Task<FinishedSyncReply> FinishedSync(FinishedSyncRequest request, ServerCallContext context)
        {
            FinishedSyncReply reply = GstoreServer.FinishedSync(request.PartitionId);

            return Task.FromResult(reply);
        }

        public override Task<PingReplicaReply> PingReplica(PingReplicaRequest request, ServerCallContext context)
        {
            PingReplicaReply reply = GstoreServer.PingReplica();

            return Task.FromResult(reply);
        }
    }
}
