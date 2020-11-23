using System;
using System.Collections.Generic;
using System.Text;

namespace GstoreServer.Models
{
    class Update
    {
        public int WriteId { get; }
        public string PartitionId { get; }
        public string ObjectId { get; }
        public string Value { get; }

        public Update(int writeId, string partitionId, string objectId, string value)
        {
            this.WriteId = writeId;
            this.PartitionId = partitionId;
            this.ObjectId = objectId;
            this.Value = value;
        }
    }
}
