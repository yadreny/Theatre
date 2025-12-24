using System;
using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class DropExtension
    {
        public static Dictionary<Dropdown.OptionData, T> ToOptionDict<T>(this List<T> list, Func<T, string> convFunc = null)
        {
            if (convFunc == null)
            {
                convFunc = x => x.ToString();
            }

            Dictionary<Dropdown.OptionData, T> res = new Dictionary<Dropdown.OptionData, T>();

            foreach (T elem in list)
            {
                res.Add(new Dropdown.OptionData(convFunc(elem)), elem);
            }
            return res;
        }

    }
}
