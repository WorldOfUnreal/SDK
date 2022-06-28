using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    /// <summary>
    /// A configuration for an agent.
    /// </summary>
    class AgentConfig
    {
        AgentConfig()
        {
        }

        Optional<ReplicaTransport> transport = Optional.Empty();
        Optional<Duration> ingressExpiryDuration = Optional.Empty();
        Identity identity = new AnonymousIdentity();
        NonceFactory nonceFactory = new NonceFactory();
    }
}