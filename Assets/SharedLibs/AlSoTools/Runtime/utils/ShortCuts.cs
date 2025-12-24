using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public static class ShortCuts
    {
        public static void GodBlessAmerica() => Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

        public static T GetOrAttachComponent<T>(this MonoBehaviour self) where T : Component
            => self.gameObject.GetOrAttachComponent<T>();
        public static T GetOrAttachComponent<T>(this Transform self) where T : Component
            => self.gameObject.GetOrAttachComponent<T>();

        public static T GetOrAttachComponent<T>(this GameObject self) where T : Component
        {
            T result = self.GetComponent<T>();
            if (result == null) result = self.AddComponent<T>();
            return result;
        }

        public static T GetOrAttachComponentToChild<T>(this GameObject self, string name) where T : Behaviour
        {
            GameObject child = self.Find(name);
            if (!child)
            {
                Debug.LogError(self.name + " cant find child " + name);
                Debug.LogError(self);
            }
            T result = child.GetComponent<T>();
            if (result != null) return result;
            result = child.AddComponent<T>();
            if (result == null)
            {
                string message = $"failed to add component {typeof(T)}";
                throw new Exception(message);
            }
            return result;
        }

        public static T GetOrAttachComponentToChild<T>(this Transform self, string name) where T : Behaviour
            => self.gameObject.GetOrAttachComponentToChild<T>(name);

        public static T GetOrAttachComponentToChild<T>(this MonoBehaviour self, string name) where T : Behaviour
            => self.gameObject.GetOrAttachComponentToChild<T>(name);

        public static Texture2D CreateIfNotExist(ref Texture2D obj, string path)
        {
            if (obj == null)
            {
                obj = Resources.Load<Texture2D>(path);
                if (obj == null) Debug.LogError($"wrong texture path: {path}");
            }
            return obj;
        }


        public static T CreateIfNotExist<T>(ref T obj, Func<T> creator) where T : class
        {
            if (obj == null) obj = creator();
            return obj;
        }

        public static T CreateIfNotExist<T>(ref T obj, Func<T> creator, T defaultValue) where T : struct
        {
            if (obj.Equals(defaultValue)) obj = creator();
            return obj;
        }


        //public static T CreateIfNotExist<T>(ref T obj, Func<T> creator, object locker)
        //{
        //    lock (locker)
        //    {
        //        if (obj == null) obj = creator();
        //        return obj;
        //    }
        //}

        public static float DefFloat = 666;

        public static T CreateIfNotDefault<T>(ref T obj, Func<T> creator, T defaultValue)
        {
            //if (obj.Equals(default(T))) obj = creator();
            if (obj.Equals(defaultValue)) obj = creator();
            return obj;
        }


        public static C AddToDictionaryIfNotExist<K, P, C>(Dictionary<K, P> dict, K key, Func<K, C> func) where C : P
        {
            if (!dict.ContainsKey(key))
            {
                P elem = func(key);
                dict.Add(key, elem);
            }
            return (C)dict[key];
        }
    }

}