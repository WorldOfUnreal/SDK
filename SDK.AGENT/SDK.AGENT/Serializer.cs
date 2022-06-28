using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public interface ISerializer
    {
        void SerializeField<T>(string key, T value);
    }
}