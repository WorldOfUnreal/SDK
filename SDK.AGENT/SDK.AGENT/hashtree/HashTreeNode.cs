using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Hashtree
{
    public abstract class HashTreeNode
    {
        protected static readonly Logger LOG = LoggerFactory.GetLogger(typeof(HashTreeNode));
        NodeType type;
        static HashTreeNode Deserialize(JsonNode node)
        {
            if (node.IsArray())
            {
                int tag = node[0].IntValue();
                switch (tag)
                {
                    case 0:
                        if (node.Count > 1)
                            throw new Exception("Invalid Length");
                        return new EmptyHashTreeNode();
                    case 1:
                        if (node.Count != 3)
                            throw new Exception("Invalid Length");
                        HashTreeNode left = HashTreeNode.Deserialize(node[1]);
                        HashTreeNode right = HashTreeNode.Deserialize(node[2]);
                        return new ForkHashTreeNode(left, right);
                    case 2:
                        if (node.Count != 3)
                            throw new Exception("Invalid Length");
                        try
                        {

                            // Incompatible with Android, using custom function instead
                            Label label = new Label(Base64.GetDecoder().Decode(node[1].AsText()));

                            //Label label1 = new Label(Base64.decodeBase64(node.get(1).asText()));
                            //Label label = new Label(Base64.decodeBase64(node.get(1).binaryValue()));
                            HashTreeNode subtree = HashTreeNode.Deserialize(node[2]);
                            return new LabeledHashTreeNode(label, subtree);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(String.Format("Invalid Node Type %s", node.GetNodeType().Name()));
                        }

                    case 3:
                        if (node.Count != 2)
                            throw new Exception("Invalid Length");
                        try
                        {
                            byte[] value = node[1].BinaryValue();
                            return new LeafHashTreeNode(value);
                        }
                        catch (IOException e)
                        {
                            throw new Exception(String.Format("Invalid Node Type %s", node.GetNodeType().Name()));
                        }

                    case 4:
                        if (node.Count != 2)
                            throw new Exception("Invalid Length");
                        try
                        {
                            byte[] value = node[1].BinaryValue();
                            byte[] digest = DigestUtils.Sha256(value);
                            return new PrunedHashTreeNode(digest);
                        }
                        catch (IOException e)
                        {
                            throw new Exception(String.Format("Invalid Node Type %s", node.GetNodeType().Name()));
                        }

                    default:
                        throw new Exception(String.Format("Unknown tag: %d, expected the tag to be one of {{0, 1, 2, 3, 4}}", tag));
                        break;
                }
            }
            else
                throw new Exception(String.Format("Invalid Node Type %s", node.GetNodeType().Name()));
        }

        /*
	* Calculate the digest of this node only.
	*/
        public virtual byte[] Digest()
        {
            MessageDigest messageDigest = DigestUtils.GetSha256Digest();
            this.DomainSep(messageDigest);
            switch (this.type)
            {
                case EMPTY:
                    break;
                case FORK:
                    messageDigest.Update(((ForkHashTreeNode)this).left.Digest());
                    messageDigest.Update(((ForkHashTreeNode)this).right.Digest());
                    break;
                case LABELED:
                    messageDigest.Update(((LabeledHashTreeNode)this).label.value);
                    messageDigest.Update(((LabeledHashTreeNode)this).subtree.Digest());
                    break;
                case LEAF:
                    messageDigest.Update(((LeafHashTreeNode)this).value);
                    break;
                case PRUNED:
                    return ((PrunedHashTreeNode)this).digest;
            }

            return messageDigest.Digest();
        }

        /* Update a hasher with the domain separator (byte(|s|) . s).
	 */
        virtual void DomainSep(MessageDigest messageDigest)
        {
            string domainSep;
            switch (this.type)
            {
                case EMPTY:
                    domainSep = "ic-hashtree-empty";
                    break;
                case FORK:
                    domainSep = "ic-hashtree-fork";
                    break;
                case LABELED:
                    domainSep = "ic-hashtree-labeled";
                    break;
                case LEAF:
                    domainSep = "ic-hashtree-leaf";
                    break;
                default:
                    return;
                    break;
            }

            messageDigest.Update((byte)domainSep.Length());
            messageDigest.Update(domainSep.GetBytes());
        }

        /*
	 Lookup the path for the current node only. If the node does not contain the label,
    this will return [None], signifying that whatever process is recursively walking the
    tree should continue with siblings of this node (if possible). If it returns
    [Some] value, then it found an actual result and this may be propagated to the
    original process doing the lookup.
    
    This assumes a sorted hash tree, which is what the spec says the system should return.
    It will stop when it finds a label that's greater than the one being looked for.
	*/
        public virtual LookupResult LookupPath(IList<Label> path)
        {
            if (path == null || path.IsEmpty())
            {
                switch (this.type)
                {
                    case EMPTY:
                        return new LookupResult(LookupResult.LookupResultStatus.ABSENT);
                    case FORK:
                        return new LookupResult(LookupResult.LookupResultStatus.ERROR);
                    case LABELED:
                        return new LookupResult(LookupResult.LookupResultStatus.ERROR);
                    case LEAF:
                        return new LookupResult(LookupResult.LookupResultStatus.FOUND, ((LeafHashTreeNode)this).value);
                    case PRUNED:
                        return new LookupResult(LookupResult.LookupResultStatus.UNKNOWN);
                }
            }
            else
            {
                LookupLabelResult result = this.LookupLabel(path[0]);
                switch (result.status)
                {
                    case UNKNOWN:
                        return new LookupResult(LookupResult.LookupResultStatus.UNKNOWN);
                    case ABSENT:
                    case CONTINUE:
                        if (Arrays.AsList(NodeType.EMPTY, NodeType.PRUNED, NodeType.LEAF).Contains(this.type))
                            return new LookupResult(LookupResult.LookupResultStatus.UNKNOWN);
                        else
                            return new LookupResult(LookupResult.LookupResultStatus.ABSENT);
                    case FOUND:
                        path.Remove(0);
                        return result.value.LookupPath(path);
                }
            }

            throw new Exception("Invalid Path " + path);
        }

        /*
	Lookup a single label, returning a reference to the labeled [HashTreeNode] node if found.
    
    This assumes a sorted hash tree, which is what the spec says the system should
    return. It will stop when it finds a label that's greater than the one being looked
    for.
    
     This function is implemented with flattening in mind, ie. flattening the forks
     is not necessary.	
     */
        virtual LookupLabelResult LookupLabel(Label label)
        {
            switch (this.type)
            {
                case LABELED:

                    // If this node is a labeled node, check for the name. This assume a
                    int i = label.CompareTo(((LabeledHashTreeNode)this).label);
                    if (i > 0)
                        return new LookupLabelResult(LookupLabelResultStatus.CONTINUE);
                    else if (i == 0)
                        return new LookupLabelResult(LookupLabelResultStatus.FOUND, ((LabeledHashTreeNode)this).subtree);
                    else

                        // If this node has a smaller label than the one we're looking for, shortcut
                        // out of this search (sorted tree), we looked too far.
                        return new LookupLabelResult(LookupLabelResultStatus.ABSENT);
                case FORK:
                {
                    LookupLabelResult leftResult = ((ForkHashTreeNode)this).left.LookupLabel(label);
                    switch (leftResult.status)
                    {
                        case CONTINUE:
                        case UNKNOWN:
                        {
                            LookupLabelResult rightResult = ((ForkHashTreeNode)this).right.LookupLabel(label);
                            if (rightResult.status == LookupLabelResultStatus.ABSENT)
                            {
                                if (leftResult.status == LookupLabelResultStatus.ABSENT)
                                    return new LookupLabelResult(LookupLabelResultStatus.UNKNOWN);
                                else
                                    return new LookupLabelResult(LookupLabelResultStatus.ABSENT);
                            }
                            else
                                return rightResult;
                        }

                        default:
                            return leftResult;
                            break;
                    }
                }

                case PRUNED:
                    return new LookupLabelResult(LookupLabelResultStatus.UNKNOWN);
                default:

                    // Any other type of node and we need to look for more forks.
                    return new LookupLabelResult(LookupLabelResultStatus.CONTINUE);
                    break;
            }
        }

        class LookupLabelResult
        {
            LookupLabelResultStatus status;
            HashTreeNode value;
            LookupLabelResult(LookupLabelResultStatus status)
            {
                this.status = status;
            }

            LookupLabelResult(LookupLabelResultStatus status, HashTreeNode value)
            {
                this.status = status;
                this.value = value;
            }
        }

        enum LookupLabelResultStatus
        {
            // The label is not part of this node's tree.
            ABSENT,
            // This partial view does not include information about this path, and the original
            // tree may or may note include this value.
            UNKNOWN,
            // The label was not found, but could still be somewhere else.
            CONTINUE,
            // The value was found at the referenced node.
            FOUND
        }
    }
}