using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSo
{
    public interface IAsloAttribute { }
    public abstract class AlsoAttribute : Attribute, IAsloAttribute
    {
        public override int GetHashCode()
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        }
    }

    public static class AlsoAttributeReader
    {
        public static T GetClassMeta<T>(this Type t) where T : IAsloAttribute
        {
            return Attribute.GetCustomAttributes(t).OfType<T>().LastOrDefault();
        }

        public static IEnumerable<T> GetClassMetas<T>(this Type t) where T : IAsloAttribute
        {
            return Attribute.GetCustomAttributes(t).OfType<T>();
        }

    }

    public abstract class EscoOwnableMeta : AlsoAttribute
    {
        public PropertyDescription Owner { get; set; }
    }

    public class DontShow : AlsoAttribute { }

    public class ExcludeFromDescription : AlsoAttribute { }

    //public interface ISerializableValue { }

    public class IgroneProperty : AlsoAttribute { }
}
