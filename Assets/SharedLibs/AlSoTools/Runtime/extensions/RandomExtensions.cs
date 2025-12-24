using System.Collections.Generic;

namespace AlSo
{
    public class MyRandom : System.Random
    {
        public int Seed { get; }

        public MyRandom(int seed) : base(seed)
        {
            Seed = seed;
        }

        public float GetRandomFloat() => (float)NextDouble();
    }

    public static class MyRandomExtensions
    {
        //public static IList<T> Shuffle<T>(this IList<T> list, int seed)
        //{
        //    UnityEngine.Random.InitState(seed);

        //    int n = list.Count;
        //    while (n > 1)
        //    {
        //        n--;
        //        int k = UnityEngine.Random.Range(0, list.Count);
        //        T value = list[k];
        //        list[k] = list[n];
        //        list[n] = value;
        //    }
        //    return list;
        //}

        //public static float[] GenerateRandomFloats(this int seed, int quantity, float min, float max)
        //{
        //    float[] res = new float[quantity];
        //    for (int i = 0; i < quantity; i++)
        //    {
        //        UnityEngine.Random.InitState(seed + i);
        //        res[i] = UnityEngine.Random.Range(min, max);
        //    }
        //    return res;
        //}

        //public static T RandomElement<T>(this List<T> list)
        //{
        //    int index = UnityEngine.Random.Range(0, list.LastEnabledIndex());
        //    return list[index];
        //}

    }
}
