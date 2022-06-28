using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Hashtree
{
    public sealed class HashTreeDeserializer : JsonDeserializer<HashTree>
    {
        protected static readonly Logger LOG = LoggerFactory.GetLogger(typeof(HashTreeDeserializer));
        public override HashTree Deserialize(JsonParser parser, DeserializationContext ctx)
        {
            ObjectCodec oc = parser.GetCodec();
            JsonNode node = oc.ReadTree(parser);
            return new HashTree(HashTreeNode.Deserialize(node));
        }
    }
}