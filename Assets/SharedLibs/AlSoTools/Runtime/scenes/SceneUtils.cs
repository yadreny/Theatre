using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlSo
{
    public static class SceneUtils
    {
        public static Scene RootScene
        {
            get
            {
                var res = SceneManager.GetSceneAt(0);
                //Debug.LogError(res.name);
                return res;
            }
        }
        public static Scene AddedScene => SceneManager.GetSceneAt(1);

        public static GameObject Get(Scene scene, string name) => scene.GetRootGameObjects().SingleOrDefault(x=>x.name == name);   

        public static GameObject Find(this Scene scene, string name)=> Get(scene, name);


        public static void Freeze(this Scene scene) => scene.GetRootGameObjects()
            .SelectMany(x=>x.GetAllChildren())
            .Union(scene.GetRootGameObjects())
            .ForEach(x => x.SetActive(false));
}

    public class SceneExit
    {
        public static SceneExit Instance { get; } = new SceneExit();

        public Action<string> OnSceneExit { get; set; }

        private SceneExit() { }
    }


    
}
