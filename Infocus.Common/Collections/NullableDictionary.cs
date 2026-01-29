using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Infocus.Common.Collections
{
    [SerializableAttribute]
    [ComVisibleAttribute(false)]
    public class NullableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public NullableDictionary()
        {
            
        }
        public NullableDictionary(int capacity)
            : base(capacity)
        {
            
        }
        public NullableDictionary(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
            
        }
        public NullableDictionary(int capacity, IEqualityComparer<TKey> comparer)
            : base(capacity, comparer)
        {
            
        }
        public NullableDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary)
        {
            
        }
        public NullableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
            : base(dictionary, comparer)
        {
            
        }
        protected NullableDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            
        }

        public virtual new TValue this[TKey key]
        {
            get
            {
                try
                {
                    return base[key];
                }
                catch(KeyNotFoundException)
                {
                    return default(TValue);
                }
            }
            set
            {
                base[key] = value;
            }
        }
    }
}
