using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Http
{
    public class ReplicaOkHttpTransport : IReplicaTransport
    {
        protected static readonly Logger LOG = LoggerFactory.GetLogger(typeof(ReplicaOkHttpTransport));
        readonly OkHttpClient client;
        URI uri;
        private readonly MediaType dfinityContentType = MediaType.Parse(ReplicaHttpProperties.DFINITY_CONTENT_TYPE);
        ReplicaOkHttpTransport(URI url)
        {

            //check if url ends with /	
            if ('/' == url.ToString().CharAt(url.ToString().Length() - 1))
                this.uri = URI.Create(url.ToString().Substring(0, url.ToString().Length() - 1));
            else
                this.uri = url;
            client = new OkHttpClient();
        }

        ReplicaOkHttpTransport(URI url, int timeout)
        {

            //check if url ends with /	
            if ('/' == url.ToString().CharAt(url.ToString().Length() - 1))
                this.uri = URI.Create(url.ToString().Substring(0, url.ToString().Length() - 1));
            else
                this.uri = url;
            client = new Builder().ReadTimeout(timeout, TimeUnit.SECONDS).Build();
        }

        //check if url ends with /	
        //check if url ends with /	
        public static ReplicaTransport Create(string url)
        {
            return new ReplicaOkHttpTransport(new URI(url));
        }

        public static ReplicaTransport Create(string url, int timeout)
        {
            return new ReplicaOkHttpTransport(new URI(url), timeout);
        }

        public virtual CompletableFuture<ReplicaResponse> Status()
        {
            Request httpRequest = new Builder().Url(uri.ToString() + ReplicaHttpProperties.API_VERSION_URL_PART + ReplicaHttpProperties.STATUS_URL_PART).Get().AddHeader(ReplicaHttpProperties.CONTENT_TYPE, ReplicaHttpProperties.DFINITY_CONTENT_TYPE).Build();
            return this.Execute(httpRequest);
        }

        public virtual CompletableFuture<ReplicaResponse> Query(Principal containerId, byte[] envelope, Map<string, string> headers)
        {
            RequestBody requestBody = RequestBody.Create(envelope, dfinityContentType);
            Builder builder = new Builder().Url(uri.ToString() + ReplicaHttpProperties.API_VERSION_URL_PART + String.Format(ReplicaHttpProperties.QUERY_URL_PART, containerId.ToString())).Post(requestBody);
            if (headers != null)
            {
                Iterator<string> names = headers.KeySet().Iterator();
                while (names.HasNext())
                {
                    string name = names.Next();
                    builder.AddHeader(name, headers[name]);
                }
            }

            Request httpRequest = builder.Build();
            return this.Execute(httpRequest);
        }

        public virtual CompletableFuture<ReplicaResponse> Call(Principal containerId, byte[] envelope, RequestId requestId, Map<string, string> headers)
        {
            RequestBody requestBody = RequestBody.Create(envelope, dfinityContentType);
            Builder builder = new Builder().Url(uri.ToString() + ReplicaHttpProperties.API_VERSION_URL_PART + String.Format(ReplicaHttpProperties.CALL_URL_PART, containerId.ToString())).Post(requestBody);
            if (headers != null)
            {
                Iterator<string> names = headers.KeySet().Iterator();
                while (names.HasNext())
                {
                    string name = names.Next();
                    builder.AddHeader(name, headers[name]);
                }
            }

            Request httpRequest = builder.Build();
            return this.Execute(httpRequest);
        }

        public virtual CompletableFuture<ReplicaResponse> ReadState(Principal containerId, byte[] envelope, Map<string, string> headers)
        {
            RequestBody requestBody = RequestBody.Create(envelope, dfinityContentType);
            Builder builder = new Builder().Url(uri.ToString() + ReplicaHttpProperties.API_VERSION_URL_PART + String.Format(ReplicaHttpProperties.READ_STATE_URL_PART, containerId.ToString())).Post(requestBody);
            if (headers != null)
            {
                Iterator<string> names = headers.KeySet().Iterator();
                while (names.HasNext())
                {
                    string name = names.Next();
                    builder.AddHeader(name, headers[name]);
                }
            }

            Request httpRequest = builder.Build();
            return this.Execute(httpRequest);
        }

        virtual CompletableFuture<ReplicaResponse> Execute(Request httpRequest)
        {
            try
            {
                URI requestUri = httpRequest.Url().Uri();
                LOG.Debug("Executing request " + httpRequest.Method() + " " + requestUri);
                CompletableFuture<ReplicaResponse> response = new CompletableFuture<ReplicaResponse>();
                Call call = client.NewCall(httpRequest);
                call.Enqueue(new AnonymousCallback(this));
                return response;
            }
            catch (Exception e)
            {
                throw AgentError.Create(AgentError.AgentErrorCode.URL_PARSE_ERROR, e);
            }
        }

        private sealed class AnonymousCallback : Callback
        {
            public AnonymousCallback(ReplicaOkHttpTransport parent)
            {
                this.parent = parent;
            }

            private readonly ReplicaOkHttpTransport parent;
            public override void OnResponse(Call call, Response httpResponse)
            {
                LOG.Debug(requestUri + "->" + httpResponse.Code());
                byte[] bytes;
                try
                {
                    ReplicaResponse replicaResponse = new ReplicaResponse();
                    replicaResponse.headers = new HashMap<string, string>();
                    Headers headers = httpResponse.Headers();
                    Iterator<string> names = headers.Names().Iterator();
                    while (names.HasNext())
                    {
                        string name = names.Next();
                        replicaResponse.headers.Put(name, headers[name]);
                    }

                    bytes = httpResponse.Body().Bytes();
                    if (bytes == null)
                        bytes = ArrayUtils.EMPTY_BYTE_ARRAY;
                    replicaResponse.payload = bytes;
                    response.Complete(replicaResponse);
                }
                catch (IOException e)
                {
                    response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.HTTP_ERROR, e, e.GetLocalizedMessage()));
                }
            }

            public override void OnFailure(Call call, IOException ex)
            {
                LOG.Debug(requestUri + "->" + ex);
                response.CompleteExceptionally(AgentError.Create(AgentError.AgentErrorCode.HTTP_ERROR, ex, ex.GetLocalizedMessage()));
            }
        }
    }
}