using System.Collections.Generic;

namespace AlSo
{
    public static class DictKeyByValue
    {
        public static K GetKey<K, T>(this Dictionary<K, T> dict, T value) where K : class where T : class
        {
            foreach (K key in dict.Keys)
            {
                if (dict[key] == value) return key;
            }
            return null;
        }
    }
}