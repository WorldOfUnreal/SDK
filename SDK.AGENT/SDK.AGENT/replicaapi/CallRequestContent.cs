using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Replicaapi
{
    public sealed class CallRequestContent : ISerialize
    {
        public CallRequest callRequest = new CallRequest();
        public sealed class CallRequest
        {
            public Optional<byte[]> nonce;
            public long ingressExpiry;
            public Principal sender;
            public Principal canisterId;
            public string methodName;
            public byte[] arg;
        }

        public override void Serialize(Serializer serializer)
        {
            serializer.SerializeField("request_type", "call");
            if (callRequest.nonce.IsPresent())
                serializer.SerializeField("nonce", callRequest.nonce.Get());
            serializer.SerializeField("ingress_expiry", callRequest.ingressExpiry);
            serializer.SerializeField("sender", callRequest.sender);
            serializer.SerializeField("canister_id", callRequest.canisterId);
            serializer.SerializeField("method_name", callRequest.methodName);
            serializer.SerializeField("arg", callRequest.arg);
        }
    }
}