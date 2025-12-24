using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlSo
{
    public static class ArrayExtension
    {
        public static T[] Trim<T>(this T[] arr, int newLen)
        {
            if (newLen == 0) return Array.Empty<T>();

            if ((uint)newLen > (uint)arr.Length)
                throw new ArgumentOutOfRangeException(nameof(newLen));
            if (newLen == arr.Length) return arr;
            Array.Resize(ref arr, newLen); // аллокация + копирование
            return arr;
        }

        public static int IndexOf(this Array arr, object elem)
        {
            return Array.IndexOf(arr, elem);
        }

        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            List<T> result = new List<T>();
            for (int i = 0; i < array.Length; i++)
            {
                if (i == index) continue;
                result.Add(array[i]);
            }
            return result.ToArray();
        }

        public static T[] InsertAt<T>(this T[] array, int index, T element)
        {
            T[] result = new T[array.Length + 1];
            for (int i = 0; i < result.Length; i++)
            {
                int arrayIndex = i <= index ? i : i - 1;
                result[i] = array[arrayIndex];
            }
            result[index] = element;
            return result;
        }
    }


    public static class ColorExtension
    {
        public static string ConvertColorToHex(Color color) => ColorUtility.ToHtmlStringRGB(color);
        public static string ColorToHex(this Color color) => ConvertColorToHex(color);
        public static string ColorToHex(this Color32 color) => ConvertColorToHex(color);
    }

    //public static Color highlight(this Color color)
    //{
    //    Color32 result = new Color32(255, 246, 0, 255);
    //    return result;
    //}

}