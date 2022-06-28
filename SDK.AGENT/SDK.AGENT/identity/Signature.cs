using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Identity
{
    public sealed class Signature
    {
        public Optional<byte[]> publicKey;
        public Optional<byte[]> signature;
        public Signature()
        {
        }

        public Signature(byte[] publicKey, byte[] signature)
        {
            this.publicKey = Optional.Of(publicKey);
            this.signature = Optional.Of(signature);
        }
    }
}