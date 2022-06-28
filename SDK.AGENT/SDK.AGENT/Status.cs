using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public class Status
    {
        public string icAPIVersion;
        public Optional<string> implSource;
        public Optional<string> implVersion;
        /// <summary>
        /// Optional. The precise git revision of the Internet Computer implementation.
        /// </summary>
        public Optional<string> implRevision;
        /// <summary>
        /// Optional.  The health status of the replica.  One hopes it's "healthy".
        /// </summary>
        public Optional<string> replicaHealthStatus;
        /// <summary>
        /// Optional.  The root (public) key used to verify certificates.
        /// </summary>
        public Optional<byte[]> rootKey;
        public Map<string, TWildcardTodo> values;
    }
}