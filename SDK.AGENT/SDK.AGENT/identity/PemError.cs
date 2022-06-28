using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Identity
{
    public sealed class PemError : Exception
    {
        private static readonly long serialVersionUID = 1L;
        static readonly string RESOURCE_BUNDLE_FILE = "dfinity_pem";
        static ResourceBundle properties;
        PemErrorCode code;
        static PemError()
        {
            properties = ResourceBundle.GetBundle(RESOURCE_BUNDLE_FILE);
        }

        public static PemError Create(PemErrorCode code, params object[] args)
        {
            string message = properties.GetString(code.label);

            // set arguments
            message = MessageFormat.Format(message, args);
            return new PemError(code, message);
        }

        public static PemError Create(PemErrorCode code, Throwable t, params object[] args)
        {
            string message = properties.GetString(code.label);

            // set arguments
            message = MessageFormat.Format(message, args);
            return new PemError(code, t, message);
        }

        private PemError(PemErrorCode code, string message): base(message)
        {
            this.code = code;
        }

        private PemError(PemErrorCode code, Throwable t, string message): base(message, t)
        {
            this.code = code;
        }

        public PemErrorCode GetCode()
        {
            return code;
        }

        public enum PemErrorCode
        {
            // PEM_ERROR("PemError")
            PEM_ERROR,
            // KEY_REJECTED("KeyRejected")
            KEY_REJECTED,
            // ERROR_STACK("ErrorStack")
            ERROR_STACK 

            // --------------------
            // TODO enum body members
            // public String label;
            // PemErrorCode(String label) {
            //     this.label = label;
            // }
            // --------------------
        }
    }
}