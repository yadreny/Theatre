using System.Linq;
using UnityEngine;
using static AlSo.ShortCuts;

namespace AlSo
{
    public static class MoldGroupBaker  
    {
        private static SkinnedMeshRenderer _smr;
        private static SkinnedMeshRenderer Smr => CreateIfNotExist(ref _smr, GetSmr);

        private static SkinnedMeshRenderer GetSmr()
        {
            GameObject obj = new GameObject();
            obj.transform.rotation = Quaternion.Euler(-90, 0, 0);
            return obj.AddComponent<SkinnedMeshRenderer>();
        }


        //SkinnedMeshRenderer  smr = this.GetComponentInChildren<SkinnedMeshRenderer>();
        //Transform[] bones = smr.bones;
        //smr.sharedMesh = smr.sharedMesh.BakeBSAndResetValues(smr.bones, moldGroup, new BlendShapeGroup[] { BlendShapeGroup.Santiment });
        //smr.ResetBSValues();
        //smr.bones = bones;
        public static Mesh BakeBSAndResetValues(this Mesh sourceMesh, Transform[] bones, IMoldGroup moldGroup, BlendShapeGroup[] keepUnbaked)
        {
            Smr.sharedMesh = sourceMesh;
            Smr.enabled = true;
            Smr.bones = bones;

            foreach (IMold element in moldGroup.Elements.Where(x=>!keepUnbaked.Contains(x.Description.Group)))
            {
                element.ApplyTo(Smr);
            }

            Mesh result = new Mesh();
            Smr.BakeMesh(result);
            result.bindposes = sourceMesh.bindposes;
            result.boneWeights = sourceMesh.boneWeights;

            foreach (IMold element in moldGroup.Elements.Where(x=>keepUnbaked.Contains(x.Description.Group)))
            {
                element.Description.TransplantateFromTo(Smr.sharedMesh, result);
            }

            Smr.enabled = false;
            return result;
        }

        public static void ResetBSValues(this SkinnedMeshRenderer smr)
        {
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                smr.SetBlendShapeWeight(i, 0);
            }
        }
    }
}