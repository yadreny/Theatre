//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;

//namespace AlSo
//{
//    public static class StringUtils
//    {



//        //public static string join(this IEnumerable<string> lines, string connector)
//        //{
//        //    if (lines.Count() == 0) return null;

//        //    string result = "";
//        //    bool first = true;
//        //    foreach (string s in lines)
//        //    {
//        //        result = result + (first ? "" : connector) + s;
//        //        first = false;
//        //    }

//        //    return result;
//        //}

//        //public static string ReplaceFirst(this string text, string search, string replace, int start, out int cursor)
//        //{
//        //    string prev = text.Substring(0, start);
//        //    text = text.Substring(start, text.Length - start);
//        //    int pos = text.IndexOf(search);
//        //    if (pos < 0)
//        //    {
//        //        cursor = start;
//        //        return prev + text;
//        //    }
//        //    string res = prev + text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
//        //    cursor = start + pos + replace.Length;
//        //    return res;
//        //}

//        //public static string ReplaceFirst(this string text, string search, string replace)
//        //{
//        //    int pos = text.IndexOf(search);
//        //    if (pos < 0)
//        //    {
//        //        return text;
//        //    }
//        //    return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
//        //}
//    }
//}