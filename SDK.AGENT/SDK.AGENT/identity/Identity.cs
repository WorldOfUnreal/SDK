using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Identity
{
    public interface IIdentity
    {
        Principal Sender();
        Signature Sign(byte[] blob);
    }
}