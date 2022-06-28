using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Replicaapi
{
    abstract class Response
    {
        public Map<string, string> headers;
        /// <returns>the headers</returns>
        public virtual Map<string, string> GetHeaders()
        {
            return headers;
        }

        /// <param name="headers">the headers to set</param>
        public virtual void SetHeaders(Map<string, string> headers)
        {
            this.headers = headers;
        }
    }
}