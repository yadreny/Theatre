using System;
using System.Linq;
using UnityEngine;
using static AlSo.ShortCuts;

namespace AlSo
{
    public class Matters : Attribute { }

    public class BundleLocation : PrefabLocation, IPrefabLocation, IAsloAttribute
    {
        protected string BundleName { get; }
        public BundleLocation(string bundleName, string path) : base(path) 
        {
            this.BundleName = bundleName;
        }

        protected override GameObject GetPrefab()
        {
            if (BundleName == null) return base.GetPrefab();

            AssetBundle bundle = AssetBundleLoader.Instance.GetByName(BundleName);

            if (bundle == null) Debug.LogError($"no bundle {BundleName}");
            GameObject res = bundle.LoadAsset<GameObject>(Path);
            if (res == null)
            {
                Debug.LogError($"bundle {BundleName} has no {Path}");
                foreach (string p in bundle.GetAllAssetNames()) Debug.LogError(p);
            }
            return res;
        }
    }

    public interface IPrefabLocation : IAsloAttribute
    {
        GameObject InstantinateGameObject();
    }

    public class PrefabLocation : AlsoAttribute, IPrefabLocation, IAsloAttribute
    {
        public PrefabLocation(string path)
        {
            this.Path = path;
        }

        public string Path { get; private set; }

        protected GameObject _prefab;
        public GameObject Prefab => _prefab ??= GetPrefab();
        
        protected virtual GameObject GetPrefab()
        {
            GameObject res = Resources.Load<GameObject>(Path);
            if (res == null) Debug.LogError($"bad path {Path}");
            return res;
        }

        public GameObject InstantinateGameObject() => GameObject.Instantiate<GameObject>(Prefab);

        public T InstantinateToParent<T>(Transform parent) where T : MonoBehaviour
        {
            Type type = typeof(T);
            GameObject res = GameObject.Instantiate<GameObject>(Prefab, parent);
            RectTransform rt = res.GetComponent<RectTransform>();
            if (rt) rt.localPosition = new Vector2();

            T attached = res.GetComponent<T>();
            if (attached != null) Debug.LogError($"{type} has compontent on prefab: {Path}");

            return res.AddComponent<T>();
        }

    }

  

    public class CapitalizeFieldNames : AlsoAttribute { }

    

}