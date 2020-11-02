﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GstoreClient.Models
{
    // PartitionId
    // Master S1
    // S1 
    // S2
    // S3 
    class Partition
    {
        public string Id { get; }
        public string Master { get; }
        public List<string> Servers { get; }

        public Partition(string id, string master, List<string> servers)
        {
            this.Id = id;
            this.Master = master;
            this.Servers = servers;
        }


    }
}