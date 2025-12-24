using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace AlSo
{
    public enum BlendShapeGroup ////face-upper-eyes
    {
        //
        Undefined,

        Body,

        // upperFace
        UpperScull,
        Eyes,
        Ears,
        Nose,

        // lowerFace
        LowerSkull,
        Mouth,
        Cheeks,
        Jaw,
        Chin,

        //
        Lineage,
        Variation,
        Expression,
        Speech,
        Look,
        Complection,
    }

    public interface IBlendShapeDescription
    {
        string Name { get; }
        //int Index { get; }
        BlendShapeGroup Group { get; }
        bool IsComportable(Mesh mesh);

        public void TransplantateFromTo(Mesh sourceMesh, Mesh targetMesh)
        {
            Vector3[] positions = new Vector3[sourceMesh.vertexCount];
            Vector3[] normals = new Vector3[sourceMesh.vertexCount];
            Vector3[] tangents = new Vector3[sourceMesh.vertexCount];

            int index = sourceMesh.GetBlendShapeIndex(Name);
            sourceMesh.GetBlendShapeFrameVertices(index, 0, positions, normals, tangents);
            targetMesh.AddBlendShapeFrame(Name, 1, positions, normals, tangents);
        }
    }

    public static class BlendShapeGroupConstants
    {
        public static readonly BlendShapeGroup[] FaceAffectors = new BlendShapeGroup[]
        {
            BlendShapeGroup.Lineage,
            BlendShapeGroup.Body,

            BlendShapeGroup.UpperScull,
            BlendShapeGroup.Eyes,
            BlendShapeGroup.Ears,
            BlendShapeGroup.Nose,

            BlendShapeGroup.LowerSkull,
            BlendShapeGroup.Mouth,
            BlendShapeGroup.Cheeks,
            BlendShapeGroup.Jaw,
            BlendShapeGroup.Chin,
        };
    }
    
    [Serializable]
    public struct BlendShapeDescription : IBlendShapeDescription
    {
        public string name;
        public string Name => name;

        public BlendShapeGroup group;
        public BlendShapeGroup Group => group;

        public BlendShapeDescription(string name, BlendShapeGroup group = BlendShapeGroup.Undefined)
        {
            this.name = name;
            this.group = group;
        }

        public bool IsComportable(Mesh mesh) => true;
    }

  

    public static class BlendShapeUtils 
    {
        public static BlendShapeDescription[] GetBlendShapedDescriptions(this Mesh mesh)
            => Enumerable.Range(0, mesh.blendShapeCount).Select(x => new BlendShapeDescription(mesh.GetBlendShapeName(x))).ToArray();


    }

}
