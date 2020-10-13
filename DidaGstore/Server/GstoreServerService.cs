using System;
using System.Collections.Generic;
using System.Text;

namespace GstoreServer
{
    class GstoreServerService : GstoreService.GstoreServiceBase
    {
        private IGstoreRepository GstoreRepository;

        public GstoreServerService()
        {
            GstoreRepository = new GstoreRepository();
        }
    }
}
