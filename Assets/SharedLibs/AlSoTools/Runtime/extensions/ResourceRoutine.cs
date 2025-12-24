using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AlSo
{
    public class ResourceRoutine
    {
        static Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
        static Dictionary<string, GameObject> gameObjs = new Dictionary<string, GameObject>();
        static Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
        static Dictionary<string, Material> materials = new Dictionary<string, Material>();

        public static Material PathToMaterial(string path)
        {
            return GetOrLoadResource(materials, path);
        }

        public static Texture PathToTexture(string path)
        {
            var q = GetOrLoadResource(textures, path);
            return q;
        }

        public static GameObject PathToGameObjectInstance(string path, Vector3 position = default, Transform parent = null)
        {
            if (path == null) return null;
            GameObject prefab = GetOrLoadResource(gameObjs, path);
            return GameObject.Instantiate(prefab, position, Quaternion.identity, parent) as GameObject;
        }

        //public static GameObject PathToGameObjectInstance(string path, Transform parent = null)
        //{
        //    return PathToGameObjectInstance(path, Vector3.zero, parent);
        //}

        public static GameObject PathToPrefab(string path) => GetOrLoadResource(gameObjs, path);

        public static AudioClip PathToAudioClip(string path) => GetOrLoadResource(audioClips, path);

        static T GetOrLoadResource<T>(Dictionary<string, T> dict, string path) where T : UnityEngine.Object
        {
            if (path == null) return null;
            if (!dict.ContainsKey(path))
            {
                object obj = Resources.Load(path);
                if (obj == null)
                {
                    throw new Exception("bad path: " + path);
                }
                dict.Add(path, obj as T);
            }
            return dict[path];
        }


        //public static Texture2D LoadFromFile(string path)
        //{
        //    string filePath = "Assets/" + path;
        //    Texture2D tex = null;
        //    byte[] fileData = File.ReadAllBytes(filePath);
        //    tex = new Texture2D(2, 2);
        //    tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.

        //    return tex;
        //}

        public static List<string> GetFilesInFolder(string folder, bool removeExtension = true)
        {
            string absolutePath = Application.dataPath + "/Resources/" + folder;
            string[] paths = Directory.GetFiles(absolutePath);

            List<string> result = new List<string>();

            foreach (string path in paths)
            {
                if (path.Split(".").Last() == "meta") continue;
                string cleaned = path.Replace(absolutePath, "");
                if (removeExtension)
                {
                    List<string> parts = cleaned.Split(".").ToList();
                    parts.Pop();
                    cleaned = string.Join(".", parts.ToArray());
                }
                result.Add(folder + cleaned);
            }
            return result;
        }


    }
}