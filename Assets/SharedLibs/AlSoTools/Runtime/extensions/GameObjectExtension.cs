using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AlSo
{
    public static class GameObjectExtension
    {
        public static GameObject CreateInstanceOfPrefab(this GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject instance = null;
#if UNITY_EDITOR            
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
#else
            instance = GameObject.Instantiate<GameObject>(prefab);
#endif
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.SetParent(parent);
            return instance;
        }

        public static GameObject CreateInstanceOfPrefab(this GameObject prefab, Transform parent)
        {
            GameObject instance = null;
#if UNITY_EDITOR            
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
#else
            instance = GameObject.Instantiate<GameObject>(prefab);
#endif
            instance.transform.SetParent(parent);
            return instance;
        }


        public static Transform GetChildByName(this Transform transform, string name)
        {
            Transform ReturnObj;
            if (transform.name == name)
            {
                return transform.transform;
            }

            foreach (Transform child in transform)
            {
                ReturnObj = GetChildByName(child, name);
                if (ReturnObj != null)
                {
                    return ReturnObj;
                }
            }
            return null;
        }

        public static void SetParent(this GameObject child, GameObject parent) => child.transform.SetParent(parent.transform);


        public static void IgnoreRaycast(this GameObject obj)
        {
            obj.layer = 2;  // коллайдеры не мешают кликать насковзь
        }

        public static List<T> GetAllComponentsInChildren<T>(this GameObject obj, List<T> founded = null) where T : Component
        {
            List<GameObject> children = obj.GetAllChildren();
            List<T> res = new List<T>();

            foreach (GameObject child in children)
            {
                T comp = child.GetComponent<T>();
                if (comp != null) res.Add(comp);
            }

            return res;
        }

        public static string HierarchyName(this GameObject obj)
        {
            Transform t = obj.transform;
            string s = "";
            while (t != null)
            {
                s = t.name + "." + s;
                t = t.parent;
            }
            return s;
        }

        public static GameObject Unparent(this GameObject gameObject)
        {
            gameObject.transform.SetParent(null);
            return gameObject;
        }

        public static GameObject Destory(this GameObject gameObject)
        {
            GameObject.Destroy(gameObject);
            return gameObject;
        }

        public static List<GameObject> GetAllChildren(this GameObject obj, List<GameObject> history = null)
        {
            history = history != null ? history : new List<GameObject>();
            List<GameObject> children = obj.GetChildren();
            foreach (GameObject child in children)
            {
                history.Add(child);
                child.GetAllChildren(history);
            }
            return history;
        }

        public static GameObject FindInactive(this GameObject parent, string name)
        {
            Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in trs)
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }
            return null;
        }

        public static GameObject Find(this GameObject obj, string name)
        {
            if (obj.name == name) return obj;

            foreach (GameObject gobj in obj.GetChildren())
            {
                GameObject res = gobj.Find(name);
                if (res != null)
                {
                    return res;
                }
            }
            return null;
        }


        public static List<GameObject> GetChildren(this GameObject obj)
        {
            List<GameObject> result = new List<GameObject>();

            for (int i = 0; i < obj.transform.childCount; i++)
            {
                result.Add(obj.transform.GetChild(i).gameObject);
            }

            return result;
        }

        public static T Find<T>(this GameObject obj) where T : Component
        {
            T res = obj.GetComponent<T>();
            if (res != null) return res;

            foreach (GameObject gobj in obj.GetChildren())
            {
                res = gobj.Find<T>();
                if (res != null) return res;
            }
            return null;
        }

        public static void RemoveAllChildren(this Transform transform)
        {
            while (transform.childCount > 0)
            {
                Transform child = transform.GetChild(0);
                child.SetParent(null);
                if (Application.isPlaying)
                {
                    child.gameObject.Destory();
                }
                else
                { 
                    GameObject.DestroyImmediate(child.gameObject);  
                }
            }
        }

        public static bool IsDestroyed(this GameObject gameObject)
        {
            // UnityEngine overloads the == opeator for the GameObject type
            // and returns null when the object has been destroyed, but 
            // actually the object is still there but has not been cleaned up yet
            // if we test both we can determine if the object has been destroyed.
            return gameObject == null;// && !ReferenceEquals(gameObject, null);
        }

 

    }
}