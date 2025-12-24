using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace AlSo.Converter
{

    public class AsID : Attribute { }

    public class Converter 
    {
        private static string OBJECT_TOKEN = "_id";
        private static string TYPE_TOKEN = "_type";
        private static Dictionary<Type, ConvTypeDescription> descriptions = new Dictionary<Type, ConvTypeDescription>();
        private static List<Type> simpleTypes = new List<Type>(){ typeof(string), typeof(int), typeof(Int32), typeof(Int16), typeof(Int64), typeof(bool), typeof(float), typeof(double) };

        private Dictionary<object, int> _casheSave = new Dictionary<object, int>();
        private Dictionary<int, object> _casheLoad = new Dictionary<int, object>();

        private bool _addTypes;
        private bool _skipSame;

        public static IJsonUtils ConverterUtils { get; set; } = JsonMiniConverter.Instance;

        public static object FromJson(Type type, string jsonString)
        {
            Converter typer = new Converter();
            return typer.ObjectFromJson(type, jsonString);
        }

        public static T FromJson<T>(string jsonString)
        {
            Converter typer = new Converter();
            return typer.ObjectFromJson<T>(jsonString);
        }

        private object ObjectFromJson(Type type, string jsonString)
        {
            object deserialized = ConverterUtils.Deserialize(jsonString);
            Dictionary<string, object> data = deserialized as Dictionary<string, object>;
            if (data == null) throw new Exception("invalid json data: " + jsonString);

            this._addTypes = data.ContainsKey(TYPE_TOKEN);
            this._skipSame = data.ContainsKey(OBJECT_TOKEN);
            object result = RestoreFromDictionary(data, type);
            return result;
        }

        private T ObjectFromJson<T>(string jsonString)
        {
            return (T)ObjectFromJson(typeof(T), jsonString);
        }

        private object RestoreFromDictionary(Dictionary<string, object> data, Type t)
        {
            //Debug.Log("type " + t + ", data = " + data);

            int id =0;
            if (this._skipSame)
            {
                //Debug.Log("?>" + data[OBJECT_TOKEN]);

                id = Convert.ToInt32(data[OBJECT_TOKEN]);
                if (_casheLoad.ContainsKey(id))
                {
                    return _casheLoad[id];
                }            
            }

            Type type = null;
            if (this._addTypes)
            {
                string typeName = data[TYPE_TOKEN] as string;
                type = Type.GetType(typeName);
            }
            else type = t;

            ConvTypeDescription description = GetDescription(type);
            object result = Activator.CreateInstance(type);

            if (_skipSame) _casheLoad.Add(id, result);

            object fieldValue;
            object fieldData;

            foreach (string key in description.fieldTypes.Keys)
            {
                if (!data.ContainsKey(key)) continue;
                if (data[key] == null) continue;

                fieldData = data[key];
                fieldValue = null;

                if (description.isLists[key])
                {
                   fieldValue = RestoreList(description.childTypes[key], fieldData as List<object>);
                }
                else
                {
                    Type fieldType = description.fieldTypes[key];
                    fieldValue = simpleTypes.Contains(fieldType) ? TypeHelp(fieldType,fieldData) : RestoreFromDictionary(fieldData as Dictionary<string, object>, fieldType);
                }
                description.Write(result, key, fieldValue);
            }
            return result;
        }

        object RestoreSubst(object obj, Type listType)
        {
            if (obj == null) return null;
            Type type = obj.GetType();
            bool isSimple = simpleTypes.Contains(type);

            //UnityEngine.Debug.LogError($"{obj} of type {listType} isSimple{isSimple}");

            return isSimple ? TypeHelp(type, obj) : RestoreFromDictionary(obj as Dictionary<string, object>, listType);
        }

        private IList RestoreList(Type childType, List<object> elems)
        {
            Type listType = typeof(List<>);
            Type constructedListType = listType.MakeGenericType(childType);
            IList result = Activator.CreateInstance(constructedListType) as IList;

            //UnityEngine.Debug.LogError(result.GetType());

            for (int i = 0; i < elems.Count; i++)
            {
                var source = elems[i];
                var res = RestoreSubst(source, childType);
                //UnityEngine.Debug.LogError($"{source} => {res}, {res.GetType().ToString()}");
                result.Add(res);
            }
            return result;
        }

        private object TypeHelp(Type type, object obj)
        {
            if (type == typeof(float)) return Convert.ToSingle(obj);
            if (type == typeof(Int32)) return Convert.ToInt32(obj);
            if (type == typeof(Int64)) return Convert.ToInt32(obj);
            return obj;
        }

        //-----------------------------------------------------------------------------------------------------------------------

        public static string ToJson(object obj)
        {
            return ToJson(obj, false, false);
        }

        public static string ToJson(object obj, bool skipSame)
        {
            return ToJson(obj, skipSame, false);
        }

        public static string ToJson(object obj, bool skipSame, bool addTypes) //=false // = false
        {
            Converter typer = new Converter();
            return typer.ObjectToJson(obj, skipSame, addTypes );
        }

        private string ObjectToJson(object obj, bool skipSame=true, bool addTypes=true)
        {
            this._addTypes = addTypes;
            this._skipSame = skipSame;
            Dictionary<string, object> data = ObjectToDictionary(obj);
            string result = ConverterUtils.Serialize(data);
            return result;
        }

        private object SubstToObject(object obj)
        {
            if (obj == null) return null;
            if (obj is Enum) return obj.ToString();

            bool isSimple = simpleTypes.Contains(obj.GetType());
            if (isSimple) return obj;

            IList list = obj as IList;
            if (list!=null) return ListToArray(list);
            else return ObjectToDictionary(obj);
        }

        public Dictionary<string, object> ObjectToDictionary(object obj)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            Type type = obj.GetType();

            //bool isUsedObject = _casheSave.ContainsKey(obj);
            //if (isUsedObject && _skipSame)
            //{
            //     data.Add(OBJECT_TOKEN, _casheSave[obj]);
            //     return data;
            //}
            //else
            //{
            //    _counter++;
            //    if (_skipSame)
            //    {
            //        _casheSave.Add(obj, _counter);
            //        data.Add(OBJECT_TOKEN, _counter);
            //    } 
            //    if (_addTypes) data.Add(TYPE_TOKEN, TypeToken(type));
            //}
        
            ConvTypeDescription description = GetDescription(type);
            object fieldData;
            object fieldValue;

            foreach (string key in description.fields.Keys)
            {
                bool asId = description.fields[key].GetCustomAttribute<AsID>() != null;

                fieldValue = description.Read(obj,key);
                fieldData = asId ? SubstToId(fieldValue) : SubstToObject(fieldValue);

                data.Add(key, fieldData);
            }
            return data;
        }

        private object SubstToId(object obj) 
        {
            if (obj== null) return null;

            if (obj is Array)
            {
                Array array = obj as Array;
                List<object> objs = new List<object>();
                foreach (object o in array) 
                {
                    var item = SubstToId(o);
                    objs.Add(item);
                }
                return objs.ToArray();
            }

            try
            {
                var id = obj.GetType().GetProperties().Single(x => x.Name == "id").GetValue(obj);
                return id;
            }
            catch
            {

                return SubstToObject(obj);
                //throw new Exception($"cant get id on " + obj);
            }
        }

        private object[] ListToArray(IList list)
        {
            object[] result = new object[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                //result[i] = SubstToObject(list[i]);
                result[i] = SubstToId(list[i]);
            }
            return result;
        }

        private static ConvTypeDescription GetDescription(Type type)
        {
            if (!descriptions.ContainsKey(type)) descriptions.Add(type, ConvTypeDescription.Describe(type));
            return descriptions[type];
        }

        private static string TypeToken(Type type)
        {
            string info = type.AssemblyQualifiedName;
            string[] parts = info.Split(',');
            return parts[0] + "," + parts[1];
        }


        private class ConvTypeDescription
        {
            public Type type;
            public Dictionary<string, Type> fieldTypes  = new Dictionary<string, Type>();
            public Dictionary<string, bool> isLists = new Dictionary<string, bool>();
            public Dictionary<string, Type> childTypes = new Dictionary<string, Type>();
            public Dictionary<string, MemberInfo> fields = new Dictionary<string, MemberInfo>();

            public static ConvTypeDescription Describe(Type type)
            {
                ConvTypeDescription result = new ConvTypeDescription();
                result.DescribeType(type);
                return result;
            }

            public void DescribeType(Type t)
            {
                type = t;
                Type childType, elemType;
                bool isList;
                string key;

                FieldInfo[] infos = type.GetFields();

                foreach (FieldInfo info in infos)
                {
                    if (info.IsStatic) continue;
                    key = info.Name;
                    childType = info.FieldType;
                    isList = childType.IsGenericType;

                    fieldTypes.Add(key, childType);
                    fields.Add(key, info);

                    isLists.Add(key, isList);

                    if (isList)
                    {
                        elemType = childType.GetGenericArguments()[0];
                        childTypes.Add(key, elemType);
                    }
                }

                PropertyInfo[] pinfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (PropertyInfo pinfo in pinfos)
                {
                    if (((pinfo.CanRead && pinfo.CanWrite) || pinfo.GetCustomAttribute<Matters>()!=null )&& pinfo.GetIndexParameters().Length == 0)
                    {
                        //Debug.Log(pinfo.Name + " " + pinfo.GetIndexParameters().Length);

                        key = pinfo.Name;
                        childType = pinfo.PropertyType;
                        isList = childType.IsGenericType;

                        fieldTypes.Add(key, childType);
                        fields.Add(key, pinfo);

                        isLists.Add(key, isList);

                        if (isList)
                        {
                            elemType = childType.GetGenericArguments()[0];
                            childTypes.Add(key, elemType);
                        }
                    }
                }
            }

            public void Write(object obj, string key, object fieldValue)
            {
                MemberInfo mi = fields[key];

                FieldInfo fi = mi as FieldInfo;
                if (fi != null)
                {
                    fi.SetValue(obj, fieldValue);
                }
                PropertyInfo pi = mi as PropertyInfo;
                if (pi != null)
                {
                    pi.SetValue(obj, fieldValue, null);
                }
            }

            public object Read(object obj, string key)
            {
                MemberInfo mi = fields[key];
                FieldInfo fi = mi as FieldInfo;
                if (fi != null)
                {
                    return fi.GetValue(obj);
                }
                PropertyInfo pi = mi as PropertyInfo;

                if (pi != null && pi.CanRead && pi.GetIndexParameters().Length == 0)
                {
                    return pi.GetValue(obj, null);
                }
                return null;
            }

        }
    }
}
