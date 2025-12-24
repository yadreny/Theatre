using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using AlSo.Converter;

namespace AlSo
{
    public enum ListType { None, Array, List }

    public class PropertyDescription
    {
        private static string[] InvisibleFields { get; } = new string[] { "id", "x", "y", "host" };

        public PropertyInfo Info { get; }
        public PropertyDescription(PropertyInfo info)
        {
            Info = info;
        }

        private Action<object, object> _writeFunc;
        private Action<object, object> writeFunc
        {
            get
            {
                if (_writeFunc == null)
                {
                    _writeFunc = DoesMatters ? GetDoesMattersWriter() : Info.SetValue;
                } 
                return _writeFunc;
            }
        }

        private Action<object, object> GetDoesMattersWriter()
        {
            FieldInfo info = Info.DeclaringType.GetField($"<{Info.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            return (obj, value) => info.SetValue(obj, value);
        }

        private Func<object, object> _readFunc;
        private Func<object, object> readFunc
        {
            get
            {
                if (_readFunc == null) _readFunc = Info.GetValue;
                return _readFunc;
            }
        }


        private bool isListDefined;
        private ListType _listType;
        public ListType listType
        {
            get
            {
                if (!isListDefined)
                {
                    isListDefined = true;

                    bool isArray = PropertyType.IsExtendedFrom<Array>();
                    if (isArray)
                    {
                        _listType = ListType.Array;
                        return _listType;
                    }
                    bool IsList = PropertyType.IsExtendedFrom<IList>();
                    if (IsList)
                    {
                        _listType = ListType.List;
                        return _listType;
                    }
                    _listType = ListType.None;
                }
                return _listType;
            }
        }

        public bool IsList => listType != ListType.None;
        public bool IsSingle => listType == ListType.None;

        private bool DoesMatters => Info.GetCustomAttribute<Matters>() != null;

        public bool isValid => DoesMatters || !isInvalid;
        public bool isInvalid => isInvalidType || isInvalidList; //|| Name.Contains("k__BackingField") || Type.ToString().Contains("System.Action")
        bool isInvalidType => ElementType.IsExtendedFrom<IDictionary>();
        bool isInvalidList => listType != ListType.None && !validListType;
        bool validListType =>
            ElementType.IsExtendedFrom<Texture2D>() 
            || ElementType.IsExtendedFrom<ISerializableData>() 
            || ElementType.IsExtendedFrom<Vector2>() 
            || ElementType == typeof(int) 
            || ElementType == typeof(float);

        public bool isVisible => !InvisibleFields.Contains(Name) && GetMeta<DontShow>() == null;

        private string _name;
        public string Name
        {
            get
            {
                if (_name == null) _name = Info.Name;
                return _name;
            }
        }
        //private Type _propertyType;
        public Type PropertyType => Info.PropertyType;
        //{
        //    get
        //    {
        //        if (_propertyType == null) _propertyType = ;
        //        return _propertyType;
        //    }
        //}

        public Type _elementType;
        public Type ElementType
        {
            get
            {
                if (_elementType == null)
                {
                    _elementType = listType switch
                    {
                        ListType.None => PropertyType,
                        ListType.Array => PropertyType.GetElementType(),
                        ListType.List => PropertyType.GetGenericArguments()[0],
                        _ => throw new Exception(),
                    }; 
                }
                return _elementType;
            }
        }

        public Type owner => Info.DeclaringType;

        public bool isSavable => Info.CanWrite || DoesMatters;

        IEnumerable<Attribute> _metas;
        public IEnumerable<Attribute> metas
        {
            get
            {
                if (_metas == null) _metas = GetMetas();
                return _metas;
            }
        }

        private IEnumerable<Attribute> GetMetas()
        {
            IEnumerable<Attribute> result = Info.GetCustomAttributes(true).OfType<Attribute>();
            Type[] ifaces = owner.GetInterfaces();
            if (ifaces != null)
            {
                foreach (Type iface in ifaces)
                {
                    TypeDescription td = TypeDescriptionLibray.Instance.GetDescription(iface);
                    MemberInfo info = td.All.SingleOrDefault(x => x.Name == Name)?.Info;
                    if (info != null) info.GetCustomAttributes(false).OfType<Attribute>().ForEach(x => result.Append(x));
                }
            }
            result.OfType<EscoOwnableMeta>().ForEach(x => x.Owner = this);
            return result;
        }

        private Dictionary<Type, IAsloAttribute> GetMetaCache = new Dictionary<Type, IAsloAttribute>();

        public T GetMeta<T>() where T : IAsloAttribute
        {
            Type t = typeof(T);
            if (GetMetaCache.TryGetValue(t, out IAsloAttribute res)) return (T)res;

            foreach (Attribute a in metas)
            {
                if (!(a is T)) continue;
                T item = (T)(object)a;
                GetMetaCache.Add(t, item);
                return item;
            }
            return default;
        }

        public List<T> getMetas<T>() where T : AlsoAttribute
        {
            List<T> result = new List<T>();
            foreach (Attribute a in metas)
            {
                if (!(a is T)) continue;
                T item = (T)a;
                result.Add(item);
            }
            return result;
        }

        public void Write(object obj, object value)
        {
            try
            {
                writeFunc(obj, value);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
                throw new Exception($"{obj.GetType()}.{Name} = {value}, can't be executed");
            }
        }

        public object Read(object obj)
        {
            try
            {
                return readFunc(obj);
            }
            catch
            {
                throw new Exception($"can't read {obj}.{Name} of type {ElementType}");
            }
        }

        public T getValue<T>(object source) => (T)Read(source);
        public void setValue<T>(object source, T value) => Write(source, value);
    }
}