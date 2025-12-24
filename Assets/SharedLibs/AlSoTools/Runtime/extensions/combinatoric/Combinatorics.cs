using System.Linq;

namespace System.Collections.Generic
{
    public static class Combinatorics
    {
        static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetKCombs(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }


        public static IEnumerable<IEnumerable<T>> GetCombinations<T>(this IEnumerable<T> list, int length) where T : IComparable
        {
            if (length < 1) throw new Exception("bad subset length");
            return GetKCombs<T>(list, length);
        }
        
        
    }


    public static class ComboExt
    {
        public static IEnumerable<IEnumerable<T>> GetAllPossibleCombos<T>(this IEnumerable<IEnumerable<T>> groups)
        {
            IEnumerable<IEnumerable<T>> combos = new T[][] { new T[0] };

            foreach (var inner in groups)
                combos = from c in combos
                         from i in inner
                         select c.AppendElem(i);

            return combos;
        }

        static IEnumerable<TSource> AppendElem<TSource>(this IEnumerable<TSource> source, TSource item)
        {
            foreach (TSource element in source)
                yield return element;

            yield return item;
        }

    }

}
