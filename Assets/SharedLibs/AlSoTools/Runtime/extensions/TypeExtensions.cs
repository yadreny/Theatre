using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static AlSo.ShortCuts;

namespace AlSo
{
    public interface IKnownType { }

    public static class TypeExtensions
    {
        static Type[] _simpleTypes = new Type[] { typeof(string), typeof(int), typeof(bool), typeof(float) };

        public static bool IsSimple(this Type t)
        {
            return _simpleTypes.Contains(t);
        }

        public static bool IsChild(this Type parent, Type child)
        {
            return parent.IsAssignableFrom(child);
            //return child.IsSubclassOf(parent);
        }
        public static bool IsExtendedFrom(this Type child, Type parent) => parent.IsAssignableFrom(child);

        public static bool IsExtendedFrom<PARENT>(this Type child) => child.IsExtendedFrom(typeof(PARENT));

        private static Func<Type, bool> TypeFilter(Type parentType, bool allowSelf, bool allowAbstract, IEnumerable<Type> ifaces)
        {
            return (type) =>
            {
                if (type.IsAbstract && !allowAbstract) return false;
                if (type == parentType && !allowSelf) return false;
                if (ifaces != null) return ifaces.All(x => type.IsExtendedFrom(x));
                return true;
            };
        }

        public static Type[] GetExtendedFrom<T>(bool allowSelf = false, bool allowAbstract = false, List<Type> ifaces = null) where T : IKnownType
            => GetExtendedFrom(typeof(T), allowSelf, allowAbstract, ifaces);

        public static Type[] GetExtendedFrom(this Type type, bool allowSelf = false, bool allowAbstract = false, List<Type> ifaces = null)
        {
            if (!known.IsAssignableFrom(type)) throw new Exception($"add IKnown iface to type {type}");
            Func<Type, bool> filter = TypeFilter(type, allowSelf, allowAbstract, ifaces);
            return Types.Where(x => type.IsAssignableFrom(x)).Where(filter).ToArray();

            //typeof(IMyInterface).IsAssignableFrom(typeof(MyType))
        }

        private static Type[] _types;
        private static Type[] Types => CreateIfNotExist(ref _types, () => TypeTable.Values.ToArray());

        private static Dictionary<string, Type> _typeTable;
        private static Dictionary<string, Type> TypeTable => CreateIfNotExist(ref _typeTable, GetTable);

        readonly static Type known = typeof(IKnownType);

        private static bool IsGood(Assembly assembly)
        {
            return assembly.FullName.Contains("com.");
        }

        private static Dictionary<string, Type> GetTable()
        {
            Dictionary<string, Type> result = new Dictionary<string, Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(IsGood).ToArray();
            foreach (Assembly assembly in assemblies) 
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (!known.IsAssignableFrom(type)) continue;
                    try
                    {
                        result.Add(type.ToString(), type);
                    }
                    catch
                    {
                        Debug.LogError($"{type} in more than one assembly");
                    }
                }
            }
            return result;
        }

        public static Type GetType(this string typeName)
        {
            try
            {
                return TypeTable[typeName];
            }
            catch
            {
                Debug.LogWarning("can't find type: " + typeName);
            }
            return null;
        }

    }
}

//public static List<Type> GetExtendedFrom(this Type t, bool allowSelf = false, bool allowAbstract = false, List<Type> ifaces = null)
//{
//    List<Type> result = new List<Type>();

//    if (allowSelf)
//    {
//        if (!t.IsAbstract || allowAbstract) result.Add(t);
//    }

//    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

//    foreach (Assembly assembly in assemblies)
//    {
//        Type[] types = assembly.GetTypes();
//        foreach (Type type in types)
//        {
//            if (type.IsExtendedFrom(t))
//            {
//                if (!type.IsAbstract || allowAbstract)
//                {
//                    if (!result.Contains(type)) result.Add(type);
//                }
//            }
//        }
//    }

//    if (ifaces != null) result = result.Where(type => ifaces.All(iface => type.IsExtendedFrom(iface))).ToList();

//    return result;
//}

//static List<Type> filterIfaces(List<Type> types, List<Type> ifaces)
//{
//    List<Type> result = new List<Type>();
//    foreach (Type type in types)
//    {
//        bool isGood = true;
//        foreach (Type iface in ifaces)
//        {
//            if (!iface.IsAssignableFrom(type))
//            {
//                isGood = false;
//                break;
//            }
//        }
//        if (isGood) result.Add(type);
//    }
//    return result;
//}