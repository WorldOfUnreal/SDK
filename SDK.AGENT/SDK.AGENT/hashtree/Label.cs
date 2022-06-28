using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Hashtree
{
    public sealed class Label : IComparable<Label>
    {
        static readonly Logger LOG = LoggerFactory.GetLogger(typeof(Label));
        byte[] value;
        public Label(byte[] value)
        {
            this.value = value;
        }

        public Label(string value)
        {
            if (value != null)
                this.value = value.GetBytes();
        }

        public byte[] Get()
        {
            return this.value;
        }

        public bool Equals(Label label)
        {
            return Arrays.Equals(this.value, label.Get());
        }

        public string ToString()
        {
            if (this.value != null)
                return new string (this.value);
            else
                return null;
        }

        public override int CompareTo(Label label)
        {
            int result = 0;
            ByteBuffer bytes1 = ByteBuffer.Wrap(this.value);
            ByteBuffer bytes2 = ByteBuffer.Wrap(label.value);
            while (bytes1.Position() < this.value.length && bytes2.Position() < label.value.length)
            {
                result = Long.Compare(Byte.ToUnsignedLong(bytes1.Get()), Byte.ToUnsignedLong(bytes2.Get()));
                if (result != 0)
                    break;
            }

            if (result == 0 && this.value.length != label.value.length)
                return label.value.length - this.value.length;
            return result;
        }
    }
}