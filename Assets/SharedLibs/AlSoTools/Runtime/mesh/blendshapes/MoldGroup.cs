using AlSo;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.SharedLibs.AlSoTools.Runtime.mesh.blendshapes
{
    public class MoldGroup : ScriptableObject, IMoldGroup
    {
        public Mold[] elements;
        public IEnumerable<IMold> Elements => elements.OfType<IMold>();
        
    }
}