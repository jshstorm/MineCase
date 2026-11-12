using System;
using System.Collections.Generic;
using System.Text;

namespace MineCase.Engine
{
    /// <summary>
    /// 一个键对应多个值的字典
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    internal sealed class MultiValueDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, List<TValue>> _dictionary = new Dictionary<TKey, List<TValue>>();

        public void Add(TKey key, TValue value)
        {
            if (!_dictionary.TryGetValue(key, out var values))
            {
                values = new List<TValue>();
                _dictionary.Add(key, values);
            }

            values.Add(value);
        }

        public bool Remove(TKey key, TValue value)
        {
            if (_dictionary.TryGetValue(key, out var values) && values.Remove(value))
            {
                if (values.Count == 0)
                    _dictionary.Remove(key);
                return true;
            }

            return false;
        }

        public bool TryGetValue(TKey key, out IReadOnlyCollection<TValue> value)
        {
            if (_dictionary.TryGetValue(key, out var values))
            {
                value = values;
                return true;
            }

            value = null;
            return false;
        }
    }
}
