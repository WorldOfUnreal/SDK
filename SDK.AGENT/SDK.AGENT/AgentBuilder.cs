using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public class AgentBuilder
    {
        AgentConfig config = new AgentConfig();
        public virtual Agent Build()
        {
            Agent agent = new Agent(this);
            return agent;
        }

        public virtual AgentBuilder Transport(ReplicaTransport transport)
        {
            this.config.transport = Optional.Of(transport);
            return this;
        }

        public virtual AgentBuilder IngresExpiry(Duration duration)
        {
            this.config.ingressExpiryDuration = Optional.Of(duration);
            return this;
        }

        /*
	 * Add an identity provider for signing messages. This is required.
	 * @param identity identity provider
	 */
        public virtual AgentBuilder Identity(Identity identity)
        {
            this.config.identity = identity;
            return this;
        }

        /*
	* Add a NonceFactory to this Agent. By default, no nonce is produced.
	*/
        public virtual AgentBuilder NonceFactory(NonceFactory nonceFactory)
        {
            this.config.nonceFactory = nonceFactory;
            return this;
        }
    }
}