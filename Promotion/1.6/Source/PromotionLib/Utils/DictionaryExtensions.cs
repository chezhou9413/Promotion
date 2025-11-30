using System;
using System.Collections.Generic;

namespace PromotionLib
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// 从字典中移除所有满足条件的键值对。
        /// 安全地在遍历期间标记并移除，避免枚举器冲突。
        /// </summary>
        public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dict, Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            if (dict == null || predicate == null)
                return;

            List<TKey> keysToRemove = new List<TKey>();
            foreach (var kv in dict)
            {
                if (predicate(kv))
                    keysToRemove.Add(kv.Key);
            }

            foreach (var key in keysToRemove)
            {
                dict.Remove(key);
            }
        }
    }
}
