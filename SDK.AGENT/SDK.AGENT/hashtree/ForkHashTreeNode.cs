using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Hashtree
{
    public sealed class ForkHashTreeNode : HashTreeNode
    {
        HashTreeNode left;
        HashTreeNode right;
        ForkHashTreeNode(HashTreeNode left, HashTreeNode right)
        {
            this.type = NodeType.FORK;
            this.left = left;
            this.right = right;
        }
    }
}