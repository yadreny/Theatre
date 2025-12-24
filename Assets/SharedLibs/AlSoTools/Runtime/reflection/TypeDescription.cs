using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using static AlSo.ShortCuts;
using AlSo.Converter;
using UnityEngine;
//using System.Diagnostics;

namespace AlSo
{
    public class TypeDescriptionLibray
    {
        private TypeDescriptionLibray() { }
        public static TypeDescriptionLibray Instance { get; } = new TypeDescriptionLibray();

        private Dictionary<Type, TypeDescription> Instances { get; } = new Dictionary<Type, TypeDescription>();

        public TypeDescription GetDescription(Type type)
        {
            if (Instances.TryGetValue(type, out TypeDescription result)) return result;

            result = new TypeDescription(type);
            Instances.Add(type, result);
            return result;
        }
    }

    public class TypeDescription
    {
        public Type Type { get; }

        internal TypeDescription(Type type)
        {
            Type = type;
        }

        private Dictionary<string, PropertyDescription> _fields;
        public Dictionary<string, PropertyDescription> Fields => CreateIfNotExist(ref _fields, GetFields);

        private Dictionary<string, PropertyDescription> GetFields()
        {
            Dictionary<string, PropertyDescription> result = new Dictionary<string, PropertyDescription>();
            bool allowStatic = false;
            BindingFlags bindingFlags = allowStatic ? withStatic : noStatic;
            Type action = typeof(System.Action);
            Type exclude = typeof(ExcludeFromDescription);
            Type delegatus = typeof(Delegate);

            PropertyInfo[] propInfos = Type.GetProperties(bindingFlags);
            foreach (PropertyInfo propInfo in propInfos)
            {
                if (propInfo.PropertyType.IsExtendedFrom(delegatus)) continue;
                //if (propInfo.Name.Contains("k__BackingField")) continue;  // &&
                if (action.IsAssignableFrom(propInfo.PropertyType)) continue;
                if (propInfo.IsDefined(exclude)) continue;

                PropertyDescription pd = new PropertyDescription(propInfo);
                if (pd.isValid) result.Add(pd.Name, pd);
            }
            return result;
        }



        static List<string> FIELDS_ORDER = new List<string>() { "name", "desc", "icon", "pict" };

        const BindingFlags noStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        const BindingFlags withStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;





        IEnumerable<PropertyDescription> _all;
        public IEnumerable<PropertyDescription> All
        {
            get
            {
                if (_all == null) _all = Fields.Values.ToList();
                return _all;
            }
        }

        IEnumerable<PropertyDescription> _saveable;
        public IEnumerable<PropertyDescription> Saveable
        {
            get
            {
                if (_saveable == null)
                {
                    HashSet<PropertyDescription> res = new HashSet<PropertyDescription>();
                    foreach (PropertyDescription pd in All)
                    {
                        if (pd.isSavable) res.Add(pd);
                    }
                    _saveable = res.ToArray();
                }
                return _saveable;
            }
        }

        IEnumerable<PropertyDescription> _editableVisible;
        public IEnumerable<PropertyDescription> editableVisible => CreateIfNotExist(ref _editableVisible, ()
            => Saveable.Where(x => x.isVisible));

        IEnumerable<PropertyDescription> _editableOrdererd;
        public IEnumerable<PropertyDescription> editableOrdererd => CreateIfNotExist(ref _editableOrdererd, ()
            => editableVisible.OrderBy(x => FIELDS_ORDER.Contains(x.Name) ? FIELDS_ORDER.IndexOf(x.Name) : FIELDS_ORDER.Count + Fields.Values.ToList().IndexOf(x)));


        public PropertyDescription GetField(string fieldName)
        {
            if (fieldName == null) throw new Exception("fieldName is null");
            if (Fields == null) throw new Exception("no fields");
            if (Fields.ContainsKey(fieldName)) return Fields[fieldName];
            throw new Exception($"{Type} does't contain field: {fieldName}");
        }

        public PropertyDescription getFieldByValue(object fieldValue, object owner)
        {
            foreach (PropertyDescription fd in this.Fields.Values)
            {
                if (fd.Read(owner) == fieldValue) return fd;
            }
            throw new Exception();
        }


    }
}