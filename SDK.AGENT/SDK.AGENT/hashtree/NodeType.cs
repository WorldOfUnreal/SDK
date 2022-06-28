using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Hashtree
{
    enum NodeType
    {
        // EMPTY(0)
        EMPTY,
        // FORK(1)
        FORK,
        // LABELED(2)
        LABELED,
        // LEAF(3)
        LEAF,
        // PRUNED(4)
        PRUNED 

        // --------------------
        // TODO enum body members
        // public int value;
        // NodeType(int value) {
        //     this.value = value;
        // }
        // --------------------
    }
}