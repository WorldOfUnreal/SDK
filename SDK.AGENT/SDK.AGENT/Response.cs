using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public sealed class Response<T>
    {
        public static readonly string X_IC_NODE_ID_HEADER = "x-ic-node-id";
        public static readonly string X_IC_SUBNET_ID_HEADER = "x-ic-subnet-id";
        public static readonly string X_IC_CANISTER_ID_HEADER = "x-ic-canister-id";
        Map<string, string> headers;
        T payload;
        public Response(T payload, Map<string, string> headers)
        {
            this.headers = headers;
            this.payload = payload;
        }

        public Response(T payload)
        {
            this.headers = new HashMap<string, string>();
            this.payload = payload;
        }

        /// <returns>the headers</returns>
        public Map<string, string> GetHeaders()
        {
            return headers;
        }

        /// <returns>the payload</returns>
        public T GetPayload()
        {
            return payload;
        }
    }
}