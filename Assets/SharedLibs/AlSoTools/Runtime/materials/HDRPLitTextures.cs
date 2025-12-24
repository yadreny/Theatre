using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSo
{
    public class HDRPLitTextures
    {
        public readonly static string Diffuse = "_BaseColorMap";
        public readonly static string Normal = "_NormalMap";
        public readonly static string MaskMap = "_MaskMap";

        public readonly static string[] Textures = new string[] { Diffuse, Normal, MaskMap };
    }

}
