using AlSo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public interface ISerializableData
    {
        ISerizalizer Serializer => AlSo.Serializer.SimpleImageToBase64;
        Dictionary<string, object> GetParams(bool debug=false) => Serializer.GetParams(this, SkipNulls, debug);
        bool SkipNulls => true;
    }

    public interface ISerizalizer
    {
        Dictionary<string, object> GetParams(object obj, bool skipNulls, bool debug);
    }

    public class Serializer : ISerizalizer
    {
        public static Serializer SimpleImageToBase64 { get; } = new Serializer(
            new Dictionary<Type, Func<object, object>>()
            {
                { typeof(Texture2D), (x)=> (x as Texture2D).ToPngBase64WithPrefix() },
            },
            new Dictionary<Type, Func<object, object>>()
            {
                { typeof(Texture2D), (x)=> "img" },
            }
        );

        protected Dictionary<Type, Func<object, object>> Converter { get; }
        protected Dictionary<Type, Func<object, object>> DebugConverter { get; }
        public Serializer(Dictionary<Type, Func<object, object>> converter, Dictionary<Type, Func<object, object>> debugConverter=null)
        {
            this.Converter = converter;
            DebugConverter = debugConverter;    
        }

        private object Convert(object source, bool debug)
        {
            if (source == null) return null;
            Type type = source.GetType();
            ISerializableData serializable = source as ISerializableData;
            if (serializable != null)
            {
                return serializable.GetParams(debug);
            }
            Dictionary<Type, Func<object, object>> converter = debug ? DebugConverter : Converter;
            bool needConverter = converter.TryGetValue(type, out Func<object, object> converterFunc);
            object result = needConverter ? converterFunc(source) : source;
            return result;
        }

        private object GetValue(object obj, PropertyDescription prop, bool debug)
        {
            debug = debug && DebugConverter!= null; 
            object value = prop.Read(obj);
            if (prop.IsSingle) return Convert(value, debug);
            IList list = value as IList;
            List<object> result = new List<object>();
            foreach (object item in list) result.Add(Convert(item, debug));
            return result;
        }

        public Dictionary<string, object> GetParams(object obj, bool skipNulls, bool debug)
        {
            Dictionary<string, object> postData = new();
            IEnumerable<PropertyDescription> props = TypeDescriptionLibray.Instance.GetDescription(obj.GetType()).Fields.Values;
            foreach (PropertyDescription property in props)
            {
                object value = GetValue(obj, property, debug);
                if (value == null && skipNulls) continue;
                postData.Add(property.Name, value);
            }
            return postData;
        }
    }
}