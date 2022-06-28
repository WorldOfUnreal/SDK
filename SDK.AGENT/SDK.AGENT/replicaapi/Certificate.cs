using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Replicaapi
{
    public sealed class Certificate
    {
        public HashTree tree;
        public byte[] signature;
        public Optional delegation;
    }
}