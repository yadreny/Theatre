using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static AlSo.ShortCuts;

namespace AlSo
{
    public static class CameraExtensions
    {
        //private static Material _normaler;
        //private static Material Normaler => CreateIfNotExist(ref _normaler, ()=> Resources.Load<Material>("Normaler"));

        //private static Material _depther;
        //private static Material Depther => CreateIfNotExist(ref _depther, () => Resources.Load<Material>("Depther"));

        //private static Texture2D GetNormals(int width, int height, int mult) => Normaler.RenderToTexture(width*mult, height * mult).ResizeMe(width);
        //private static Texture2D GetDepths(int width, int height, int mult) => Depther.RenderToTexture(width * mult, height * mult).ResizeMe(width);

        //public static void SaveMaps(int width, int height, int mult)
        //{
        //    GetNormals(width, height, mult).SaveTo("Assets/depth/Resources/resultN.png");
        //    GetDepths(width, height, mult).SaveTo("Assets/depth/Resources/resultD.png");
            
        //    Debug.LogError("done");
        //}

    }
}
