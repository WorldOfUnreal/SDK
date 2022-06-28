using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Hashtree
{
    public sealed class LabeledHashTreeNode : HashTreeNode
    {
        Label label;
        HashTreeNode subtree;
        LabeledHashTreeNode(Label label, HashTreeNode subtree)
        {
            this.type = NodeType.LABELED;
            this.label = label;
            this.subtree = subtree;
        }
    }
}