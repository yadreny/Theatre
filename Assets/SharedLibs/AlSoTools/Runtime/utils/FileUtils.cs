using System;
using System.IO;
using System.Text;

namespace AlSo
{
    public static class FileUtils
    {
        public static bool WriteToFile(this string data, string path)
        {
            try
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(data);
                }
                return true;
            }
            catch 
            {
                throw new Exception($"can't create file at {path}");
            };
        }

        public static string Load(string filePath, bool allowNotExist = false)
        {
            bool exist = File.Exists(filePath);
            if (!exist)
            {
                UnityEngine.Debug.LogError("Can't load file from " + Path.GetFullPath(filePath));
                if (allowNotExist) return null;
                return null;
                //else throw new Exception();
            }
            //UnityEngine.Debug.Log($"file exists {filePath}");
            string result = File.ReadAllText(filePath, Encoding.UTF8);
            //UnityEngine.Debug.Log($"file loaded {filePath}");
            return result;
        }
    }
}