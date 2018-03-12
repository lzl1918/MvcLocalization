using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LocalizationCore.CodeMatching.Internals
{
    internal sealed class CacheDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> dictionary;
        private readonly int cacheSize;
        public int CacheSize => cacheSize;

        public ICollection<TKey> Keys => dictionary.Keys;
        public ICollection<TValue> Values => dictionary.Values;
        public int Count => dictionary.Count;
        public bool IsReadOnly => false;
        public TValue this[TKey key] { get => dictionary[key]; set => UpdateItem(key, value); }

        public CacheDictionary(int cacheSize)
        {
            this.cacheSize = cacheSize;
            dictionary = new Dictionary<TKey, TValue>(capacity: cacheSize);
        }

        public void Add(TKey key, TValue value) => UpdateItem(key, value);
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
        public bool Remove(TKey key) => dictionary.Remove(key);
        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);
        public void Add(KeyValuePair<TKey, TValue> item) => UpdateItem(item.Key, item.Value);
        public void Clear() => dictionary.Clear();
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (dictionary.TryGetValue(item.Key, out TValue value))
            {
                return value.Equals(item.Value);
            }
            return false;
        }
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var pair in dictionary)
            {
                array[arrayIndex] = pair;
                arrayIndex++;
            }
        }
        public bool Remove(KeyValuePair<TKey, TValue> item) => dictionary.Remove(item.Key);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => dictionary.GetEnumerator();

        private void UpdateItem(TKey key, TValue value)
        {
            while (dictionary.Count > cacheSize)
            {

            }
            dictionary[key] = value;
        }
    }
}
