using System;
using System.Collections.Generic;
using System.Text;

namespace GstoreServer
{
    interface IGstoreRepository
    {
        public string Read(string serverId, string partitionId, string objectId);
        public bool Write(string partitionId, string objectId, string value);
    }
}
