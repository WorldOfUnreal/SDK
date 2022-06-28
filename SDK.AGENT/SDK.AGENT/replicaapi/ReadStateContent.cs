using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Replicaapi
{
    public sealed class ReadStateContent : ISerialize
    {
        public ReadStateRequest readStateRequest = new ReadStateRequest();
        public sealed class ReadStateRequest
        {
            public long ingressExpiry;
            public Principal sender;
            public List<List<byte[]>> paths;
        }

        public override void Serialize(Serializer serializer)
        {
            serializer.SerializeField("request_type", "read_state");
            serializer.SerializeField("ingress_expiry", readStateRequest.ingressExpiry);
            serializer.SerializeField("sender", readStateRequest.sender);
            if (readStateRequest.paths != null && !readStateRequest.paths.IsEmpty())
                serializer.SerializeField("paths", readStateRequest.paths);
        }
    }
}