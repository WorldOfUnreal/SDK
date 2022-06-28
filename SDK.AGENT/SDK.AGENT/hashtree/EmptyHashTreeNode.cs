using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Hashtree
{
    public sealed class EmptyHashTreeNode : HashTreeNode
    {
        EmptyHashTreeNode()
        {
            this.type = NodeType.EMPTY;
        }
    }
}