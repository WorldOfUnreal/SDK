using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Http
{
    class ReplicaHttpProperties
    {
        static readonly string DFINITY_CONTENT_TYPE = "application/cbor";
        static readonly string API_VERSION_URL_PART = "/api/v2/";
        static readonly string STATUS_URL_PART = "status";
        static readonly string QUERY_URL_PART = "canister/%s/query";
        static readonly string CALL_URL_PART = "canister/%s/call";
        static readonly string READ_STATE_URL_PART = "canister/%s/read_state";
        static readonly string CONTENT_TYPE = "Content-Type";
        static readonly int TIMEOUT = 5;
        static readonly long CONNECTION_TTL = 1L;
    }
}