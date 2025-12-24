using System;
using UnityEngine;

namespace AlSo
{
    public static class TextureBase64Ext
    {
        public static Texture2D Base64Decode(this string str)
        {
            str = str.Contains(PREFIX) ? str.Replace(PREFIX, string.Empty) : str;
            byte[] newBytes = Convert.FromBase64String(str);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(newBytes);
            return tex;
        }

        public static string ToJpgBase64(this Texture2D texture)
        {
            byte[] bytes = texture.EncodeToJPG();
            string s = Convert.ToBase64String(bytes);
            return s;
        }

        public static readonly string PREFIX = "data:image/png;base64,";

        public static string ToPngBase64WithPrefix(this Texture2D texture)
        {
            string result = $"{PREFIX}{texture.ToPngBase64()}";
            return result;
        }

        public static string ToPngBase64(this Texture2D texture)
        {
            byte[] bytes = texture.EncodeToPNG();
            string base64Image = Convert.ToBase64String(bytes);
            
            //int padding = 3 - ((base64Image.Length + 3) % 4);
            //base64Image = base64Image.PadRight(base64Image.Length + padding, '=');

            return base64Image;
        }
    }
}
