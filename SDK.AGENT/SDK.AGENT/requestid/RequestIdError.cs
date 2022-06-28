using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Requestid
{
    public sealed class RequestIdError : Exception
    {
        private static readonly long serialVersionUID = 1L;
        static readonly string RESOURCE_BUNDLE_FILE = "dfinity_requestid";
        static ResourceBundle properties;
        RequestIdErrorCode code;
        static RequestIdError()
        {
            properties = ResourceBundle.GetBundle(RESOURCE_BUNDLE_FILE);
        }

        public static RequestIdError Create(RequestIdErrorCode code, params object[] args)
        {
            string message = properties.GetString(code.label);

            // set arguments
            message = MessageFormat.Format(message, args);
            return new RequestIdError(code, message);
        }

        public static RequestIdError Create(RequestIdErrorCode code, Throwable t, params object[] args)
        {
            string message = properties.GetString(code.label);

            // set arguments
            message = MessageFormat.Format(message, args);
            return new RequestIdError(code, t, message);
        }

        private RequestIdError(RequestIdErrorCode code, string message): base(message)
        {
            this.code = code;
        }

        private RequestIdError(RequestIdErrorCode code, Throwable t, string message): base(message, t)
        {
            this.code = code;
        }

        public RequestIdErrorCode GetCode()
        {
            return code;
        }

        public enum RequestIdErrorCode
        {
            // CUSTOM_SERIALIZER_ERROR("CustomSerializerError")
            CUSTOM_SERIALIZER_ERROR,
            // EMPTY_SERIALIZER("EmptySerializer")
            EMPTY_SERIALIZER,
            // INVALID_STATE("InvalidState")
            INVALID_STATE 

            // --------------------
            // TODO enum body members
            // public String label;
            // RequestIdErrorCode(String label) {
            //     this.label = label;
            // }
            // --------------------
        }
    }
}