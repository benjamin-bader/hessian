using System.Collections;
using System.Collections.Generic;

namespace Hessian.Collections
{
    public class TwoWayDictionary<TKey, TValue> : ITwoWayDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> dict;
        private readonly TwoWayDictionary<TValue, TKey> inverse;

        public int Count {
            get { return dict.Count; }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public ITwoWayDictionary<TValue, TKey> Inverse {
            get { return inverse; }
        }

        public TValue this[TKey key] {
            get { return dict[key]; }
            set { UpdateDictAndInverse(key, value, false); }
        }

        public TKey this[TValue valueKey] {
            get { return inverse[valueKey]; }
            set { inverse[valueKey] = value; }
        }

        public ICollection<TKey> Keys {
            get { return dict.Keys; }
        }

        public ICollection<TValue> Values {
            get { return inverse.dict.Keys; }
        } 

        public TwoWayDictionary()
            : this(new Dictionary<TKey, TValue>(), new Dictionary<TValue, TKey>())
        {
        }

        public TwoWayDictionary(IDictionary<TKey, TValue> forwards, IDictionary<TValue, TKey> backwards)
        {
            dict = forwards;
            inverse = new TwoWayDictionary<TValue, TKey>(backwards, this);
        }

        private TwoWayDictionary(IDictionary<TKey, TValue> dict, TwoWayDictionary<TValue, TKey> inverse)
        {
            this.dict = dict;
            this.inverse = inverse;
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public bool ContainsValue(TValue value)
        {
            return inverse.ContainsKey(value);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dict.TryGetValue(key, out value);
        }

        public bool TryGetKey(TValue value, out TKey key)
        {
            return inverse.TryGetValue(value, out key);
        }

        public void Add(TKey key, TValue value)
        {
            UpdateDictAndInverse(key, value, true);
        }

        public bool Remove(TKey key)
        {
            return RemoveFromDictAndInverse(key);
        }

        private void UpdateDictAndInverse(TKey key, TValue value, bool throwIfContained)
        {
            if (!throwIfContained) {
                dict.Remove(key);
                inverse.dict.Remove(value);
            }

            dict.Add(key, value);
            inverse.dict.Add(value, key);
        }

        private bool RemoveFromDictAndInverse(TKey key)
        {
            TValue value;
            if (!TryGetValue(key, out value)) {
                return false;
            }

            return RemoveFromDictAndInverse(key, value);
        }

        private bool RemoveFromDictAndInverse(TKey key, TValue value)
        {
            if (!ContainsKey(key) || !ContainsValue(value)) {
                return false;
            }

            return dict.Remove(key) && inverse.dict.Remove(value);
        }

        #region ICollection<KeyValuePair<TKey, TValue>>

        public void Add(KeyValuePair<TKey, TValue> kvp)
        {
            Add(kvp.Key, kvp.Value);
        }

        public bool Remove(KeyValuePair<TKey, TValue> kvp)
        {
            return RemoveFromDictAndInverse(kvp.Key, kvp.Value);
        }

        public void Clear()
        {
            dict.Clear();
            inverse.dict.Clear();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dict).CopyTo(array, arrayIndex);
        }

        public bool Contains(KeyValuePair<TKey, TValue> kvp)
        {
            return ContainsKey(kvp.Key) && ContainsValue(kvp.Value);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
