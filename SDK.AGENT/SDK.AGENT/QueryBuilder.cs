using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    /*
 * A Query Request Builder.
 *	This makes it easier to do query calls without actually passing all arguments.
 */
    public sealed class QueryBuilder
    {
        Agent agent;
        Principal effectiveCanisterId;
        Principal canisterId;
        string methodName;
        byte[] arg;
        Optional<long> ingressExpiryDatetime;
        QueryBuilder(Agent agent, Principal canisterId, string methodName)
        {
            this.agent = agent;
            this.canisterId = canisterId;
            this.methodName = methodName;
            this.effectiveCanisterId = canisterId.Clone();
            this.ingressExpiryDatetime = Optional.Empty();
            this.arg = ArrayUtils.EMPTY_BYTE_ARRAY;
        }

        public static QueryBuilder Create(Agent agent, Principal canisterId, string methodName)
        {
            return new QueryBuilder(agent, canisterId, methodName);
        }

        public QueryBuilder EffectiveCanisterId(Principal effectiveCanisterId)
        {
            this.effectiveCanisterId = effectiveCanisterId;
            return this;
        }

        public QueryBuilder Arg(byte[] arg)
        {
            this.arg = arg;
            return this;
        }

        public QueryBuilder ExpireAt(LocalDateTime time)
        {
            this.ingressExpiryDatetime = Optional.Of(time.ToEpochSecond(ZoneOffset.UTC));
            return this;
        }

        public QueryBuilder ExpireAfter(Duration duration)
        {
            Duration permittedDrift = Duration.OfSeconds(Agent.DEFAULT_PERMITTED_DRIFT);
            this.ingressExpiryDatetime = Optional.Of((Duration.OfMillis(System.CurrentTimeMillis()).Plus(duration).Minus(permittedDrift)).ToNanos());
            return this;
        }

        /*
	 * Make a query call. This will return a byte vector.
	 */
        public CompletableFuture<byte[]> Call()
        {
            return agent.QueryRaw(this.canisterId, this.effectiveCanisterId, this.methodName, this.arg, this.ingressExpiryDatetime);
        }

        /*
	 * Make a query call. This will return AgentResponse with a byte vector and headers.
	 */
        public CompletableFuture<Response<byte[]>> Call(Map<string, string> headers)
        {
            Request<byte[]> request = new Request<byte[]>(this.arg, headers);
            return agent.QueryRaw(this.canisterId, this.effectiveCanisterId, this.methodName, request, this.ingressExpiryDatetime);
        }
    }
}