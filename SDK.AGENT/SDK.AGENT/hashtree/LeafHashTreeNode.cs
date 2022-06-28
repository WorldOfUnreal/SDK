using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Hashtree
{
    public sealed class LeafHashTreeNode : HashTreeNode
    {
        byte[] value;
        LeafHashTreeNode(byte[] value)
        {
            this.type = NodeType.LEAF;
            this.value = value;
        }

        public byte[] GetValue()
        {
            return this.value;
        }
    }
}