using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public sealed class ResponseAuthentication
    {
        static RequestStatusResponse LookupRequestStatus(Certificate certificate, RequestId requestId)
        {
            IList<Label> pathStatus = new List<Label>();
            pathStatus.Add(new Label("request_status"));
            pathStatus.Add(new Label(requestId.Get()));
            pathStatus.Add(new Label("status"));
            LookupResult result = certificate.tree.LookupPath(pathStatus);
            switch (result.status)
            {
                case ABSENT:
                    throw AgentError.Create(AgentError.AgentErrorCode.LOOKUP_PATH_ABSENT, pathStatus);
                case UNKNOWN:
                    return new RequestStatusResponse(RequestStatusResponse.InnerStatus.UNKNOWN_STATUS);
                case FOUND:
                {
                    string status = new string (result.value, StandardCharsets.UTF_8);
                    switch (status)
                    {
                        case RequestStatusResponse.DONE_STATUS_VALUE:
                            return new RequestStatusResponse(RequestStatusResponse.InnerStatus.DONE_STATUS);
                        case RequestStatusResponse.PROCESSING_STATUS_VALUE:
                            return new RequestStatusResponse(RequestStatusResponse.InnerStatus.PROCESSING_STATUS);
                        case RequestStatusResponse.RECEIVED_STATUS_VALUE:
                            return new RequestStatusResponse(RequestStatusResponse.InnerStatus.RECEIVED_STATUS);
                        case RequestStatusResponse.REJECTED_STATUS_VALUE:
                            return LookupRejection(certificate, requestId);
                        case RequestStatusResponse.REPLIED_STATUS_VALUE:
                            return LookupReply(certificate, requestId);
                        default:
                            throw AgentError.Create(AgentError.AgentErrorCode.INVALID_REQUEST_STATUS, pathStatus, status);
                            break;
                    }
                }

                default:
                    throw AgentError.Create(AgentError.AgentErrorCode.LOOKUP_PATH_ERROR, pathStatus);
                    break;
            }
        }

        static RequestStatusResponse LookupRejection(Certificate certificate, RequestId requestId)
        {
            int rejectCode = LookupRejectCode(certificate, requestId);
            string rejectMessage = LookupRejectMessage(certificate, requestId);
            return new RequestStatusResponse(rejectCode, rejectMessage);
        }

        static int LookupRejectCode(Certificate certificate, RequestId requestId)
        {
            IList<Label> path = new List<Label>();
            path.Add(new Label("request_status"));
            path.Add(new Label(requestId.Get()));
            path.Add(new Label("reject_code"));
            byte[] code = LookupValue(certificate, path);
            return Leb128.ReadUnsigned(code);
        }

        static string LookupRejectMessage(Certificate certificate, RequestId requestId)
        {
            IList<Label> path = new List<Label>();
            path.Add(new Label("request_status"));
            path.Add(new Label(requestId.Get()));
            path.Add(new Label("reject_message"));
            byte[] msg = LookupValue(certificate, path);
            return new string (msg, StandardCharsets.UTF_8);
        }

        static RequestStatusResponse LookupReply(Certificate certificate, RequestId requestId)
        {
            IList<Label> path = new List<Label>();
            path.Add(new Label("request_status"));
            path.Add(new Label(requestId.Get()));
            path.Add(new Label("reply"));
            byte[] replyData = LookupValue(certificate, path);
            CallReply reply = new CallReply(replyData);
            return new RequestStatusResponse(reply);
        }

        static byte[] LookupValue(Certificate certificate, IList<Label> path)
        {
            LookupResult result = certificate.tree.LookupPath(path);
            switch (result.status)
            {
                case ABSENT:
                    throw AgentError.Create(AgentError.AgentErrorCode.LOOKUP_PATH_ABSENT, path);
                case UNKNOWN:
                    throw AgentError.Create(AgentError.AgentErrorCode.LOOKUP_PATH_UNKNOWN, path);
                case FOUND:
                    return result.value;
                case ERROR:
                    throw AgentError.Create(AgentError.AgentErrorCode.LOOKUP_PATH_ERROR, path);
                default:
                    throw AgentError.Create(AgentError.AgentErrorCode.LOOKUP_PATH_ERROR, path);
                    break;
            }
        }
    }
}