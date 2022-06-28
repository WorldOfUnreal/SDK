using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public sealed class ProxyBuilder
    {
        static readonly Logger LOG = LoggerFactory.GetLogger(typeof(ProxyBuilder));
        static int WAITER_TIMEOUT = 60;
        static int WAITER_SLEEP = 5;
        Agent agent;
        Principal effectiveCanisterId;
        Principal canisterId;
        Optional<long> ingressExpiryDatetime;
        private Waiter waiter;
        ProxyBuilder(Agent agent, Principal canisterId)
        {
            Security.AddProvider(new BouncyCastleProvider());
            this.agent = agent;
            this.canisterId = canisterId;
            this.effectiveCanisterId = canisterId.Clone();
            this.ingressExpiryDatetime = Optional.Empty();
        }

        ProxyBuilder(Principal canisterId)
        {
            Security.AddProvider(new BouncyCastleProvider());
            this.canisterId = canisterId;
            this.effectiveCanisterId = canisterId.Clone();
            this.ingressExpiryDatetime = Optional.Empty();
        }

        ProxyBuilder()
        {
            Security.AddProvider(new BouncyCastleProvider());
            this.ingressExpiryDatetime = Optional.Empty();
        }

        public static ProxyBuilder Create(Agent agent, Principal canisterId)
        {
            return new ProxyBuilder(agent, canisterId);
        }

        public static ProxyBuilder Create(Principal canisterId)
        {
            return new ProxyBuilder(canisterId);
        }

        public static ProxyBuilder Create()
        {
            return new ProxyBuilder();
        }

        public ProxyBuilder EffectiveCanisterId(Principal effectiveCanisterId)
        {
            this.effectiveCanisterId = effectiveCanisterId;
            return this;
        }

        public ProxyBuilder ExpireAt(LocalDateTime time)
        {
            this.ingressExpiryDatetime = Optional.Of(time.ToEpochSecond(ZoneOffset.UTC));
            return this;
        }

        public ProxyBuilder ExpireAfter(Duration duration)
        {
            Duration permittedDrift = Duration.OfSeconds(Agent.DEFAULT_PERMITTED_DRIFT);
            this.ingressExpiryDatetime = Optional.Of((Duration.OfMillis(System.CurrentTimeMillis()).Plus(duration).Minus(permittedDrift)).ToNanos());
            return this;
        }

        public ProxyBuilder Waiter(Waiter waiter)
        {
            this.waiter = waiter;
            return this;
        }

        public T GetProxy<T>(Class<T> interfaceClass)
        {
            Agent agent = this.agent;
            if (agent == null)
            {
                if (interfaceClass.IsAnnotationPresent(typeof(org.ic4j.agent.annotations.Agent)))
                {
                    org.ic4j.agent.annotations.Agent agentAnnotation = interfaceClass.GetAnnotation(typeof(org.ic4j.agent.annotations.Agent));
                    Transport transportAnnotation = agentAnnotation.Transport();
                    Identity identity = new AnonymousIdentity();
                    org.ic4j.agent.annotations.Identity identityAnnotation = agentAnnotation.Identity();
                    try
                    {
                        ReplicaTransport transport = ReplicaApacheHttpTransport.Create(transportAnnotation.Url());
                        switch (identityAnnotation.Type())
                        {
                            case ANONYMOUS:
                                identity = new AnonymousIdentity();
                                break;
                            case BASIC:
                                if ("".Equals(identityAnnotation.Pem_file()))
                                {
                                    KeyPair keyPair = KeyPairGenerator.GetInstance("Ed25519").GenerateKeyPair();
                                    identity = BasicIdentity.FromKeyPair(keyPair);
                                }
                                else
                                {
                                    Path path = Paths[identityAnnotation.Pem_file()];
                                    identity = BasicIdentity.FromPEMFile(path);
                                }

                                break;
                            case SECP256K1:
                                Path path = Paths[identityAnnotation.Pem_file()];
                                identity = Secp256k1Identity.FromPEMFile(path);
                                break;
                        }

                        agent = new AgentBuilder().Transport(transport).Identity(identity).Build();
                    }
                    catch (URISyntaxException e)
                    {
                        throw AgentError.Create(AgentError.AgentErrorCode.INVALID_REPLICA_URL, e, transportAnnotation.Url());
                    }
                    catch (NoSuchAlgorithmException e)
                    {
                        throw PemError.Create(PemError.PemErrorCode.PEM_ERROR, e, identityAnnotation.Pem_file());
                    }
                }
                else
                    throw AgentError.Create(AgentError.AgentErrorCode.MISSING_REPLICA_TRANSPORT);
            }

            Principal canisterId = this.canisterId;
            Principal effectiveCanisterId = this.effectiveCanisterId;
            if (effectiveCanisterId == null)
            {
                if (interfaceClass.IsAnnotationPresent(typeof(EffectiveCanister)))
                {
                    EffectiveCanister effectiveCanister = interfaceClass.GetAnnotation(typeof(EffectiveCanister));
                    effectiveCanisterId = Principal.FromString(effectiveCanister.Value());
                }
            }

            if (canisterId == null)
            {
                if (interfaceClass.IsAnnotationPresent(typeof(Canister)))
                {
                    Canister canister = interfaceClass.GetAnnotation(typeof(Canister));
                    canisterId = Principal.FromString(canister.Value());
                    if (effectiveCanisterId == null)
                        effectiveCanisterId = canisterId.Clone();
                }
            }

            AgentInvocationHandler agentInvocationHandler = new AgentInvocationHandler(agent, canisterId, effectiveCanisterId, this.ingressExpiryDatetime, waiter);
            T proxy = (T)Proxy.NewProxyInstance(interfaceClass.GetClassLoader(), new Class{interfaceClass}, agentInvocationHandler);
            return proxy;
        }

        class AgentInvocationHandler : IInvocationHandler
        {
            Agent agent;
            Principal canisterId;
            Principal effectiveCanisterId;
            Optional<long> ingressExpiryDatetime;
            Waiter waiter;
            AgentInvocationHandler(Agent agent, Principal canisterId, Principal effectiveCanisterId, Optional<long> ingressExpiryDatetime, Waiter waiter)
            {
                this.agent = agent;
                this.canisterId = canisterId;
                this.effectiveCanisterId = effectiveCanisterId;
                this.ingressExpiryDatetime = ingressExpiryDatetime;
                this.waiter = waiter;
            }

            public override object Invoke(object proxy, Method method, Object[] args)
            {
                if (method.IsAnnotationPresent(typeof(QUERY)) || method.IsAnnotationPresent(typeof(UPDATE)))
                {
                    MethodType methodType = null;
                    string methodName = method.GetName();
                    if (method.IsAnnotationPresent(typeof(QUERY)))
                        methodType = MethodType.QUERY;
                    else if (method.IsAnnotationPresent(typeof(UPDATE)))
                        methodType = MethodType.UPDATE;
                    if (method.IsAnnotationPresent(typeof(Name)))
                    {
                        Name nameAnnotation = method.GetAnnotation(typeof(Name));
                        methodName = nameAnnotation.Value();
                    }

                    Parameter[] parameters = method.GetParameters();
                    List<IDLValue> candidArgs = new List<IDLValue>();
                    PojoSerializer pojoSerializer = new PojoSerializer();
                    if (args != null)
                        for (int i = 0; i < args.length; i++)
                        {
                            object arg = args[i];
                            Argument argumentAnnotation = null;
                            bool skip = false;
                            foreach (Annotation annotation in method.GetParameterAnnotations()[i])
                            {
                                if (typeof(Ignore).IsInstance(annotation))
                                {
                                    skip = true;
                                    continue;
                                }

                                if (typeof(Argument).IsInstance(annotation))
                                    argumentAnnotation = (Argument)annotation;
                            }

                            if (skip)
                                continue;
                            {
                                if (argumentAnnotation != null)
                                {
                                    Type type = argumentAnnotation.Value();
                                    IDLType idlType;
                                    if (parameters[i].GetType().IsArray())
                                        idlType = IDLType.CreateType(Type.VEC, IDLType.CreateType(type));
                                    else
                                        idlType = IDLType.CreateType(type);
                                    IDLValue idlValue = IDLValue.Create(arg, pojoSerializer, idlType);
                                    candidArgs.Add(idlValue);
                                }
                                else
                                    candidArgs.Add(IDLValue.Create(arg, pojoSerializer));
                            }
                        }

                    IDLArgs idlArgs = IDLArgs.Create(candidArgs);
                    byte[] buf = idlArgs.ToBytes();
                    PojoDeserializer pojoDeserializer = new PojoDeserializer();
                    switch (methodType)
                    {
                        case QUERY:
                        {
                            QueryBuilder queryBuilder = QueryBuilder.Create(agent, this.canisterId, methodName);
                            queryBuilder.effectiveCanisterId = this.effectiveCanisterId;
                            queryBuilder.ingressExpiryDatetime = this.ingressExpiryDatetime;
                            CompletableFuture<Response<byte[]>> builderResponse = queryBuilder.Arg(buf).Call(null);
                            try
                            {
                                if (method.GetReturnType().Equals(typeof(CompletableFuture)))
                                {
                                    CompletableFuture<object> response = new CompletableFuture();
                                    builderResponse.WhenComplete((input, ex) =>
                                    {
                                        if (ex == null)
                                        {
                                            if (input != null)
                                            {
                                                IDLArgs outArgs = IDLArgs.FromBytes(input.GetPayload());
                                                if (outArgs.GetArgs().IsEmpty())
                                                    response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, "Missing return value"));
                                                else
                                                {
                                                    if (method.IsAnnotationPresent(typeof(ResponseClass)))
                                                    {
                                                        Class<TWildcardTodo> responseClass = method.GetAnnotation(typeof(ResponseClass)).Value();
                                                        if (responseClass.IsAssignableFrom(typeof(IDLArgs)))
                                                            response.Complete(outArgs);
                                                        else if (responseClass.IsAssignableFrom(typeof(Response)))
                                                            response.Complete(input);
                                                        else
                                                            response.Complete(outArgs.GetArgs()[0].GetValue(pojoDeserializer, responseClass));
                                                    }
                                                    else
                                                        response.Complete(outArgs.GetArgs()[0].GetValue());
                                                }
                                            }
                                            else
                                                response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, "Missing return value"));
                                        }
                                        else
                                            response.CompleteExceptionally(ex);
                                    });
                                    return response;
                                }
                                else
                                {
                                    if (method.GetReturnType().Equals(typeof(Response)))
                                        return builderResponse.Get();
                                    byte[] output = builderResponse.Get().GetPayload();
                                    IDLArgs outArgs = IDLArgs.FromBytes(output);
                                    if (method.GetReturnType().Equals(typeof(IDLArgs)))
                                        return outArgs;
                                    if (outArgs.GetArgs().IsEmpty())
                                        throw AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, "Missing return value");
                                    return outArgs.GetArgs()[0].GetValue(pojoDeserializer, method.GetReturnType());
                                }
                            }
                            catch (Exception e)
                            {
                                throw AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, e, e.GetLocalizedMessage());
                            }
                        }

                        case UPDATE:
                        {
                            UpdateBuilder updateBuilder = UpdateBuilder.Create(this.agent, this.canisterId, methodName);
                            updateBuilder.effectiveCanisterId = this.effectiveCanisterId;
                            updateBuilder.ingressExpiryDatetime = this.ingressExpiryDatetime;
                            CompletableFuture<object> response = new CompletableFuture<object>();
                            Waiter waiter = this.waiter;
                            if (waiter == null)
                            {
                                if (method.IsAnnotationPresent(typeof(org.ic4j.agent.annotations.Waiter)))
                                {
                                    org.ic4j.agent.annotations.Waiter waiterAnnotation = method.GetAnnotation(typeof(org.ic4j.agent.annotations.Waiter));
                                    waiter = Waiter.Create(waiterAnnotation.Timeout(), waiterAnnotation.Sleep());
                                }
                                else
                                    waiter = Waiter.Create(WAITER_TIMEOUT, WAITER_SLEEP);
                            }

                            CompletableFuture<Response<RequestId>> requestResponse = updateBuilder.Arg(buf).Call(null);
                            CompletableFuture<Response<byte[]>> builderResponse = updateBuilder.GetState(requestResponse.Get().GetPayload(), null, waiter);
                            builderResponse.WhenComplete((input, ex) =>
                            {
                                if (ex == null)
                                {
                                    if (input != null)
                                    {
                                        IDLArgs outArgs = IDLArgs.FromBytes(input.GetPayload());
                                        if (outArgs.GetArgs().IsEmpty())
                                            response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, "Missing return value"));
                                        else
                                        {
                                            if (method.IsAnnotationPresent(typeof(ResponseClass)))
                                            {
                                                Class<TWildcardTodo> responseClass = method.GetAnnotation(typeof(ResponseClass)).Value();
                                                if (responseClass.IsAssignableFrom(typeof(IDLArgs)))
                                                    response.Complete(outArgs);
                                                else if (responseClass.IsAssignableFrom(typeof(Response)))
                                                    response.Complete(input);
                                                else
                                                    response.Complete(outArgs.GetArgs()[0].GetValue(pojoDeserializer, responseClass));
                                            }
                                            else
                                                response.Complete(outArgs.GetArgs()[0].GetValue());
                                        }
                                    }
                                    else
                                        response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, "Missing return value"));
                                }
                                else
                                    response.CompleteExceptionally(ex);
                            });
                            return response;
                        }

                        default:
                            throw AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, "Invalid Candid method type");
                            break;
                    }
                }
                else
                    throw AgentError.Create(AgentError.AgentErrorCode.CUSTOM_ERROR, "Candid method type not defined");
            }
        }
    }
}