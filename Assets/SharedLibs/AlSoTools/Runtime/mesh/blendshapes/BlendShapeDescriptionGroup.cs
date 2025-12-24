using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AlSo
{
    public interface IBlendShapeSource
    {
        IEnumerable<string> GetBlendShapeNames();   
    }

    //public abstract class AbsBlendShapeSource : ScriptableObject, IBlendShapeSource
    //{
    //    public abstract IEnumerable<string> GetBlendShapeNames();
    //}

    public class BlendShapeDescriptionGroup : ScriptableObject, IBlendShapeDescriptionGroup
    {
        public SkinnedMeshRenderer source;
        public BlendShapeDescription[] descriptions;
        public IEnumerable<IBlendShapeDescription> Descriptions => descriptions.OfType<IBlendShapeDescription>().ToArray();
    }

    //created with BlendShapeDescriptionGroupGenerator
}