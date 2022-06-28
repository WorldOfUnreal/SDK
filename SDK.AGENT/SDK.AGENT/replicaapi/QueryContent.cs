using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Replicaapi
{
    public sealed class QueryContent : ISerialize
    {
        public QueryRequest queryRequest = new QueryRequest();
        public sealed class QueryRequest
        {
            public long ingressExpiry;
            public Principal sender;
            public Principal canisterId;
            public string methodName;
            public byte[] arg;
        }

        public override void Serialize(Serializer serializer)
        {
            serializer.SerializeField("request_type", "query");
            serializer.SerializeField("ingress_expiry", queryRequest.ingressExpiry);
            serializer.SerializeField("sender", queryRequest.sender);
            serializer.SerializeField("canister_id", queryRequest.canisterId);
            serializer.SerializeField("method_name", queryRequest.methodName);
            serializer.SerializeField("arg", queryRequest.arg);
        }
    }
}