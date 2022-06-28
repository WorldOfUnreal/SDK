using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Replicaapi
{
    public sealed class Status
    {
        static readonly string UNKNOWN_STATUS_VALUE = "unknown";
        static readonly string RECEIVED_STATUS_VALUE = "received";
        static readonly string PROCESSING_STATUS_VALUE = "processing";
        static readonly string REJECTED_STATUS_VALUE = "rejected";
        static readonly string REPLIED_STATUS_VALUE = "replied";
        static readonly string DONE_STATUS_VALUE = "done";
        public InnerStatus status;
        public Optional<CallReply> replied;
        public Optional<Rejected> rejected;
        void SetStatus(JsonNode statusNode)
        {
            if (statusNode != null && statusNode.IsTextual())
            {
                switch (statusNode.AsText())
                {
                    case UNKNOWN_STATUS_VALUE:
                        this.status = InnerStatus.UNKNOWN_STATUS;
                        break;
                    case RECEIVED_STATUS_VALUE:
                        this.status = InnerStatus.RECEIVED_STATUS;
                        break;
                    case PROCESSING_STATUS_VALUE:
                        this.status = InnerStatus.PROCESSING_STATUS;
                        break;
                    case REJECTED_STATUS_VALUE:
                    {
                        this.rejected = Optional.Of(new Rejected());
                        this.replied = Optional.Empty();
                        this.status = InnerStatus.REJECTED_STATUS;
                        break;
                    }

                    case REPLIED_STATUS_VALUE:
                    {
                        this.replied = Optional.Of(new CallReply());
                        this.rejected = Optional.Empty();
                        this.status = InnerStatus.REPLIED_STATUS;
                        break;
                    }

                    case DONE_STATUS_VALUE:
                        this.status = InnerStatus.DONE_STATUS;
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
            // UNKNOWN_STATUS(UNKNOWN_STATUS_VALUE)
            UNKNOWN_STATUS,
            // RECEIVED_STATUS(RECEIVED_STATUS_VALUE)
            RECEIVED_STATUS,
            // PROCESSING_STATUS(PROCESSING_STATUS_VALUE)
            PROCESSING_STATUS,
            // REJECTED_STATUS(REJECTED_STATUS_VALUE)
            REJECTED_STATUS,
            // REPLIED_STATUS(REPLIED_STATUS_VALUE)
            REPLIED_STATUS,
            // DONE_STATUS(DONE_STATUS_VALUE)
            DONE_STATUS 

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