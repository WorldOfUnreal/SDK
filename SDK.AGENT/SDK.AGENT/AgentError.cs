using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public sealed class AgentError : Exception
    {
        /// <summary>
        /// </summary>
        private static readonly long serialVersionUID = 1L;
        static readonly string RESOURCE_BUNDLE_FILE = "dfinity_agent";
        static ResourceBundle properties;
        AgentErrorCode code;
        static AgentError()
        {
            properties = ResourceBundle.GetBundle(RESOURCE_BUNDLE_FILE);
        }

        public static AgentError Create(AgentErrorCode code, params object[] args)
        {
            string message = properties.GetString(code.label);

            // set arguments
            message = MessageFormat.Format(message, args);
            return new AgentError(code, message);
        }

        public static AgentError Create(AgentErrorCode code, Throwable t, params object[] args)
        {
            string message = properties.GetString(code.label);

            // set arguments
            message = MessageFormat.Format(message, args);
            return new AgentError(code, t, message);
        }

        private AgentError(AgentErrorCode code, string message): base(message)
        {
        }

        private AgentError(AgentErrorCode code, Throwable t, string message): base(message, t)
        {
        }

        public AgentErrorCode GetCode()
        {
            return code;
        }

        public enum AgentErrorCode
        {
            // INVALID_REPLICA_URL("InvalidReplicaUrl")
            INVALID_REPLICA_URL,
            // TIMEOUT_WAITING_FOR_RESPONSE("TimeoutWaitingForResponse")
            TIMEOUT_WAITING_FOR_RESPONSE,
            // URL_SYNTAX_ERROR("UrlSyntaxError")
            URL_SYNTAX_ERROR,
            // URL_PARSE_ERROR("UrlParseError")
            URL_PARSE_ERROR,
            // PRINCIPAL_ERROR("PrincipalError")
            PRINCIPAL_ERROR,
            // REPLICA_ERROR("ReplicaError")
            REPLICA_ERROR,
            // INVALID_CBOR_DATA("InvalidCborData")
            INVALID_CBOR_DATA,
            // HTTP_ERROR("HttpError")
            HTTP_ERROR,
            // CANNOT_USE_AUTHENTICATION_ON_NONSECURE_URL("CannotUseAuthenticationOnNonSecureUrl")
            CANNOT_USE_AUTHENTICATION_ON_NONSECURE_URL,
            // AUTHENTICATION_ERROR("AuthenticationError")
            AUTHENTICATION_ERROR,
            // INVALID_REPLICA_STATUS("InvalidReplicaStatus")
            INVALID_REPLICA_STATUS,
            // REQUEST_STATUS_DONE_NO_REPLY("RequestStatusDoneNoReply")
            REQUEST_STATUS_DONE_NO_REPLY,
            // MESSAGE_ERROR("MessageError")
            MESSAGE_ERROR,
            // CUSTOM_ERROR("CustomError")
            CUSTOM_ERROR,
            // LEB128_READ_ERROR("Leb128ReadError")
            LEB128_READ_ERROR,
            // UTF8_READ_ERROR("Utf8ReadError")
            UTF8_READ_ERROR,
            // LOOKUP_PATH_ABSENT("LookupPathAbsent")
            LOOKUP_PATH_ABSENT,
            // LOOKUP_PATH_UNKNOWN("LookupPathUnknown")
            LOOKUP_PATH_UNKNOWN,
            // LOOKUP_PATH_ERROR("LookupPathError")
            LOOKUP_PATH_ERROR,
            // INVALID_REQUEST_STATUS("InvalidRequestStatus")
            INVALID_REQUEST_STATUS,
            // CERTIFICATE_VERIFICATION_FAILED("CertificateVerificationFailed")
            CERTIFICATE_VERIFICATION_FAILED,
            // NO_ROOT_KEY_IN_STATUS("NoRootKeyInStatus")
            NO_ROOT_KEY_IN_STATUS,
            // COULD_NOT_READ_ROOT_KEY("CouldNotReadRootKey")
            COULD_NOT_READ_ROOT_KEY,
            // MISSING_REPLICA_TRANSPORT("MissingReplicaTransport")
            MISSING_REPLICA_TRANSPORT,
            // TRANSPORT_ERROR("TransportError")
            TRANSPORT_ERROR 

            // --------------------
            // TODO enum body members
            // public String label;
            // AgentErrorCode(String label) {
            //     this.label = label;
            // }
            // --------------------
        }
    }
}