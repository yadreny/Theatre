//#region import
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using UnityEngine.UI;
//using AlSo;
//using static AlSo.ShortCuts;
//using System.Threading;
//using System.Globalization;
//using UnityEngine.Rendering.HighDefinition;
//#endregion

//namespace Runtime
//{
//    public class DecalProjectorCreator
//    {
//        public const float FADE = 0.3f;
//        public static DecalProjectorCreator instance { get; private set; } = new DecalProjectorCreator();

//        public DecalProjector getNewInstance()
//        {
//            GameObject obj = new GameObject("decal projector");
            
//            obj.transform.rotation = Quaternion.Euler(90, 0, 0);
//            DecalProjector proj = obj.AddComponent<DecalProjector>();
//            return proj;
//        }

//    }
//}
