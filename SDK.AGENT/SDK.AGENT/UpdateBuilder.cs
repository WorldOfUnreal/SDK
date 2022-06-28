using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    /*
* An Update Request Builder.
* This makes it easier to do update calls without actually passing all arguments or specifying
* if you want to wait or not.
*/
    public sealed class UpdateBuilder
    {
        static readonly Logger LOG = LoggerFactory.GetLogger(typeof(UpdateBuilder));
        Agent agent;
        Principal effectiveCanisterId;
        Principal canisterId;
        string methodName;
        byte[] arg;
        Optional<long> ingressExpiryDatetime;
        UpdateBuilder(Agent agent, Principal canisterId, string methodName)
        {
            this.agent = agent;
            this.canisterId = canisterId;
            this.methodName = methodName;
            this.effectiveCanisterId = canisterId.Clone();
            this.ingressExpiryDatetime = Optional.Empty();
            this.arg = ArrayUtils.EMPTY_BYTE_ARRAY;
        }

        public static UpdateBuilder Create(Agent agent, Principal canisterId, string methodName)
        {
            return new UpdateBuilder(agent, canisterId, methodName);
        }

        public UpdateBuilder EffectiveCanisterId(Principal effectiveCanisterId)
        {
            this.effectiveCanisterId = effectiveCanisterId;
            return this;
        }

        public UpdateBuilder Arg(byte[] arg)
        {
            this.arg = arg;
            return this;
        }

        public UpdateBuilder ExpireAt(LocalDateTime time)
        {
            this.ingressExpiryDatetime = Optional.Of(time.ToEpochSecond(ZoneOffset.UTC));
            return this;
        }

        public UpdateBuilder ExpireAfter(Duration duration)
        {
            Duration permittedDrift = Duration.OfSeconds(Agent.DEFAULT_PERMITTED_DRIFT);
            this.ingressExpiryDatetime = Optional.Of((Duration.OfMillis(System.CurrentTimeMillis()).Minus(permittedDrift)).ToNanos());
            return this;
        }

        /*
	 * Make a update call. This will return a byte vector.
	 */
        public CompletableFuture<byte[]> CallAndWait(Waiter waiter)
        {
            RequestId requestId;
            try
            {
                requestId = agent.UpdateRaw(this.canisterId, this.effectiveCanisterId, this.methodName, this.arg, this.ingressExpiryDatetime).Get();
            }
            catch (InterruptedException e)
            {
                throw AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, e, e.GetLocalizedMessage());
            }
            catch (ExecutionException e)
            {
                throw AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, e, e.GetLocalizedMessage());
            }
            catch (AgentError e)
            {
                throw AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, e, e.GetLocalizedMessage());
            }

            CompletableFuture<byte[]> response = new CompletableFuture<byte[]>();
            do
            {
                try
                {
                    RequestStatusResponse statusResponse = agent.RequestStatusRaw(requestId, effectiveCanisterId).Get();
                    switch (statusResponse.status)
                    {
                        case REPLIED_STATUS:
                            response.Complete(statusResponse.replied.Get().arg);
                            return response;
                        case REJECTED_STATUS:
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.REPLICA_ERROR, statusResponse.rejected.Get().rejectCode, statusResponse.rejected.Get().rejectMessage));
                            return response;
                        case DONE_STATUS:
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.REQUEST_STATUS_DONE_NO_REPLY, requestId.ToHexString()));
                            return response;
                    }
                }
                catch (InterruptedException e)
                {
                    LOG.Debug(e.GetLocalizedMessage(), e);
                }
                catch (ExecutionException e)
                {
                    LOG.Debug(e.GetLocalizedMessage(), e);
                }
                catch (AgentError e)
                {
                    LOG.Debug(e.GetLocalizedMessage(), e);
                }
            }
            while (waiter.WaitUntil());
            throw AgentError.Create(AgentError.AgentErrorCode.TIMEOUT_WAITING_FOR_RESPONSE);
        }

        /*
	 * Make a update call. This will return a byte RequestId.
	 */
        public CompletableFuture<RequestId> Call()
        {
            return agent.UpdateRaw(this.canisterId, this.effectiveCanisterId, this.methodName, this.arg, this.ingressExpiryDatetime);
        }

        public CompletableFuture<byte[]> GetState(RequestId requestId, Waiter waiter)
        {
            CompletableFuture<byte[]> response = new CompletableFuture<byte[]>();
            do
            {
                try
                {
                    RequestStatusResponse statusResponse = agent.RequestStatusRaw(requestId, effectiveCanisterId).Get();
                    switch (statusResponse.status)
                    {
                        case REPLIED_STATUS:
                            response.Complete(statusResponse.replied.Get().arg);
                            return response;
                        case REJECTED_STATUS:
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.REPLICA_ERROR, statusResponse.rejected.Get().rejectCode, statusResponse.rejected.Get().rejectMessage));
                            return response;
                        case DONE_STATUS:
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.REQUEST_STATUS_DONE_NO_REPLY, requestId.ToHexString()));
                            return response;
                    }
                }
                catch (InterruptedException e)
                {
                    LOG.Debug(e.GetLocalizedMessage(), e);
                }
                catch (ExecutionException e)
                {
                    LOG.Debug(e.GetLocalizedMessage(), e);
                }
                catch (AgentError e)
                {
                    LOG.Debug(e.GetLocalizedMessage(), e);
                }
            }
            while (waiter.WaitUntil());
            throw AgentError.Create(AgentError.AgentErrorCode.TIMEOUT_WAITING_FOR_RESPONSE);
        }

        /*
	 * Make a update call. This will return AgentResponse with a requestId and headers.
	 */
        public CompletableFuture<Response<RequestId>> Call(Map<string, string> headers)
        {
            Request<byte[]> request = new Request<byte[]>(this.arg, headers);
            return agent.UpdateRaw(this.canisterId, this.effectiveCanisterId, this.methodName, request, this.ingressExpiryDatetime);
        }

        public CompletableFuture<Response<byte[]>> GetState(RequestId requestId, Map<string, string> headers, Waiter waiter)
        {
            CompletableFuture<Response<byte[]>> response = new CompletableFuture<Response<byte[]>>();
            do
            {
                try
                {
                    Request<Void> request = new Request<Void>(null, headers);
                    Response<RequestStatusResponse> rawResponse = agent.RequestStatusRaw(requestId, effectiveCanisterId, request).Get();
                    RequestStatusResponse statusResponse = rawResponse.GetPayload();
                    switch (statusResponse.status)
                    {
                        case REPLIED_STATUS:
                            Response<byte[]> stateResponse = new Response<byte[]>(statusResponse.replied.Get().arg, rawResponse.GetHeaders());
                            response.Complete(stateResponse);
                            return response;
                        case REJECTED_STATUS:
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.REPLICA_ERROR, statusResponse.rejected.Get().rejectCode, statusResponse.rejected.Get().rejectMessage));
                            return response;
                        case DONE_STATUS:
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.REQUEST_STATUS_DONE_NO_REPLY, requestId.ToHexString()));
                            return response;
                    }
                }
                catch (InterruptedException e)
                {
                    LOG.Debug(e.GetLocalizedMessage(), e);
                }
                catch (ExecutionException e)
                {
                    LOG.Debug(e.GetLocalizedMessage(), e);
                }
                catch (AgentError e)
                {
                    LOG.Debug(e.GetLocalizedMessage(), e);
                }
            }
            while (waiter.WaitUntil());
            throw AgentError.Create(AgentError.AgentErrorCode.TIMEOUT_WAITING_FOR_RESPONSE);
        }
    }
}