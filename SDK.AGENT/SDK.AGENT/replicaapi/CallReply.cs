using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Replicaapi
{
    public sealed class CallReply : Response
    {
        public byte[] arg;
        public CallReply()
        {
        }

        public CallReply(byte[] arg)
        {
            this.arg = arg;
        }
    }
}