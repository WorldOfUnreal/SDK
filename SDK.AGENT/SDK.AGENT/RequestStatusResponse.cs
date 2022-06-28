using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public sealed class RequestStatusResponse
    {
        public static readonly string UNKNOWN_STATUS_VALUE = "unknown";
        public static readonly string RECEIVED_STATUS_VALUE = "received";
        public static readonly string PROCESSING_STATUS_VALUE = "processing";
        public static readonly string REJECTED_STATUS_VALUE = "rejected";
        public static readonly string REPLIED_STATUS_VALUE = "replied";
        public static readonly string DONE_STATUS_VALUE = "done";
        public InnerStatus status;
        public Optional<CallReply> replied;
        public Optional<Rejected> rejected;
        RequestStatusResponse(InnerStatus status)
        {
            this.status = status;
        }

        RequestStatusResponse(CallReply replied)
        {
            this.status = InnerStatus.REPLIED_STATUS;
            this.replied = Optional.Of(replied);
        }

        RequestStatusResponse(int rejectCode, string rejectMessage)
        {
            this.status = InnerStatus.REJECTED_STATUS;
            this.rejected = Optional.Of(new Rejected(rejectCode, rejectMessage));
        }

        public sealed class Rejected
        {
            public int rejectCode;
            public string rejectMessage;
            Rejected(int rejectCode, string rejectMessage)
            {
                this.rejectCode = rejectCode;
                this.rejectMessage = rejectMessage;
            }
        }

        public enum InnerStatus
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
            // public String toString() {
            //     return this.value;
            // }
            // --------------------
        }
    }
}