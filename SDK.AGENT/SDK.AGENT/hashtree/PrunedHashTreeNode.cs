using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Hashtree
{
    public sealed class PrunedHashTreeNode : HashTreeNode
    {
        byte[] digest;
        PrunedHashTreeNode(byte[] digest)
        {
            this.type = NodeType.PRUNED;
            this.digest = digest;
        }
    }
}