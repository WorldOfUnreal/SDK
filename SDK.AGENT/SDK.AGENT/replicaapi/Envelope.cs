using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Replicaapi
{
    public sealed class Envelope<T>
    {
        public T content;
        public Optional<byte[]> senderPubkey;
        public Optional<byte[]> senderSig;
    }
}