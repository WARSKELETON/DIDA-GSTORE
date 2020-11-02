using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GstoreClient
{
    class PuppetMasterServiceImpl : PuppetMasterService.PuppetMasterServiceBase
    {

        private GstoreClient GstoreClient;

        public PuppetMasterServiceImpl(GstoreClient client)
        {
            GstoreClient = client;
        }

        public override Task<StatusReply> PrintStatus(StatusRequest request, ServerCallContext context)
        {
            StatusReply reply = GstoreClient.PrintStatus();

            return Task.FromResult(reply);
        }
    }
}
