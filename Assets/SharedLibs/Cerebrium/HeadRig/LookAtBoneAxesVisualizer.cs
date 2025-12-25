using System;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlSo
{
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    [AddComponentMenu("AlSo/Locomotion/Look At Bone Axes Visualizer")]
    public class LookAtBoneAxesVisualizer : MonoBehaviour
    {
        [Header("Rig")]
        public Animator animator;

        [Header("Settings")]
        public LookAtRigSettings settings;

        [Header("Reference")]
        [Tooltip("Если задан — его forward/up считаются базовыми. Если null — берём animator.transform.")]
        public Transform bodyTransform;

        [Header("Draw")]
        public bool drawInEditMode = true;
        public bool drawInPlayMode = true;

        public bool drawFromSettings = true;

        [Min(0.01f)]
        public float axisLength = 0.12f;

        [Min(0.0f)]
        public float axisHeadSize = 0.02f;

        public bool drawUnityAxes = true;

        public bool drawSettingsForwardAxis = true;
        public bool drawSettingsUpAxis = true;

        public bool drawSettingsParentForwardAxis = true;
        public bool drawSettingsParentUpAxis = true;

        public bool drawLabels = true;
        public bool drawBodyReferenceAxes = true;

        [Header("Manual list (when settings is null or drawFromSettings=false)")]
        public HumanBodyBones[] bonesToDraw = new[]
        {
            HumanBodyBones.Head,
            HumanBodyBones.Neck,
            HumanBodyBones.Chest,
            HumanBodyBones.Spine
        };

        private void OnEnable()
        {
            EnsureRig();
        }

        private void OnValidate()
        {
            EnsureRig();
        }

        private void EnsureRig()
        {
            if (animator == null)
            {
                animator = GetComponentInParent<Animator>();
            }

            if (bodyTransform == null && animator != null)
            {
                bodyTransform = animator.transform;
            }
        }

#if UNITY_EDITOR
        [Button(ButtonSizes.Medium)]
        private void AutoDetectAxes()
        {
            EnsureRig();

            if (settings == null)
            {
                UnityEngine.Debug.LogWarning("[LookAtBoneAxesVisualizer] AutoDetectAxes: settings is null.", this);
                return;
            }

            if (animator == null || !animator.isHuman)
            {
                UnityEngine.Debug.LogWarning("[LookAtBoneAxesVisualizer] AutoDetectAxes: Animator is null or not humanoid.", this);
                return;
            }

            if (settings.bones == null || settings.bones.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[LookAtBoneAxesVisualizer] AutoDetectAxes: settings.bones is empty.", this);
                return;
            }

            Transform refTr = bodyTransform != null ? bodyTransform : animator.transform;

            Vector3 bodyUp = refTr.up;

            Vector3 bodyFwd = Vector3.ProjectOnPlane(refTr.forward, bodyUp);
            if (bodyFwd.sqrMagnitude <= 1e-10f)
            {
                bodyFwd = Vector3.ProjectOnPlane(Vector3.forward, bodyUp);
            }
            bodyFwd.Normalize();

            Undo.RecordObject(settings, "Auto Detect LookAt Axes");

            for (int i = 0; i < settings.bones.Length; i++)
            {
                var b = settings.bones[i];

                Transform boneTr = animator.GetBoneTransform(b.bone);
                if (boneTr == null)
                {
                    continue;
                }

                b.forwardAxis = DetectBestForwardAxis(boneTr, bodyFwd);
                b.upAxis = DetectBestUpAxisGivenForward(boneTr, b.forwardAxis, bodyUp);

                if (boneTr.parent != null)
                {
                    b.parentForwardAxis = DetectBestForwardAxis(boneTr.parent, bodyFwd);
                    b.parentUpAxis = DetectBestUpAxisGivenForward(boneTr.parent, b.parentForwardAxis, bodyUp);
                }

                settings.bones[i] = b;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            UnityEngine.Debug.Log("[LookAtBoneAxesVisualizer] AutoDetectAxes done (forward aligned to body forward, up aligned to body up).", this);
        }

        private static LookAtForwardAxis DetectBestForwardAxis(Transform tr, Vector3 desiredForwardWorld)
        {
            LookAtForwardAxis best = LookAtForwardAxis.Z;
            float bestDot = float.NegativeInfinity;

            Try(tr, desiredForwardWorld, LookAtForwardAxis.X, ref best, ref bestDot);
            Try(tr, desiredForwardWorld, LookAtForwardAxis.Y, ref best, ref bestDot);
            Try(tr, desiredForwardWorld, LookAtForwardAxis.Z, ref best, ref bestDot);
            Try(tr, desiredForwardWorld, LookAtForwardAxis.MinusX, ref best, ref bestDot);
            Try(tr, desiredForwardWorld, LookAtForwardAxis.MinusY, ref best, ref bestDot);
            Try(tr, desiredForwardWorld, LookAtForwardAxis.MinusZ, ref best, ref bestDot);

            return best;
        }

        private static void Try(Transform tr, Vector3 desiredForwardWorld, LookAtForwardAxis axis, ref LookAtForwardAxis best, ref float bestDot)
        {
            Vector3 w = tr.TransformDirection(LookAtRigSettings.AxisToVector(axis));
            if (w.sqrMagnitude <= 1e-10f)
            {
                return;
            }

            w.Normalize();
            float d = Vector3.Dot(w, desiredForwardWorld);

            if (d > bestDot)
            {
                bestDot = d;
                best = axis;
            }
        }

        private static LookAtForwardAxis DetectBestUpAxisGivenForward(Transform tr, LookAtForwardAxis forwardAxis, Vector3 desiredUpWorld)
        {
            LookAtForwardAxis opposite = LookAtRigSettings.Opposite(forwardAxis);

            LookAtForwardAxis best = LookAtForwardAxis.Y;
            float bestDot = float.NegativeInfinity;

            TryUp(tr, desiredUpWorld, LookAtForwardAxis.X, forwardAxis, opposite, ref best, ref bestDot);
            TryUp(tr, desiredUpWorld, LookAtForwardAxis.Y, forwardAxis, opposite, ref best, ref bestDot);
            TryUp(tr, desiredUpWorld, LookAtForwardAxis.Z, forwardAxis, opposite, ref best, ref bestDot);
            TryUp(tr, desiredUpWorld, LookAtForwardAxis.MinusX, forwardAxis, opposite, ref best, ref bestDot);
            TryUp(tr, desiredUpWorld, LookAtForwardAxis.MinusY, forwardAxis, opposite, ref best, ref bestDot);
            TryUp(tr, desiredUpWorld, LookAtForwardAxis.MinusZ, forwardAxis, opposite, ref best, ref bestDot);

            if (bestDot == float.NegativeInfinity)
            {
                return LookAtForwardAxis.Y;
            }

            return best;
        }

        private static void TryUp(
            Transform tr,
            Vector3 desiredUpWorld,
            LookAtForwardAxis candidate,
            LookAtForwardAxis forwardAxis,
            LookAtForwardAxis oppositeForwardAxis,
            ref LookAtForwardAxis best,
            ref float bestDot)
        {
            if (candidate == forwardAxis || candidate == oppositeForwardAxis)
            {
                return;
            }

            Vector3 w = tr.TransformDirection(LookAtRigSettings.AxisToVector(candidate));
            if (w.sqrMagnitude <= 1e-10f)
            {
                return;
            }

            w.Normalize();
            float d = Vector3.Dot(w, desiredUpWorld);

            if (d > bestDot)
            {
                bestDot = d;
                best = candidate;
            }
        }
#endif

        private void OnDrawGizmos()
        {
            if (!ShouldDrawNow())
            {
                return;
            }

            EnsureRig();

            if (animator == null || !animator.isHuman)
            {
                return;
            }

            if (drawBodyReferenceAxes)
            {
                Transform refTr = bodyTransform != null ? bodyTransform : animator.transform;
                Vector3 p = refTr.position + refTr.up * 1.6f;

                float len = Mathf.Max(0.01f, axisLength) * 1.5f;

                Gizmos.color = Color.white;
                DrawArrow(p, refTr.forward, len, axisHeadSize);

#if UNITY_EDITOR
                if (drawLabels)
                {
                    Handles.color = Color.white;
                    Handles.Label(p + refTr.up * 0.03f, "Body Forward (white)");
                }
#endif
            }

            if (drawFromSettings && settings != null && settings.bones != null && settings.bones.Length > 0)
            {
                DrawFromSettings();
                return;
            }

            DrawFromManualList();
        }

        private bool ShouldDrawNow()
        {
            if (Application.isPlaying)
            {
                return drawInPlayMode;
            }

#if UNITY_EDITOR
            return drawInEditMode;
#else
            return false;
#endif
        }

        private void DrawFromSettings()
        {
            var arr = settings.bones;
            for (int i = 0; i < arr.Length; i++)
            {
                Transform t = animator.GetBoneTransform(arr[i].bone);
                if (t == null)
                {
                    continue;
                }

                DrawOneBone(t, arr[i].bone.ToString(), arr[i], true);
            }
        }

        private void DrawFromManualList()
        {
            if (bonesToDraw == null || bonesToDraw.Length == 0)
            {
                return;
            }

            for (int i = 0; i < bonesToDraw.Length; i++)
            {
                HumanBodyBones hb = bonesToDraw[i];
                Transform t = animator.GetBoneTransform(hb);
                if (t == null)
                {
                    continue;
                }

                DrawOneBone(t, hb.ToString(), default, false);
            }
        }

        private void DrawOneBone(Transform bone, string label, LookAtRigSettings.BoneLimit lim, bool hasSettingsAxes)
        {
            Vector3 p = bone.position;

            float len = Mathf.Max(0.01f, axisLength);
            float head = Mathf.Max(0.0f, axisHeadSize);

            if (drawUnityAxes)
            {
                Gizmos.color = Color.red;
                DrawArrow(p, bone.right, len, head);

                Gizmos.color = Color.green;
                DrawArrow(p, bone.up, len, head);

                Gizmos.color = Color.blue;
                DrawArrow(p, bone.forward, len, head);
            }

            if (hasSettingsAxes)
            {
                if (drawSettingsForwardAxis)
                {
                    Vector3 local = LookAtRigSettings.AxisToVector(lim.forwardAxis);
                    Vector3 w = bone.TransformDirection(local).normalized;
                    Gizmos.color = Color.yellow;
                    DrawArrow(p, w, len * 1.15f, head);
                }

                if (drawSettingsUpAxis)
                {
                    Vector3 local = LookAtRigSettings.AxisToVector(lim.upAxis);
                    Vector3 w = bone.TransformDirection(local).normalized;
                    Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // orange
                    DrawArrow(p, w, len * 1.05f, head);
                }

                if (bone.parent != null)
                {
                    if (drawSettingsParentForwardAxis)
                    {
                        Vector3 localP = LookAtRigSettings.AxisToVector(lim.parentForwardAxis);
                        Vector3 wP = bone.parent.TransformDirection(localP).normalized;
                        Gizmos.color = Color.magenta;
                        DrawArrow(p, wP, len * 0.95f, head);
                    }

                    if (drawSettingsParentUpAxis)
                    {
                        Vector3 localPU = LookAtRigSettings.AxisToVector(lim.parentUpAxis);
                        Vector3 wPU = bone.parent.TransformDirection(localPU).normalized;
                        Gizmos.color = Color.cyan;
                        DrawArrow(p, wPU, len * 0.9f, head);
                    }
                }
            }

#if UNITY_EDITOR
            if (drawLabels)
            {
                if (!hasSettingsAxes)
                {
                    Handles.color = Color.white;
                    Handles.Label(p + Vector3.up * (len * 0.2f), label);
                    return;
                }

                string txt = $"{label}  fwd:{lim.forwardAxis}  up:{lim.upAxis}";
                if (bone.parent != null)
                {
                    txt += $"  pfwd:{lim.parentForwardAxis}  pup:{lim.parentUpAxis}";
                }

                Handles.color = Color.white;
                Handles.Label(p + Vector3.up * (len * 0.2f), txt);
            }
#endif
        }

        private static void DrawArrow(Vector3 origin, Vector3 dir, float length, float headSize)
        {
            if (dir.sqrMagnitude <= 1e-10f)
            {
                return;
            }

            dir.Normalize();
            Vector3 end = origin + dir * length;

            Gizmos.DrawLine(origin, end);

            if (headSize <= 1e-6f)
            {
                return;
            }

            Vector3 up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(dir, up)) > 0.95f)
            {
                up = Vector3.right;
            }

            Vector3 side = Vector3.Cross(dir, up).normalized;
            Vector3 back = -dir;

            Vector3 a = end + (back + side) * headSize;
            Vector3 b = end + (back - side) * headSize;

            Gizmos.DrawLine(end, a);
            Gizmos.DrawLine(end, b);
        }
    }
}
