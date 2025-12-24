using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AlSo
{
    public interface IBlendShapeDescriptionGroup 
    {
        IEnumerable<IBlendShapeDescription> Descriptions { get; }
    }
}