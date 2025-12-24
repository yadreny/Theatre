using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
#if UNITY_EDITOR
#endif
using UnityEngine;
using UnityEngine.Networking;

namespace AlSo
{
    public class AssetBundleLoader
    {
        public static AssetBundleLoader Instance { get; } = new AssetBundleLoader();

        private static string PcLocation => "Assets/AssetBundles";
        private static string GlLocation => Application.streamingAssetsPath;

        public Action<float> OnProgress { get; set; }

        public void Forget(string name)
        {
            AssetBundle.GetAllLoadedAssetBundles().Single(x=>x.name == name).Unload(true);
        }

        public async UniTask<AssetBundle> LoadAssetBundle(string bundleName)
        {
            AssetBundle assetBundle = null;
            await UniTask.Yield();
#if UNITY_EDITOR || UNITY_STANDALONE
            assetBundle = await LoadAssetBundlePC(bundleName);
#elif UNITY_WEBGL
            assetBundle = await LoadAssetBundleGL(bundleName);
#endif
            return assetBundle;
        }

        public static async UniTask<AssetBundle> LoadAssetBundlePC(string bundleName)
        {
            string bundlePath = $"{PcLocation}/{bundleName}";// Path.Combine(PcLocation, bundleName);

            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);

            while (!request.isDone)
            {
                Instance.OnProgress?.Invoke(request.progress);
                await UniTask.NextFrame();  // Await until the next frame
            }

            if (request.assetBundle == null)
            {
                Debug.LogError($"pc cant load: {bundlePath}");
            }
            return request.assetBundle;
        }


        public static AssetBundle LoadAssetBundlePCSync(string bundleName)
        {
            if (Instance.IsLoaded(bundleName)) return Instance.GetByName(bundleName);

            //Debug.LogError($"loading bundle {bundleName}");
            string bundlePath = $"{PcLocation}/{bundleName}";// Path.Combine(PcLocation, bundleName);
            var result = AssetBundle.LoadFromFile(bundlePath);
            if (result == null) Debug.LogError($"pc cant load: {bundlePath}");
            return result;
        }


        public static async UniTask<AssetBundle> LoadAssetBundleGL(string bundleName)
        {
            string bundlePath = Path.Combine(GlLocation, bundleName);

            using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath))
            {
                var operation = uwr.SendWebRequest();

                while (!operation.isDone)
                {
                    Instance.OnProgress?.Invoke(operation.progress);
                    await UniTask.NextFrame(); 
                }

                if (uwr.result != UnityWebRequest.Result.Success) Debug.LogError($"Failed to download AssetBundle: {uwr.error}");
                else
                {
                    AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                    return bundle;
                }
            }
            return null;
        }

        public bool IsLoaded(string name) => AssetBundle.GetAllLoadedAssetBundles().Any(x=>x.name == name); 

        public AssetBundle GetByName(string name)
        {
            var bundles = AssetBundle.GetAllLoadedAssetBundles();
            foreach (var bundle in bundles) 
            {
                if (bundle.name == name) return bundle;
            }
            Debug.LogError($"{name} was not added to bundles");
            return null;
        }
        
    }
}