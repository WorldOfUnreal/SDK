using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Requestid
{
    public sealed class RequestIdSerializer : ISerializer
    {
        static readonly Logger LOG = LoggerFactory.GetLogger(typeof(RequestIdSerializer));
        MessageDigest messageDigest = DigestUtils.GetSha256Digest();
        TreeMap<ByteBuffer, byte[]> fields = new TreeMap<ByteBuffer, byte[]>();
        public override void SerializeField<T>(string key, T value)
        {
            byte[] keyHash = this.HashValue(key);
            byte[] valueHash = this.HashValue(value);
            fields.Put(ByteBuffer.Wrap(keyHash), valueHash);
        }

        /*
	 * Hash a single value, returning its sha256_hash.
	 */
        byte[] HashValue<T>(T value)
        {
            byte[] bytes;
            if (value is IList)
                return this.HashList((IList<TWildcardTodo>)value);
            if (value is long)
                bytes = this.SerializeLong((long)value);
            else if (value is Principal)
                bytes = ((Principal)value).GetValue();
            else if (value is byte[])
                bytes = (byte[])value;
            else
                bytes = value.ToString().GetBytes();
            return DigestUtils.Sha256(bytes);
        }

        byte[] HashList(IList<TWildcardTodo> value)
        {
            MessageDigest messageDigest;
            try
            {
                messageDigest = (MessageDigest)this.messageDigest.Clone();
                foreach (object item in value)
                {
                    byte[] bytes = this.HashValue(item);
                    messageDigest.Update(bytes);
                }

                return messageDigest.Digest();
            }
            catch (CloneNotSupportedException e)
            {
                throw RequestIdError.Create(RequestIdError.RequestIdErrorCode.CUSTOM_SERIALIZER_ERROR, e, e.GetLocalizedMessage());
            }
        }

        byte[] SerializeLong(long value)
        {

            // 10 bytes is enough for a 64-bit number in leb128.
            byte[] buffer = new byte[10];
            ByteBuffer writeable = ByteBuffer.Wrap(buffer);
            int nBytes = Leb128.WriteUnsigned(writeable, value);
            return Arrays.CopyOf(buffer, nBytes);
        }

        void HashFields()
        {
            List<ByteBuffer> keyValues = new List<ByteBuffer>();
            foreach (Map.Entry<ByteBuffer, byte[]> entry in fields.EntrySet())
            {
                ByteBuffer key = entry.GetKey();
                byte[] value = entry.GetValue();
                ByteBuffer keyValue = (ByteBuffer)((Buffer)ByteBuffer.Allocate(key.Limit() + value.length).Put(key).Put(value).Rewind());
                keyValues.Add(keyValue);
            }


            // Have to use custom comparator. Rust implementation is sorting using unsigned
            // values
            // while ByteBuffer comparator is using signed bye array. Sort result is
            // different there
            // so would be hash result
            keyValues.Sort(new AnonymousComparator(this));
            foreach (ByteBuffer value in keyValues)
                messageDigest.Update(value);
        }

        private sealed class AnonymousComparator : Comparator
        {
            public AnonymousComparator(RequestIdSerializer parent)
            {
                this.parent = parent;
            }

            private readonly RequestIdSerializer parent;
            public int Compare(ByteBuffer bytes1, ByteBuffer bytes2)
            {
                int result = 0;
                while (bytes1.Limit() > 0 && bytes2.Limit() > 0)
                {
                    result = Long.Compare(Byte.ToUnsignedLong(bytes1.Get()), Byte.ToUnsignedLong(bytes2.Get()));
                    if (result != 0)
                        break;
                }

                bytes1.Rewind();
                bytes2.Rewind();
                return result;
            }
        }

        /*
	 * Finish the hashing and returns the RequestId for the structure that was
	 * serialized.
	 */
        RequestId Finish()
        {
            return new RequestId(messageDigest.Digest());
        }
    }
}