using System;
using System.Collections.Generic;
using System.Linq;

namespace AlSo
{
    public interface ISingleton
    { 
    
    }
}


namespace System.Collections.Generic
{
    public static class ListExtensions
    {

        public static int IndexOf<T>(this IEnumerable<T> source, T item)
        {
            int index = 0;
            var comparer = EqualityComparer<T>.Default;
            foreach (var element in source)
            {
                if (comparer.Equals(element, item))
                    return index;
                index++;
            }
            return -1;
        }

        //public static T Random<T>(this IEnumerable<T> enumerable)
        //{
        //    var r = new Random();
        //    var list = enumerable as IList<T> ?? enumerable.ToList();
        //    return list.ElementAt(r.Next(0, list.Count()));
        //}

        //public static void SmartInsert<T>(this List<T> list, int index, T item)
        //{
        //    if (index == list.Count + 1) list.Add(item);
        //    else if (index <= list.Count) list.Insert(index, item);
        //    else throw new Exception("bad index");
        //}

        public static IEnumerable<T> ContinueWith<T>(this T first, IEnumerable<T> elems)
        {
            List<T> result = new List<T>() { first };
            foreach (T elem in elems) result.Add(elem);
            return result;
        }

        public static T Random<T>(this IEnumerable<T> list)
        {
            T[] arr = list.ToArray();
            return arr[UnityEngine.Random.Range(0, arr.Length)];
        }

        public static T Random<T>(this IEnumerable<T> list, System.Random random)
        {
            T[] arr = list.ToArray();
            return arr[random.Next(arr.Length)];
        }

        public static List<T> ToListOrNull<T>(this IEnumerable<T> items) => (items.Count() > 0) ? items.ToList() : null;

        public static IEnumerable<T> IntersectionMany<T>(this IEnumerable<IEnumerable<T>> listOfLists)
        {
            return listOfLists.SelectMany(elem => elem).Distinct().Where(elem => listOfLists.All(list => list.Contains(elem)));
        }

        public static bool intersects<T>(this IList<T> list, IList<T> other)
        {
            foreach (T elem in list)
            {
                if (other.Contains(elem)) return true;
            }
            return false;
        }

        public static List<T> convert<T>(this IList list)
        {
            List<T> result = new List<T>();
            foreach (T item in list)
            {
                result.Add(item);
            }
            return result;
        }

        public static int LastEnabledIndex(this IList list) => list.Count - 1;

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) return;
            if (action == null) throw new ArgumentNullException("action");

            foreach (T element in source)
            {
                action(element);
            }
        }

        public static Dictionary<K, V> ConvertToDictionary<T, K, V>
            (this IEnumerable<T> list, Func<T, K> itemToKey, Func<T,V> itemToValue)
        {
            Dictionary<K, V> res = new Dictionary<K, V>();
            foreach (T element in list)
            {
                K key = itemToKey(element);
                V value = itemToValue(element);
                res.Add(key, value);    
            }
            return res;
        }

        public static Dictionary<K, V> ConvertToDictionary<K, V>(this IEnumerable<V> list, Func<V, K> func)
        {
            Dictionary<K, V> res = new Dictionary<K, V>();
            list.ForEach(value => res.Add(func(value), value));
            return res;
        }

        public static List<T> Clone<T>(this List<T> list) where T : class
        {
            List<T> result = new List<T>();
            list.ForEach(x => result.Add(x));
            return result;
        }

        public static IEnumerable<T> ReverseAndReturn<T>(this IEnumerable<T> elements)
        {
            elements.Reverse();
            return elements;
        }

        public static List<T> ReorderToLast<T>(this List<T> list, T element)
        {
            list.Remove(element);
            list.Insert(list.Count, element);
            return list;
        }

        public static List<T> ReorderToFirst<T>(this List<T> list, T element)
        {
            list.Remove(element);
            list.Insert(0, element);
            return list;
        }

        public static List<T> ToList<T>(this IEnumerable list)
        {
            List<T> result = new List<T>();
            foreach (T item in list)
            {
                result.Add(item);
            }
            return result;
        }

        public static void AddItemsFrom<T>(this List<T> list, IList newElems, bool noDuplicates = false) 
        {
            foreach (T item in newElems)
            {
                if (noDuplicates && list.Contains(item))
                {
                    object oldValue = list[list.IndexOf(item)];
                    object itemValue = item;
                    if (oldValue == itemValue)
                    {
                        continue;
                    }
                }
                list.Add(item);
            }
        }

        public static T Pop<T>(this List<T> list)
        {
            if (list == null) throw new ArgumentNullException("list");
            if (list.Count == 0)
            {
                throw new ArgumentException("list is empty");
            }

            int id = list.LastEnabledIndex();
            T value = list[id];
            list.RemoveAt(id);
            return value;
        }

        public static T Shift<T>(this List<T> list)
        {
            T value = list.First();
            list.RemoveAt(0);
            return value;
        }

        public static T Next<T>(this T element, IEnumerable<T> sequence)
        {
            List<T> list = sequence.ToList();
            if (!list.Contains(element)) throw new Exception($"list doesnt contains element");
            int id = list.IndexOf(element);
            return id == list.Count - 1 ? default : list[id + 1];
        }

        public static T Prev<T>(this T element, IEnumerable<T> sequence)
        {
            List<T> list = sequence.ToList();
            if (!list.Contains(element)) throw new Exception($"list doesnt contains element");
            int id = list.IndexOf(element);
            return id == 0 ? default : list[id - 1];
        }

    }
}