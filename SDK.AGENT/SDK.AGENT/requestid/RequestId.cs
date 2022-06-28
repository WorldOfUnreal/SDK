using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Requestid
{
    public sealed class RequestId
    {
        byte[] value;
        RequestId(byte[] value)
        {
            this.value = value;
        }

        /*
	Derive the request ID from a serializable data structure.
	
	See https://hydra.dfinity.systems//build/268411/download/1/dfinity/spec/public/index.html#api-request-id
	
	# Warnings

	The argument type simply needs to be serializable; the function
	does NOT sift between fields to include them or not and assumes
	the passed value only includes fields that are not part of the
	envelope and should be included in the calculation of the request
	id.
	*/
        public static RequestId ToRequestId<T extends Serialize>(T value)
        {
            RequestIdSerializer serializer = new RequestIdSerializer();
            value.Serialize(serializer);
            serializer.HashFields();
            return serializer.Finish();
        }

        public static RequestId FromHexString(string hexValue)
        {
            try
            {
                return new RequestId((Hex.DecodeHex(hexValue)));
            }
            catch (DecoderException e)
            {
                throw RequestIdError.Create(RequestIdError.RequestIdErrorCode.CUSTOM_SERIALIZER_ERROR, e, e.GetLocalizedMessage());
            }
        }

        public byte[] Get()
        {
            return this.value;
        }

        public string ToString()
        {
            return this.value.ToString();
        }

        public string ToHexString()
        {
            return Hex.EncodeHexString(this.value);
        }
    }
}