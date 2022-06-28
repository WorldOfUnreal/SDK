using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public sealed class NonceFactory
    {
        public byte[] Generate()
        {
            byte[] nonce = new byte[16];
            new SecureRandom().NextBytes(nonce);
            return nonce;
        }
    }
}