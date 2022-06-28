using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public sealed class Agent
    {
        static readonly byte[] IC_REQUEST_DOMAIN_SEPARATOR = "\nic-request".GetBytes(StandardCharsets.UTF_8);
        static readonly byte[] IC_STATE_ROOT_DOMAIN_SEPARATOR = "\ric-state-root".GetBytes(StandardCharsets.UTF_8);
        static readonly byte[] IC_ROOT_KEY = "\\x30\\x81\\x82\\x30\\x1d\\x06\\x0d\\x2b\\x06\\x01\\x04\\x01\\x82\\xdc\\x7c\\x05\\x03\\x01\\x02\\x01\\x06\\x0c\\x2b\\x06\\x01\\x04\\x01\\x82\\xdc\\x7c\\x05\\x03\\x02\\x01\\x03\\x61\\x00\\x81\\x4c\\x0e\\x6e\\xc7\\x1f\\xab\\x58\\x3b\\x08\\xbd\\x81\\x37\\x3c\\x25\\x5c\\x3c\\x37\\x1b\\x2e\\x84\\x86\\x3c\\x98\\xa4\\xf1\\xe0\\x8b\\x74\\x23\\x5d\\x14\\xfb\\x5d\\x9c\\x0c\\xd5\\x46\\xd9\\x68\\x5f\\x91\\x3a\\x0c\\x0b\\x2c\\xc5\\x34\\x15\\x83\\xbf\\x4b\\x43\\x92\\xe4\\x67\\xdb\\x96\\xd6\\x5b\\x9b\\xb4\\xcb\\x71\\x71\\x12\\xf8\\x47\\x2e\\x0d\\x5a\\x4d\\x14\\x50\\x5f\\xfd\\x74\\x84\\xb0\\x12\\x91\\x09\\x1c\\x5f\\x87\\xb9\\x88\\x83\\x46\\x3f\\x98\\x09\\x1a\\x0b\\xaa\\xae".GetBytes(StandardCharsets.UTF_8);
        static readonly int DEFAULT_INGRESS_EXPIRY_DURATION = 300;
        static readonly int DEFAULT_PERMITTED_DRIFT = 60;
        static readonly Logger LOG = LoggerFactory.GetLogger(typeof(Agent));
        ReplicaTransport transport;
        Duration ingressExpiryDuration;
        Identity identity;
        NonceFactory nonceFactory;
        Optional<byte[]> rootKey;
        Agent(AgentBuilder builder)
        {
            this.transport = builder.config.transport.Get();
            if (builder.config.ingressExpiryDuration.IsPresent())
                ingressExpiryDuration = builder.config.ingressExpiryDuration.Get();
            else
                ingressExpiryDuration = Duration.OfSeconds(DEFAULT_INGRESS_EXPIRY_DURATION);
            this.identity = builder.config.identity;
            this.nonceFactory = builder.config.nonceFactory;
        }

        long GetExpiryDate()
        {

            // TODO: evaluate if we need this on the agent side
            Duration permittedDrift = Duration.OfSeconds(DEFAULT_PERMITTED_DRIFT);
            return ((this.ingressExpiryDuration.Plus(Duration.OfMillis(System.CurrentTimeMillis()))).Minus(permittedDrift)).ToNanos();
        }

        /*
	 * By default, the agent is configured to talk to the main Internet Computer,
	 * and verifies responses using a hard-coded public key.
	 * 
	 * This function will instruct the agent to ask the endpoint for its public key,
	 * and use that instead. This is required when talking to a local test instance,
	 * for example.
	 * 
	 * Only use this when you are _not_ talking to the main Internet Computer,
	 * otherwise you are prone to man-in-the-middle attacks! Do not call this
	 * function by default.*
	 */
        public void FetchRootKey()
        {
            Status status;
            try
            {
                status = this.Status().Get();
                if (status.rootKey.IsPresent())
                    this.SetRootKey(status.rootKey.Get());
                else
                    AgentError.Create(AgentError.AgentErrorCode.NO_ROOT_KEY_IN_STATUS, status);
            }
            catch (InterruptedException e)
            {
                LOG.Error(e.GetLocalizedMessage(), e);
                AgentError.Create(AgentError.AgentErrorCode.TRANSPORT_ERROR, e);
            }
            catch (ExecutionException e)
            {
                LOG.Error(e.GetLocalizedMessage(), e);
                AgentError.Create(AgentError.AgentErrorCode.TRANSPORT_ERROR, e);
            }
        }

        /*
	 * By default, the agent is configured to talk to the main Internet Computer,
	 * and verifies responses using a hard-coded public key.
	 * 
	 * Using this function you can set the root key to a known one if you know if
	 * beforehand.
	 */
        public void SetRootKey(byte[] rootKey)
        {
            lock (this)
            {
                this.rootKey = Optional.Of(rootKey);
            }
        }

        byte[] GetRootKey()
        {
            if (rootKey.IsPresent())
                return rootKey.Get();
            else
                throw AgentError.Create(AgentError.AgentErrorCode.COULD_NOT_READ_ROOT_KEY);
        }

        byte[] ConstructMessage(RequestId requestId)
        {
            return ArrayUtils.AddAll(IC_REQUEST_DOMAIN_SEPARATOR, requestId.Get());
        }

        /*
	 * Calls and returns the information returned by the status endpoint of a
	 * replica.
	 */
        public CompletableFuture<Status> Status()
        {
            ObjectMapper objectMapper = new ObjectMapper(new CBORFactory());
            objectMapper.RegisterModule(new Jdk8Module());
            CompletableFuture<Status> response = new CompletableFuture<Status>();
            transport.Status().WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        try
                        {
                            Status status = objectMapper.ReadValue(input.payload, typeof(Status));
                            response.Complete(status);
                        }
                        catch (Exception e)
                        {
                            LOG.Debug(e.GetLocalizedMessage());
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_CBOR_DATA, e, input));
                        }
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_CBOR_DATA, input));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        public CompletableFuture<Response<byte[]>> QueryRaw(Principal canisterId, Principal effectiveCanisterId, string method, Request<byte[]> request, Optional<long> ingressExpiryDatetime)
        {
            QueryContent queryContent = new QueryContent();
            queryContent.queryRequest.methodName = method;
            queryContent.queryRequest.canisterId = canisterId;
            queryContent.queryRequest.arg = request.GetPayload();
            queryContent.queryRequest.sender = this.identity.Sender();
            if (ingressExpiryDatetime.IsPresent())
                queryContent.queryRequest.ingressExpiry = ingressExpiryDatetime.Get();
            else
                queryContent.queryRequest.ingressExpiry = this.GetExpiryDate();
            CompletableFuture<Response<byte[]>> response = new CompletableFuture<Response<byte[]>>();
            this.QueryEndpoint(effectiveCanisterId, queryContent, request.GetHeaders()).WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        if (input.replied.IsPresent())
                        {
                            byte[] out = input.replied.Get().arg;
                            Response<byte[]> queryResponse = new Response<byte[]>(@out, input.headers);
                            response.Complete(queryResponse);
                        }
                        else if (input.rejected.IsPresent())
                        {
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.REPLICA_ERROR, input.rejected.Get().rejectCode, input.rejected.Get().rejectMessage));
                        }
                        else
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_REPLICA_STATUS));
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_REPLICA_STATUS));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        public CompletableFuture<byte[]> QueryRaw(Principal canisterId, Principal effectiveCanisterId, string method, byte[] arg, Optional<long> ingressExpiryDatetime)
        {
            QueryContent queryContent = new QueryContent();
            queryContent.queryRequest.methodName = method;
            queryContent.queryRequest.canisterId = canisterId;
            queryContent.queryRequest.arg = arg;
            queryContent.queryRequest.sender = this.identity.Sender();
            if (ingressExpiryDatetime.IsPresent())
                queryContent.queryRequest.ingressExpiry = ingressExpiryDatetime.Get();
            else
                queryContent.queryRequest.ingressExpiry = this.GetExpiryDate();
            CompletableFuture<byte[]> response = new CompletableFuture<byte[]>();
            this.QueryEndpoint(effectiveCanisterId, queryContent, null).WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        if (input.replied.IsPresent())
                        {
                            byte[] out = input.replied.Get().arg;
                            response.Complete(@out);
                        }
                        else if (input.rejected.IsPresent())
                        {
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.REPLICA_ERROR, input.rejected.Get().rejectCode, input.rejected.Get().rejectMessage));
                        }
                        else
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_REPLICA_STATUS));
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_REPLICA_STATUS));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        public CompletableFuture<QueryResponse> QueryEndpoint(Principal effectiveCanisterId, QueryContent request, Map<string, string> headers)
        {
            RequestId requestId = RequestId.ToRequestId(request);
            byte[] msg = this.ConstructMessage(requestId);
            Signature signature = this.identity.Sign(msg);
            ObjectMapper objectMapper = new ObjectMapper(new CBORFactory()).RegisterModule(new Jdk8Module());
            ObjectWriter objectWriter = objectMapper.WriterFor(typeof(Envelope)).WithAttribute("request_type", "query");
            Envelope<QueryContent> envelope = new Envelope<QueryContent>();
            envelope.content = request;
            envelope.senderPubkey = signature.publicKey;
            envelope.senderSig = signature.signature;
            byte[] bytes = null;
            try
            {
                bytes = objectWriter.WriteValueAsBytes(envelope);
            }
            catch (JsonProcessingException e)
            {
                throw AgentError.Create(AgentError.AgentErrorCode.INVALID_CBOR_DATA, e, envelope);
            }

            CompletableFuture<QueryResponse> response = new CompletableFuture<QueryResponse>();
            transport.Query(effectiveCanisterId, bytes, headers).WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        try
                        {
                            QueryResponse queryResponse = objectMapper.ReadValue(input.payload, typeof(QueryResponse));
                            queryResponse.headers = input.headers;
                            response.Complete(queryResponse);
                        }
                        catch (Exception e)
                        {
                            LOG.Debug(e.GetLocalizedMessage(), e);
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.MESSAGE_ERROR, e, new string (input.payload, StandardCharsets.UTF_8)));
                        }
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.TRANSPORT_ERROR, "Payload is empty"));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        /*
	 * The simplest way to do an update call; sends a byte array and will return a
	 * RequestId. The RequestId should then be used for request_status (most likely
	 * in a loop).
	 */
        public CompletableFuture<Response<RequestId>> UpdateRaw(Principal canisterId, Principal effectiveCanisterId, string method, Request<byte[]> request, Optional<long> ingressExpiryDatetime)
        {
            CallRequestContent callRequestContent = new CallRequestContent();
            callRequestContent.callRequest.methodName = method;
            callRequestContent.callRequest.canisterId = canisterId;
            callRequestContent.callRequest.arg = request.GetPayload();
            callRequestContent.callRequest.sender = this.identity.Sender();
            if (this.nonceFactory != null)
                callRequestContent.callRequest.nonce = Optional.Of(nonceFactory.Generate());
            if (ingressExpiryDatetime.IsPresent())
                callRequestContent.callRequest.ingressExpiry = ingressExpiryDatetime.Get();
            else
                callRequestContent.callRequest.ingressExpiry = this.GetExpiryDate();
            CompletableFuture<Response<RequestId>> response = new CompletableFuture<Response<RequestId>>();
            this.CallEndpoint(effectiveCanisterId, callRequestContent, request.GetHeaders()).WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        Response<RequestId> updateResponse = new Response<RequestId>(input.requestId, input.headers);
                        response.Complete(updateResponse);
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_REPLICA_STATUS));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        /*
	 * The simplest way to do an update call; sends a byte array and will return a
	 * RequestId. The RequestId should then be used for request_status (most likely
	 * in a loop).
	 */
        public CompletableFuture<RequestId> UpdateRaw(Principal canisterId, Principal effectiveCanisterId, string method, byte[] arg, Optional<long> ingressExpiryDatetime)
        {
            CallRequestContent callRequestContent = new CallRequestContent();
            callRequestContent.callRequest.methodName = method;
            callRequestContent.callRequest.canisterId = canisterId;
            callRequestContent.callRequest.arg = arg;
            callRequestContent.callRequest.sender = this.identity.Sender();
            if (this.nonceFactory != null)
                callRequestContent.callRequest.nonce = Optional.Of(nonceFactory.Generate());
            if (ingressExpiryDatetime.IsPresent())
                callRequestContent.callRequest.ingressExpiry = ingressExpiryDatetime.Get();
            else
                callRequestContent.callRequest.ingressExpiry = this.GetExpiryDate();
            CompletableFuture<RequestId> response = new CompletableFuture<RequestId>();
            this.CallEndpoint(effectiveCanisterId, callRequestContent, null).WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        response.Complete(input.requestId);
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_REPLICA_STATUS));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        public CompletableFuture<UpdateResponse> CallEndpoint(Principal effectiveCanisterId, CallRequestContent request, Map<string, string> headers)
        {
            RequestId requestId = RequestId.ToRequestId(request);
            byte[] msg = this.ConstructMessage(requestId);
            Signature signature = this.identity.Sign(msg);
            ObjectMapper objectMapper = new ObjectMapper(new CBORFactory()).RegisterModule(new Jdk8Module());
            ObjectWriter objectWriter = objectMapper.WriterFor(typeof(Envelope)).WithAttribute("request_type", "call");
            Envelope<CallRequestContent> envelope = new Envelope<CallRequestContent>();
            envelope.content = request;
            envelope.senderPubkey = signature.publicKey;
            envelope.senderSig = signature.signature;
            byte[] bytes = null;
            try
            {
                bytes = objectWriter.WriteValueAsBytes(envelope);
            }
            catch (JsonProcessingException e)
            {
                throw AgentError.Create(AgentError.AgentErrorCode.INVALID_CBOR_DATA, e, envelope);
            }

            CompletableFuture<UpdateResponse> response = new CompletableFuture<UpdateResponse>();
            transport.Call(effectiveCanisterId, bytes, requestId, headers).WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        UpdateResponse updateResponse = new UpdateResponse();
                        updateResponse.requestId = requestId;
                        updateResponse.headers = input.headers;
                        response.Complete(updateResponse);
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.TRANSPORT_ERROR, "Payload is empty"));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        public CompletableFuture<Response<RequestStatusResponse>> RequestStatusRaw(RequestId requestId, Principal effectiveCanisterId, Request<Void> request)
        {
            List<List<byte[]>> paths = new List<List<byte[]>>();
            List<byte[]> path = new List<byte[]>();
            path.Add("request_status".GetBytes());
            path.Add(requestId.Get());
            paths.Add(path);
            CompletableFuture<Response<RequestStatusResponse>> response = new CompletableFuture<Response<RequestStatusResponse>>();
            this.ReadStateRaw(effectiveCanisterId, paths, request.GetHeaders()).WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        try
                        {
                            RequestStatusResponse requestStatusResponse = ResponseAuthentication.LookupRequestStatus(input.certificate, requestId);
                            Response<RequestStatusResponse> stateResponse = new Response<RequestStatusResponse>(requestStatusResponse, input.headers);
                            response.Complete(stateResponse);
                        }
                        catch (AgentError e)
                        {
                            response.CompleteExceptionally(e);
                        }
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_CBOR_DATA, input));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        public CompletableFuture<RequestStatusResponse> RequestStatusRaw(RequestId requestId, Principal effectiveCanisterId)
        {
            List<List<byte[]>> paths = new List<List<byte[]>>();
            List<byte[]> path = new List<byte[]>();
            path.Add("request_status".GetBytes());
            path.Add(requestId.Get());
            paths.Add(path);
            CompletableFuture<RequestStatusResponse> response = new CompletableFuture<RequestStatusResponse>();
            this.ReadStateRaw(effectiveCanisterId, paths, null).WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        try
                        {
                            RequestStatusResponse requestStatusResponse = ResponseAuthentication.LookupRequestStatus(input.certificate, requestId);
                            response.Complete(requestStatusResponse);
                        }
                        catch (AgentError e)
                        {
                            response.CompleteExceptionally(e);
                        }
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_CBOR_DATA, input));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        public CompletableFuture<CertificateResponse> ReadStateRaw(Principal effectiveCanisterId, List<List<byte[]>> paths, Map<string, string> headers)
        {
            ObjectMapper objectMapper = new ObjectMapper(new CBORFactory());
            objectMapper.RegisterModule(new Jdk8Module());
            ReadStateContent readStateContent = new ReadStateContent();
            readStateContent.readStateRequest.paths = paths;
            readStateContent.readStateRequest.sender = this.identity.Sender();
            readStateContent.readStateRequest.ingressExpiry = this.GetExpiryDate();
            CompletableFuture<CertificateResponse> response = new CompletableFuture<CertificateResponse>();
            this.ReadStateEndpoint(effectiveCanisterId, readStateContent, headers, typeof(ReadStateResponse)).WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        try
                        {
                            Certificate cert = objectMapper.ReadValue(input.state.certificate, typeof(Certificate));
                            CertificateResponse certificateResponse = new CertificateResponse();
                            certificateResponse.certificate = cert;
                            certificateResponse.headers = input.headers;
                            response.Complete(certificateResponse);
                        }
                        catch (Exception e)
                        {
                            LOG.Debug(e.GetLocalizedMessage());
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_CBOR_DATA, e, input));
                        }
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.INVALID_CBOR_DATA, input));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        public CompletableFuture<StateResponse<T>> ReadStateEndpoint<T>(Principal effectiveCanisterId, ReadStateContent request, Map<string, string> headers, Class<T> clazz)
        {
            RequestId requestId = RequestId.ToRequestId(request);
            byte[] msg = this.ConstructMessage(requestId);
            Signature signature = this.identity.Sign(msg);
            ObjectMapper objectMapper = new ObjectMapper(new CBORFactory()).RegisterModule(new Jdk8Module());
            ObjectWriter objectWriter = objectMapper.WriterFor(typeof(Envelope)).WithAttribute("request_type", "read_state");
            Envelope<ReadStateContent> envelope = new Envelope<ReadStateContent>();
            envelope.content = request;
            envelope.senderPubkey = signature.publicKey;
            envelope.senderSig = signature.signature;
            byte[] bytes = null;
            try
            {
                bytes = objectWriter.WriteValueAsBytes(envelope);
            }
            catch (JsonProcessingException e)
            {
                throw AgentError.Create(AgentError.AgentErrorCode.INVALID_CBOR_DATA, e, envelope);
            }

            CompletableFuture<StateResponse<T>> response = new CompletableFuture<StateResponse<T>>();
            transport.ReadState(effectiveCanisterId, bytes, headers).WhenComplete((input, ex) =>
            {
                if (ex == null)
                {
                    if (input != null)
                    {
                        try
                        {
                            T readStateResponse = objectMapper.ReadValue(input.payload, clazz);
                            StateResponse<T> stateResponse = new StateResponse<T>();
                            stateResponse.state = readStateResponse;
                            stateResponse.headers = input.headers;
                            response.Complete(stateResponse);
                        }
                        catch (IOException e)
                        {
                            LOG.Debug(e.GetLocalizedMessage(), e);
                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.MESSAGE_ERROR, e, new string (input.payload, StandardCharsets.UTF_8)));
                        }
                    }
                    else
                    {
                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.TRANSPORT_ERROR, "Payload is empty"));
                    }
                }
                else
                {
                    response.CompleteExceptionally(ex);
                }
            });
            return response;
        }

        public class UpdateResponse
        {
            public RequestId requestId;
            public Map<string, string> headers;
        }

        public class StateResponse<T>
        {
            public T state;
            public Map<string, string> headers;
        }

        public class CertificateResponse
        {
            public Certificate certificate;
            public Map<string, string> headers;
        }
    }
}