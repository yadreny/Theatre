using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

namespace AlSo
{
    public static class SceneTools
    {
        public static (bool succ, Scene scene) GetActiveSceneByName(string name)
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == name) return (true, scene);
            }
            return (false, default);
        }

        public static Transform[] AllTransforms
        {
            get
            {
                Scene scene = SceneManager.GetActiveScene();
                Transform[] rootObjects = scene.GetRootGameObjects().Select(x => x.transform).ToArray();
                Transform[] allChildren = rootObjects.Union(rootObjects.SelectMany(x => x.GetComponentsInChildren<Transform>())).ToArray();
                return allChildren;
            }
        }

        public static string CurrentScenePath => SceneManager.GetActiveScene().path;

        public static string CurrentSceneFolderPath => SceneManager.GetActiveScene().path.Split('/').Reverse().Skip(1).Reverse().ToArray().Join("/");
    }
}