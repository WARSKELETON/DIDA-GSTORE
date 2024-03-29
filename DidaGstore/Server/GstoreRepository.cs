﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GstoreServer
{
    class GstoreRepository : IGstoreRepository
    {
        private readonly Dictionary<Tuple<string, string>, string> Gstore;

        public GstoreRepository()
        {
            Gstore = new Dictionary<Tuple<string, string>, string>();
        }

        public Tuple<string, string> GetKey(string partitionId, string objectId)
        {
            return Gstore.Keys.FirstOrDefault(key => key.Item1 == partitionId && key.Item2 == objectId);
        }

        public string Read(string partitionId, string objectId)
        {
            return Gstore[GetKey(partitionId, objectId)];
        }

        public bool Write(string partitionId, string objectId, string value)
        {
            Tuple<string, string> key;
            if ((key = GetKey(partitionId, objectId)) != null)
            {
                Gstore[key] = value;
                return true;
            }
            Gstore[new Tuple<string, string>(partitionId, objectId)] = value;
            return true;
        }

        public List<StoredObject> GetAllObjects()
        {
            List<StoredObject> storedObjects = new List<StoredObject>();
            foreach (KeyValuePair<Tuple<string, string>, string> item in Gstore)
            {
                if (item.Value == null) continue;

                storedObjects.Add(new StoredObject
                {
                    Master = false,
                    ServerId = "",
                    PartitionId = item.Key.Item1,
                    ObjectId = item.Key.Item2,
                    Value = item.Value
                });
            }
            return storedObjects;
        }
    }
}
