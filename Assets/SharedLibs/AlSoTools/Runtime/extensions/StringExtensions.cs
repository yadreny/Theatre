using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlSo
{
    public static class StringExtensionMethods
    {
        public static bool iEndsWith(this string a, string b)
        {
            return a.EndsWith(b, System.StringComparison.InvariantCultureIgnoreCase);
        }

        public static string FileName(this string path) => path.Split('/').Last();

        public static string ParentFolderPath(this string path) => string.Join('/', path.Split('/').Reverse().Skip(1).Reverse().ToArray());

        public static string FolderName(this string path) => path.Split('/').Last();

        public static string FirstCharToUpper(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => input[0].ToString().ToUpper() + input.Substring(1)
            };

        public static bool containsOneOfChar(this string s, string line, out char c)
        {
            c = 'a';
            for (int i = 0; i < line.Length; i++)
            {
                c = line[i];
                if (s.Contains(c.ToString()))
                {
                    return true;
                }
            }
            return false;
        }


        public static string listed(this List<string> lines)
        {
            string result = String.Empty;
            lines.ForEach(x => result = $"{result}\n{x}");
            return result;
        }

        public static string Join(this IEnumerable<string> items, string separator)
        {
            if (items.Count() == 0) return "";
            return String.Join(separator, items.ToArray());
        }

        public static string FirstToUpper(this string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static string Escape(this string text, IEnumerable<string> members)
        {
            foreach (string memeber in members)
            {
                text = text.Replace(memeber, string.Empty);
            }
            return text;
        }

        public static string Strike(this string s)
        {
            string strikethrough = "";
            foreach (char c in s)
            {
                strikethrough = strikethrough + c + '\u0336';
            }
            return strikethrough;
        }


        public static string RemoveSpaces(this string str)
        {
            while (str.Contains(" "))
            {
                str = str.Replace(" ", "");
            }
            return str;
        }

        public static string[] Split(this string str, string separator)
        {
            string[] array = str.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            return array;
        }

        public static bool IsGood(this string str)
        {
            return !(str == null || str == string.Empty);
        }

        public static Color ToColor(this string hex)
        {
            try
            {
                hex = hex.Replace("#", String.Empty);

                byte a = 255;//assume fully visible unless specified in hex
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                //Only use alpha if the string has enough characters
                if (hex.Length == 8)
                {
                    a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                }
                return new Color32(r, g, b, a);
            }
            catch
            {
                Debug.LogError("color problem " + hex);
            }
            return new Color();
        }

        public static string Render(this string template, Dictionary<string, string> data)
        {
            foreach (string key in data.Keys)
            {
                string token = "$" + key;
                template = template.Replace(token, data[key]);
            }
            return template;
        }

        public static string ReplaceFirst(this string text, string search, string replace, int start, out int cursor)
        {
            string prev = text.Substring(0, start);
            text = text.Substring(start, text.Length - start);
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                cursor = start;
                return prev + text;
            }
            string res = prev + text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
            cursor = start + pos + replace.Length;
            return res;
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static string ToRoman(this int number)
        {
            if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900);
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            throw new ArgumentOutOfRangeException("something bad happened");
        }
    }
}
