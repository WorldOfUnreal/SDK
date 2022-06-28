using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Http
{
    public class ReplicaApacheHttpTransport : IReplicaTransport
    {
        protected static readonly Logger LOG = LoggerFactory.GetLogger(typeof(ReplicaApacheHttpTransport));
        readonly IOReactorConfig ioReactorConfig;
        readonly CloseableHttpAsyncClient client;
        URI uri;
        private readonly ContentType dfinityContentType = ContentType.Create(ReplicaHttpProperties.DFINITY_CONTENT_TYPE);
        ReplicaApacheHttpTransport(URI url)
        {
            this.uri = url;
            ioReactorConfig = IOReactorConfig.Custom().SetSoTimeout(Timeout.OfSeconds(ReplicaHttpProperties.TIMEOUT)).Build();
            client = HttpAsyncClients.Custom().SetIOReactorConfig(ioReactorConfig).Build();
        }

        ReplicaApacheHttpTransport(URI url, int maxTotal, int maxPerRoute, int connectionTimeToLive, int timeout)
        {
            this.uri = url;
            PoolingAsyncClientConnectionManager connectionManager = PoolingAsyncClientConnectionManagerBuilder.Create().SetPoolConcurrencyPolicy(PoolConcurrencyPolicy.STRICT).SetConnPoolPolicy(PoolReusePolicy.LIFO).SetConnectionTimeToLive(TimeValue.OfSeconds(connectionTimeToLive)).SetMaxConnTotal(maxTotal).SetMaxConnPerRoute(maxPerRoute).Build();
            ioReactorConfig = IOReactorConfig.Custom().SetSoTimeout(Timeout.OfSeconds(timeout)).Build();
            client = HttpAsyncClients.Custom().SetConnectionManager(connectionManager).SetIOReactorConfig(ioReactorConfig).Build();
        }

        ReplicaApacheHttpTransport(URI url, AsyncClientConnectionManager connectionManager, int timeout)
        {
            this.uri = url;
            ioReactorConfig = IOReactorConfig.Custom().SetSoTimeout(Timeout.OfSeconds(timeout)).Build();
            client = HttpAsyncClients.Custom().SetConnectionManager(connectionManager).SetIOReactorConfig(ioReactorConfig).Build();
        }

        public static ReplicaTransport Create(string url)
        {
            return new ReplicaApacheHttpTransport(new URI(url));
        }

        public static ReplicaTransport Create(string url, int maxTotal, int maxPerRoute, int connectionTimeToLive, int timeout)
        {
            return new ReplicaApacheHttpTransport(new URI(url), maxTotal, maxPerRoute, connectionTimeToLive, timeout);
        }

        public static ReplicaTransport Create(string url, AsyncClientConnectionManager connectionManager, int timeout)
        {
            return new ReplicaApacheHttpTransport(new URI(url), connectionManager, timeout);
        }

        public virtual CompletableFuture<ReplicaResponse> Status()
        {
            HttpHost target = HttpHost.Create(uri);
            SimpleHttpRequest httpRequest = new SimpleHttpRequest(Method.GET, target, ReplicaHttpProperties.API_VERSION_URL_PART + ReplicaHttpProperties.STATUS_URL_PART);
            return this.Execute(httpRequest, Optional.Empty());
        }

        public virtual CompletableFuture<ReplicaResponse> Query(Principal containerId, byte[] envelope, Map<string, string> headers)
        {
            HttpHost target = HttpHost.Create(uri);
            SimpleHttpRequest httpRequest = new SimpleHttpRequest(Method.POST, target, ReplicaHttpProperties.API_VERSION_URL_PART + String.Format(ReplicaHttpProperties.QUERY_URL_PART, containerId.ToString()));
            if (headers != null)
            {
                Iterator<string> names = headers.KeySet().Iterator();
                while (names.HasNext())
                {
                    string name = names.Next();
                    httpRequest.AddHeader(name, headers[name]);
                }
            }

            return this.Execute(httpRequest, Optional.Of(envelope));
        }

        public virtual CompletableFuture<ReplicaResponse> Call(Principal containerId, byte[] envelope, RequestId requestId, Map<string, string> headers)
        {
            HttpHost target = HttpHost.Create(uri);
            SimpleHttpRequest httpRequest = new SimpleHttpRequest(Method.POST, target, ReplicaHttpProperties.API_VERSION_URL_PART + String.Format(ReplicaHttpProperties.CALL_URL_PART, containerId.ToString()));
            if (headers != null)
            {
                Iterator<string> names = headers.KeySet().Iterator();
                while (names.HasNext())
                {
                    string name = names.Next();
                    httpRequest.AddHeader(name, headers[name]);
                }
            }

            return this.Execute(httpRequest, Optional.Of(envelope));
        }

        public virtual CompletableFuture<ReplicaResponse> ReadState(Principal containerId, byte[] envelope, Map<string, string> headers)
        {
            HttpHost target = HttpHost.Create(uri);
            SimpleHttpRequest httpRequest = new SimpleHttpRequest(Method.POST, target, ReplicaHttpProperties.API_VERSION_URL_PART + String.Format(ReplicaHttpProperties.READ_STATE_URL_PART, containerId.ToString()));
            if (headers != null)
            {
                Iterator<string> names = headers.KeySet().Iterator();
                while (names.HasNext())
                {
                    string name = names.Next();
                    httpRequest.AddHeader(name, headers[name]);
                }
            }

            return this.Execute(httpRequest, Optional.Of(envelope));
        }

        virtual CompletableFuture<ReplicaResponse> Execute(SimpleHttpRequest httpRequest, Optional<byte[]> payload)
        {
            try
            {
                client.Start();
                URI requestUri = httpRequest.GetUri();
                LOG.Debug("Executing request " + httpRequest.GetMethod() + " " + requestUri);
                if (payload.IsPresent())
                    httpRequest.SetBody(payload.Get(), dfinityContentType);
                else
                    httpRequest.SetHeader(HttpHeaders.CONTENT_TYPE, ReplicaHttpProperties.DFINITY_CONTENT_TYPE);
                CompletableFuture<ReplicaResponse> response = new CompletableFuture<ReplicaResponse>();
                client.Execute(httpRequest, new AnonymousFutureCallback(this));
                return response;
            }
            catch (URISyntaxException e)
            {
                throw AgentError.Create(AgentError.AgentErrorCode.URL_PARSE_ERROR, e);
            }
        }

        private sealed class AnonymousFutureCallback : FutureCallback
        {
            public AnonymousFutureCallback(ReplicaApacheHttpTransport parent)
            {
                this.parent = parent;
            }

            private readonly ReplicaApacheHttpTransport parent;
            public override void Completed(SimpleHttpResponse httpResponse)
            {
                LOG.Debug(requestUri + "->" + httpResponse.GetCode());
                ReplicaResponse replicaResponse = new ReplicaResponse();
                byte[] bytes = httpResponse.GetBodyBytes();
                replicaResponse.headers = new HashMap<string, string>();
                Header[] headers = httpResponse.GetHeaders();
                foreach (Header header in headers)
                    replicaResponse.headers.Put(header.GetName(), header.GetValue());
                if (bytes == null)
                    bytes = ArrayUtils.EMPTY_BYTE_ARRAY;
                replicaResponse.payload = bytes;
                response.Complete(replicaResponse);
            }

            public override void Failed(Exception ex)
            {
                LOG.Debug(requestUri + "->" + ex);
                response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.HTTP_ERROR, ex, ex.GetLocalizedMessage()));
            }

            public override void Cancelled()
            {
                LOG.Debug(requestUri + " cancelled");
                response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.TRANSPORT_ERROR, requestUri));
            }
        }
    }
}