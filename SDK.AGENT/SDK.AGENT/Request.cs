using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public sealed class Request<T>
    {
        Map<string, string> headers;
        T payload;
        public Request(T payload, Map<string, string> headers)
        {
            this.headers = headers;
            this.payload = payload;
        }

        public Request(T payload)
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