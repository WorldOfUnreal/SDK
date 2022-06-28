using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public sealed class ReplicaResponse
    {
        public byte[] payload;
        public Map<string, string> headers;
    }
}