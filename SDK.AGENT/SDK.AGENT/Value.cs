using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public class Value<T>
    {
        private Optional<T> value;
        public virtual void Set(T value)
        {
            this.value = Optional.Of(value);
        }

        public virtual T Get()
        {
            return value.Get();
        }
    }
}