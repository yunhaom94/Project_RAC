using System.Collections.Generic;

using RAC.Payloads;
using RAC.History;
using RAC.Errors;


namespace RAC
{
    public class MemoryManager
    {
        // TODO: a better storage system
        private Dictionary<string, Payload> storage;
        public Dictionary<string, ObjectHistory> history;

        public MemoryManager()
        {
            storage = new Dictionary<string, Payload>();
            history = new Dictionary<string, ObjectHistory>();
        }

        public bool StorePayload(string uid, Payload payload)
        {
            storage[uid] = payload;
            return true;
        }

        public Payload GetPayload(string uid)
        {
            try
            {
                return storage[uid];
            }
            catch (KeyNotFoundException)
            {
                throw new PayloadNotFoundException();
            }
        }
    }
}