using System;
using System.Collections.Generic;
using System.Text;

namespace GstoreServer
{
    class GstoreRepository : IGstoreRepository
    {
        private Dictionary<(string, string), string> Gstore;

        public GstoreRepository()
        {
            Gstore = new Dictionary<(string, string), string>();
        }

        public string Read(string partitionId, string objectId)
        {
            return Gstore[(partitionId, objectId)];
        }

        public bool Write(string partitionId, string objectId, string value)
        {
            throw new NotImplementedException();
        }
    }
}
