using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public static class MeshTools
    {
        public static Mesh Clone(this Mesh mesh)
        {
            Mesh newmesh = new Mesh();
            newmesh.vertices = mesh.vertices.Clone() as Vector3[];
            newmesh.triangles = mesh.triangles.Clone() as int[];
            newmesh.uv = mesh.uv.Clone() as Vector2[];
            newmesh.normals = mesh.normals.Clone() as Vector3[];
            newmesh.colors = mesh.colors.Clone() as Color[];
            newmesh.tangents = mesh.tangents.Clone() as Vector4[];
            newmesh.bindposes = mesh.bindposes.Clone() as Matrix4x4[];
            newmesh.boneWeights = mesh.boneWeights.Clone() as BoneWeight[];
            return newmesh;
        }
    }
}
