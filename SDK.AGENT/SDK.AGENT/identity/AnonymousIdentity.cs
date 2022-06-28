using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Identity
{
    public sealed class AnonymousIdentity : IIdentity
    {
        public override Principal Sender()
        {
            return Principal.Anonymous();
        }

        public override Signature Sign(byte[] msg)
        {
            return new Signature();
        }
    }
}