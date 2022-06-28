using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Replicaapi
{
    public sealed class QueryResponse : Response
    {
        static readonly string REJECTED_STATUS_VALUE = "rejected";
        static readonly string REPLIED_STATUS_VALUE = "replied";
        public InnerStatus status;
        public Optional<CallReply> replied;
        public Optional<Rejected> rejected;
        void SetStatus(JsonNode statusNode)
        {
            if (statusNode != null && statusNode.IsTextual())
            {
                if (REJECTED_STATUS_VALUE.Equals(statusNode.AsText()))
                {
                    this.rejected = Optional.Of(new Rejected());
                    this.replied = Optional.Empty();
                    this.status = InnerStatus.REJECTED_STATUS;
                }
                else if (REPLIED_STATUS_VALUE.Equals(statusNode.AsText()))
                {
                    this.replied = Optional.Of(new CallReply());
                    this.rejected = Optional.Empty();
                    this.status = InnerStatus.REPLIED_STATUS;
                }
            }
        }

        void SetRejectCode(JsonNode rejectCodeNode)
        {
            if (rejectCodeNode != null && rejectCodeNode.IsInt())
            {
                if (this.rejected.IsPresent())
                    this.rejected.Get().rejectCode = rejectCodeNode.AsInt();
            }
        }

        void SetRejectMessage(JsonNode rejectMessageNode)
        {
            if (rejectMessageNode != null && rejectMessageNode.IsTextual())
            {
                if (this.rejected.IsPresent())
                    this.rejected.Get().rejectMessage = rejectMessageNode.AsText();
            }
        }

        void SetReply(JsonNode replyNode)
        {
            if (replyNode != null && replyNode.Has("arg"))
            {
                JsonNode argNode = replyNode["arg"];
                if (this.replied.IsPresent())
                    try
                    {
                        this.replied.Get().arg = argNode.BinaryValue();
                    }
                    catch (IOException e)
                    {
                        throw AgentError.Create(AgentError.AgentErrorCode.INVALID_CBOR_DATA, e);
                    }
            }
        }

        public sealed class Rejected
        {
            public int rejectCode;
            public string rejectMessage;
        }

        enum InnerStatus
        {
            // REJECTED_STATUS(REJECTED_STATUS_VALUE)
            REJECTED_STATUS,
            // REPLIED_STATUS(REPLIED_STATUS_VALUE)
            REPLIED_STATUS 

            // --------------------
            // TODO enum body members
            // String value;
            // InnerStatus(String value) {
            //     this.value = value;
            // }
            // --------------------
        }
    }
}