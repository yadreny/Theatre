using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AlSo
{
    public enum LookAtForwardAxis
    {
        X,
        Y,
        Z,
        MinusX,
        MinusY,
        MinusZ
    }

    [CreateAssetMenu(menuName = "AlSo/Locomotion/Look At Rig Settings", fileName = "LookAtRigSettings")]
    public class LookAtRigSettings : ScriptableObject
    {
        [Serializable]
        public struct BoneLimit
        {
            [TableColumnWidth(150, Resizable = true)]
            public HumanBodyBones bone;

            [TableColumnWidth(110, Resizable = true)]
            [HideLabel]
            public LookAtForwardAxis forwardAxis;

            [TableColumnWidth(95, Resizable = true)]
            [HideLabel]
            public LookAtForwardAxis upAxis;

            [TableColumnWidth(120, Resizable = true)]
            [HideLabel]
            public LookAtForwardAxis parentForwardAxis;

            [TableColumnWidth(95, Resizable = true)]
            [HideLabel]
            public LookAtForwardAxis parentUpAxis;

            [TableColumnWidth(110, Resizable = true)]
            [HideLabel]
            [PropertyRange(0f, 90f)]
            public float horizontalMaxDeg;

            [TableColumnWidth(200, Resizable = true)]
            [HideLabel]
            [MinMaxSlider(-90f, 90f, true)]
            public Vector2 verticalMaxDeg;

            [TableColumnWidth(130, Resizable = true)]
            [HideLabel]
            [PropertyRange(0f, 1f)]
            public float weight;

            public void Sanitize()
            {
                if (horizontalMaxDeg < 0f) horizontalMaxDeg = 0f;
                if (horizontalMaxDeg > 90f) horizontalMaxDeg = 90f;

                if (verticalMaxDeg.x > verticalMaxDeg.y)
                {
                    float t = verticalMaxDeg.x;
                    verticalMaxDeg.x = verticalMaxDeg.y;
                    verticalMaxDeg.y = t;
                }

                verticalMaxDeg.x = Mathf.Clamp(verticalMaxDeg.x, -90f, 90f);
                verticalMaxDeg.y = Mathf.Clamp(verticalMaxDeg.y, -90f, 90f);

                if (weight < 0f) weight = 0f;
                if (weight > 1f) weight = 1f;
            }

            public float ComputeRawWeight(float verticalFactor)
            {
                float vRange = Mathf.Max(0f, verticalMaxDeg.y - verticalMaxDeg.x);
                float h = Mathf.Max(0f, horizontalMaxDeg);
                return h + vRange * Mathf.Max(0f, verticalFactor);
            }
        }

        [Min(0f)]
        public float verticalFactor = 1f;

        [TableList(AlwaysExpanded = true, DrawScrollView = true)]
        public BoneLimit[] bones = Array.Empty<BoneLimit>();

        [HorizontalGroup("Buttons")]
        [Button]
        private void RecalculateWeightsFromAngles()
        {
            if (bones == null || bones.Length == 0)
            {
                return;
            }

            float sum = 0f;

            for (int i = 0; i < bones.Length; i++)
            {
                var b = bones[i];
                b.Sanitize();

                float raw = b.ComputeRawWeight(verticalFactor);
                b.weight = raw;

                bones[i] = b;
                sum += raw;
            }

            NormalizeWeightsInternal(sum);
        }

        [HorizontalGroup("Buttons")]
        [Button]
        private void NormalizeWeights()
        {
            if (bones == null || bones.Length == 0)
            {
                return;
            }

            float sum = 0f;

            for (int i = 0; i < bones.Length; i++)
            {
                var b = bones[i];
                b.Sanitize();
                bones[i] = b;
                sum += Mathf.Max(0f, b.weight);
            }

            NormalizeWeightsInternal(sum);
        }

        private void NormalizeWeightsInternal(float sum)
        {
            if (bones == null || bones.Length == 0)
            {
                return;
            }

            if (sum <= 1e-8f)
            {
                float inv = 1f / Mathf.Max(1, bones.Length);
                for (int i = 0; i < bones.Length; i++)
                {
                    var b = bones[i];
                    b.weight = inv;
                    bones[i] = b;
                }

                return;
            }

            float invSum = 1f / sum;

            for (int i = 0; i < bones.Length; i++)
            {
                var b = bones[i];
                b.weight = Mathf.Clamp01(Mathf.Max(0f, b.weight) * invSum);
                bones[i] = b;
            }
        }

        public static Vector3 AxisToVector(LookAtForwardAxis axis)
        {
            switch (axis)
            {
                case LookAtForwardAxis.X: return Vector3.right;
                case LookAtForwardAxis.Y: return Vector3.up;
                case LookAtForwardAxis.Z: return Vector3.forward;
                case LookAtForwardAxis.MinusX: return Vector3.left;
                case LookAtForwardAxis.MinusY: return Vector3.down;
                case LookAtForwardAxis.MinusZ: return Vector3.back;
                default: return Vector3.forward;
            }
        }

        public static LookAtForwardAxis Opposite(LookAtForwardAxis axis)
        {
            switch (axis)
            {
                case LookAtForwardAxis.X: return LookAtForwardAxis.MinusX;
                case LookAtForwardAxis.Y: return LookAtForwardAxis.MinusY;
                case LookAtForwardAxis.Z: return LookAtForwardAxis.MinusZ;
                case LookAtForwardAxis.MinusX: return LookAtForwardAxis.X;
                case LookAtForwardAxis.MinusY: return LookAtForwardAxis.Y;
                case LookAtForwardAxis.MinusZ: return LookAtForwardAxis.Z;
                default: return LookAtForwardAxis.MinusZ;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (bones == null)
            {
                bones = Array.Empty<BoneLimit>();
                return;
            }

            for (int i = 0; i < bones.Length; i++)
            {
                var b = bones[i];
                b.Sanitize();
                bones[i] = b;
            }

            if (verticalFactor < 0f)
            {
                verticalFactor = 0f;
            }
        }
#endif
    }
}
