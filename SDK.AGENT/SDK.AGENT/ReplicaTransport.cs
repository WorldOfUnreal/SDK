using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public interface IReplicaTransport
    {
        CompletableFuture<ReplicaResponse> Status();
        CompletableFuture<ReplicaResponse> Query(Principal canisterId, byte[] envelope, Map<string, string> headers);
        CompletableFuture<ReplicaResponse> Call(Principal canisterId, byte[] envelope, RequestId requestId, Map<string, string> headers);
        CompletableFuture<ReplicaResponse> ReadState(Principal canisterId, byte[] envelope, Map<string, string> headers);
    }
}