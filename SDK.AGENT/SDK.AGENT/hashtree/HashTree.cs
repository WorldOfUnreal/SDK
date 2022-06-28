using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Hashtree
{
    public sealed class HashTree
    {
        HashTreeNode rootNode;
        HashTree(HashTreeNode rootNode)
        {
            this.rootNode = rootNode;
        }

        // Recomputes root hash of the full tree that this hash tree was constructed from.
        public byte[] Digest()
        {
            return this.rootNode.Digest();
        }

        // Given a (verified) tree, the client can fetch the value at a given path, which is a
        // sequence of labels (blobs).
        public LookupResult LookupPath(IList<Label> path)
        {
            return this.rootNode.LookupPath(path);
        }
    }
}