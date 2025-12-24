using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    [ExecuteInEditMode]
    public class BoneVisualizer : MonoBehaviour
    {
        public bool show = true;
        private Transform[] bones;
        private float boneRadius = 0.01f;
        private Color boneColor = Color.white;

        void OnDrawGizmos()
        {
            if (!show) return;

            bones = transform.gameObject.GetAllChildren().Select(x => x.transform).Where(x => x.parent != null).ToArray();

            Gizmos.color = boneColor;

            foreach (Transform bone in bones)
            {
                Gizmos.DrawWireSphere(bone.position, boneRadius);
                Gizmos.DrawLine(bone.position, bone.parent.position);
            }
        }
    }
}
