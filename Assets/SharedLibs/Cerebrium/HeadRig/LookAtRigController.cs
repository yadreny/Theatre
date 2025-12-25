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
    [AddComponentMenu("AlSo/Locomotion/Look At Rig Controller")]
    public class LookAtRigController : MonoBehaviour
    {
        [Header("Rig")]
        public Animator animator;
        public Transform bodyTransform;
        public LookAtRigSettings settings;

        [Header("Target")]
        public Transform lookTarget;

        [Header("Pivot")]
        [Min(0f)]
        public float headPivotUpOffset = 0.08f;

        [Header("Gizmo")]
        public bool drawGizmo = true;

        [Min(0f)]
        public float gizmoLength = 0.75f;

        [Header("Debug")]
        public bool logWhenOutOfRange = true;

        [Min(0f)]
        public float logCooldownSeconds = 0.5f;

        private Transform[] _boneTransforms = Array.Empty<Transform>();
        private Quaternion[] _restLocalRotations = Array.Empty<Quaternion>();
        private bool _restCaptured;

        private Vector3 _lastPivot;
        private Vector3 _lastLookDirWorld;
        private bool _hasLastLook;

#if UNITY_EDITOR
        private double _nextLogTimeEditor;
#endif
        private float _nextLogTimePlay;

        private void OnEnable()
        {
            EnsureSetup();
            CaptureRestPoseInternal();

#if UNITY_EDITOR
            _nextLogTimeEditor = 0.0;
#endif
            _nextLogTimePlay = 0f;
        }

        private void LateUpdate()
        {
            EnsureSetup();
            ApplyLook_NoAccumulation_NoDamp();
        }

        private void EnsureSetup()
        {
            if (animator == null)
            {
                animator = GetComponentInParent<Animator>();
            }

            if (bodyTransform == null)
            {
                bodyTransform = transform;
            }

            if (settings == null || settings.bones == null)
            {
                return;
            }

            int count = settings.bones.Length;

            if (_boneTransforms.Length != count)
            {
                _boneTransforms = new Transform[count];
                _restLocalRotations = new Quaternion[count];
                _restCaptured = false;

                for (int i = 0; i < count; i++)
                {
                    _boneTransforms[i] = null;
                    _restLocalRotations[i] = Quaternion.identity;
                }
            }

            if (animator == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                _boneTransforms[i] = animator.GetBoneTransform(settings.bones[i].bone);
            }

            if (!_restCaptured)
            {
                CaptureRestPoseInternal();
            }
        }

#if UNITY_EDITOR
        [Button(ButtonSizes.Medium)]
        private void CaptureRestPose()
        {
            EnsureSetup();
            CaptureRestPoseInternal();
            UnityEngine.Debug.Log("[LookAtRigController] Rest pose captured.", this);
        }
#endif

        private void CaptureRestPoseInternal()
        {
            if (settings == null || settings.bones == null || settings.bones.Length == 0)
            {
                _restCaptured = false;
                return;
            }

            int count = settings.bones.Length;

            if (_boneTransforms == null || _boneTransforms.Length != count)
            {
                _restCaptured = false;
                return;
            }

            if (_restLocalRotations == null || _restLocalRotations.Length != count)
            {
                _restLocalRotations = new Quaternion[count];
            }

            for (int i = 0; i < count; i++)
            {
                Transform t = _boneTransforms[i];
                _restLocalRotations[i] = t != null ? t.localRotation : Quaternion.identity;
            }

            _restCaptured = true;
        }

        private void ApplyLook_NoAccumulation_NoDamp()
        {
            _hasLastLook = false;

            if (settings == null || settings.bones == null || settings.bones.Length == 0)
            {
                return;
            }

            if (animator == null || bodyTransform == null)
            {
                return;
            }

            if (lookTarget == null)
            {
                // Ничего не делаем, не пытаемся "откатывать" — иначе будем бороться с позой аниматора.
                return;
            }

            Transform head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;

            Vector3 pivot = head != null ? head.position : transform.position;
            Vector3 pivotUp = head != null ? head.up : transform.up;

            float pivotOffset = Mathf.Max(0f, headPivotUpOffset);
            if (pivotOffset > 1e-6f)
            {
                pivot += pivotUp * pivotOffset;
            }

            Vector3 dirWorld = lookTarget.position - pivot;
            if (dirWorld.sqrMagnitude <= 1e-10f)
            {
                return;
            }

            Vector3 dirWorldN = dirWorld.normalized;

            _lastPivot = pivot;
            _lastLookDirWorld = dirWorldN;
            _hasLastLook = true;

            // === углы относительно корпуса ===
            Vector3 bodyUpW = bodyTransform.up;

            Vector3 bodyForwardPlanar = Vector3.ProjectOnPlane(bodyTransform.forward, bodyUpW);
            if (bodyForwardPlanar.sqrMagnitude <= 1e-10f)
            {
                bodyForwardPlanar = Vector3.ProjectOnPlane(Vector3.forward, bodyUpW);
            }
            bodyForwardPlanar.Normalize();

            Vector3 targetForwardPlanar = Vector3.ProjectOnPlane(dirWorldN, bodyUpW);
            if (targetForwardPlanar.sqrMagnitude <= 1e-10f)
            {
                targetForwardPlanar = bodyForwardPlanar;
            }
            else
            {
                targetForwardPlanar.Normalize();
            }

            float yawDeg = Vector3.SignedAngle(bodyForwardPlanar, targetForwardPlanar, bodyUpW);

            Vector3 rightAxis = Vector3.Cross(bodyUpW, targetForwardPlanar);
            if (rightAxis.sqrMagnitude <= 1e-10f)
            {
                rightAxis = bodyTransform.right;
            }
            else
            {
                rightAxis.Normalize();
            }

            float pitchDeg = Vector3.SignedAngle(targetForwardPlanar, dirWorldN, rightAxis);
            // pitchDeg: + вверх, - вниз

            if (logWhenOutOfRange)
            {
                float maxYawTotal = 0f;
                float maxPitchUpTotal = 0f;
                float maxPitchDownTotal = 0f;

                for (int i = 0; i < settings.bones.Length; i++)
                {
                    maxYawTotal += Mathf.Max(0f, settings.bones[i].horizontalMaxDeg);

                    Vector2 v = settings.bones[i].verticalMaxDeg;
                    float vMin = Mathf.Min(v.x, v.y);
                    float vMax = Mathf.Max(v.x, v.y);

                    maxPitchDownTotal += Mathf.Max(0f, -vMin);
                    maxPitchUpTotal += Mathf.Max(0f, vMax);
                }

                bool outYaw = Mathf.Abs(yawDeg) > (maxYawTotal + 1e-3f);
                bool outPitchUp = pitchDeg > (maxPitchUpTotal + 1e-3f);
                bool outPitchDown = pitchDeg < -(maxPitchDownTotal + 1e-3f);

                if (outYaw || outPitchUp || outPitchDown)
                {
                    if (CanLogNow())
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[LookAtRigController] Target out of range. yaw={yawDeg:F1} (max±{maxYawTotal:F1}), pitch={pitchDeg:F1} (min -{maxPitchDownTotal:F1}, max +{maxPitchUpTotal:F1}).",
                            this);
                    }
                }
            }

            // === распределяем одновременно по весам, но с лимитами ===
            int n = settings.bones.Length;

            float[] weights = new float[n];
            float wSum = 0f;

            for (int i = 0; i < n; i++)
            {
                float w = Mathf.Max(0f, settings.bones[i].weight);
                weights[i] = w;
                wSum += w;
            }

            if (wSum <= 1e-8f)
            {
                // fallback: равномерно
                for (int i = 0; i < n; i++)
                {
                    weights[i] = 1f;
                }

                wSum = Mathf.Max(1, n);
            }

            float[] yawApplied = new float[n];
            float[] pitchApplied = new float[n];

            // initial shares
            for (int i = 0; i < n; i++)
            {
                float s = weights[i] / wSum;
                yawApplied[i] = yawDeg * s;
                pitchApplied[i] = pitchDeg * s;
            }

            // clamp
            for (int i = 0; i < n; i++)
            {
                float hMax = Mathf.Max(0f, settings.bones[i].horizontalMaxDeg);
                yawApplied[i] = Mathf.Clamp(yawApplied[i], -hMax, +hMax);

                Vector2 v = settings.bones[i].verticalMaxDeg;
                float vMin = Mathf.Min(v.x, v.y);
                float vMax = Mathf.Max(v.x, v.y);
                pitchApplied[i] = Mathf.Clamp(pitchApplied[i], vMin, vMax);
            }

            // redistribute leftovers iteratively (yaw and pitch separately)
            RedistributeYaw(ref yawApplied, yawDeg, weights);
            RedistributePitch(ref pitchApplied, pitchDeg, weights);

            // === применяем без накопления: baseLocal + localDelta ===
            bool useRest = !Application.isPlaying;
            if (useRest && !_restCaptured)
            {
                CaptureRestPoseInternal();
            }

            for (int i = 0; i < n; i++)
            {
                Transform boneTr = _boneTransforms != null && i < _boneTransforms.Length ? _boneTransforms[i] : null;
                if (boneTr == null)
                {
                    continue;
                }

                float w = Mathf.Max(0f, settings.bones[i].weight);

                if (useRest && w <= 1e-6f)
                {
                    // В edit-mode вес 0 -> держим rest, чтобы не "залипало"
                    boneTr.localRotation = _restLocalRotations[i];
                    continue;
                }

                float y = yawApplied[i];
                float p = pitchApplied[i];

                if (Mathf.Abs(y) <= 1e-6f && Mathf.Abs(p) <= 1e-6f)
                {
                    if (useRest)
                    {
                        // если edit-mode и угол 0 — явно ставим rest
                        boneTr.localRotation = _restLocalRotations[i];
                    }

                    continue;
                }

                Quaternion yawQ = Quaternion.AngleAxis(y, bodyUpW);
                Quaternion pitchQ = Quaternion.AngleAxis(p, rightAxis);

                Quaternion deltaWorld = pitchQ * yawQ;

                Quaternion parentRot = boneTr.parent != null ? boneTr.parent.rotation : Quaternion.identity;
                Quaternion localDelta = Quaternion.Inverse(parentRot) * deltaWorld * parentRot;

                Quaternion baseLocal = useRest ? _restLocalRotations[i] : boneTr.localRotation;
                boneTr.localRotation = localDelta * baseLocal;
            }
        }

        private void RedistributeYaw(ref float[] yawApplied, float yawTarget, float[] weights)
        {
            const int iters = 8;
            const float eps = 1e-4f;

            int n = yawApplied.Length;

            for (int iter = 0; iter < iters; iter++)
            {
                float sum = 0f;
                for (int i = 0; i < n; i++) sum += yawApplied[i];

                float rem = yawTarget - sum;
                if (Mathf.Abs(rem) <= eps)
                {
                    return;
                }

                bool positive = rem > 0f;

                float freeW = 0f;
                for (int i = 0; i < n; i++)
                {
                    float hMax = Mathf.Max(0f, settings.bones[i].horizontalMaxDeg);

                    if (positive)
                    {
                        if (yawApplied[i] < hMax - eps)
                            freeW += Mathf.Max(0f, weights[i]);
                    }
                    else
                    {
                        if (yawApplied[i] > -hMax + eps)
                            freeW += Mathf.Max(0f, weights[i]);
                    }
                }

                if (freeW <= 1e-8f)
                {
                    return;
                }

                for (int i = 0; i < n; i++)
                {
                    float hMax = Mathf.Max(0f, settings.bones[i].horizontalMaxDeg);
                    float wi = Mathf.Max(0f, weights[i]);

                    if (wi <= 1e-8f)
                        continue;

                    if (positive)
                    {
                        float cap = hMax - yawApplied[i];
                        if (cap <= eps)
                            continue;

                        float add = rem * (wi / freeW);
                        add = Mathf.Min(add, cap);
                        yawApplied[i] += add;
                    }
                    else
                    {
                        float cap = (-hMax) - yawApplied[i]; // negative number
                        if (cap >= -eps)
                            continue;

                        float add = rem * (wi / freeW); // rem negative
                        add = Mathf.Max(add, cap);
                        yawApplied[i] += add;
                    }
                }
            }
        }

        private void RedistributePitch(ref float[] pitchApplied, float pitchTarget, float[] weights)
        {
            const int iters = 8;
            const float eps = 1e-4f;

            int n = pitchApplied.Length;

            for (int iter = 0; iter < iters; iter++)
            {
                float sum = 0f;
                for (int i = 0; i < n; i++) sum += pitchApplied[i];

                float rem = pitchTarget - sum;
                if (Mathf.Abs(rem) <= eps)
                {
                    return;
                }

                bool positive = rem > 0f;

                float freeW = 0f;
                for (int i = 0; i < n; i++)
                {
                    Vector2 v = settings.bones[i].verticalMaxDeg;
                    float vMin = Mathf.Min(v.x, v.y);
                    float vMax = Mathf.Max(v.x, v.y);

                    if (positive)
                    {
                        if (pitchApplied[i] < vMax - eps)
                            freeW += Mathf.Max(0f, weights[i]);
                    }
                    else
                    {
                        if (pitchApplied[i] > vMin + eps)
                            freeW += Mathf.Max(0f, weights[i]);
                    }
                }

                if (freeW <= 1e-8f)
                {
                    return;
                }

                for (int i = 0; i < n; i++)
                {
                    Vector2 v = settings.bones[i].verticalMaxDeg;
                    float vMin = Mathf.Min(v.x, v.y);
                    float vMax = Mathf.Max(v.x, v.y);

                    float wi = Mathf.Max(0f, weights[i]);
                    if (wi <= 1e-8f)
                        continue;

                    if (positive)
                    {
                        float cap = vMax - pitchApplied[i];
                        if (cap <= eps)
                            continue;

                        float add = rem * (wi / freeW);
                        add = Mathf.Min(add, cap);
                        pitchApplied[i] += add;
                    }
                    else
                    {
                        float cap = vMin - pitchApplied[i]; // rem negative => cap negative/zero
                        if (cap >= -eps)
                            continue;

                        float add = rem * (wi / freeW);
                        add = Mathf.Max(add, cap);
                        pitchApplied[i] += add;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmo)
            {
                return;
            }

            if (!_hasLastLook)
            {
                return;
            }

            Vector3 origin = _lastPivot;
            Vector3 dir = _lastLookDirWorld;

            float len = Mathf.Max(0.01f, gizmoLength);
            Vector3 end = origin + dir * len;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawSphere(origin, 0.01f);
            Gizmos.DrawSphere(end, 0.015f);

            if (lookTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(origin, lookTarget.position);
                Gizmos.DrawWireSphere(lookTarget.position, 0.025f);
            }
        }

        private bool CanLogNow()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                double now = EditorApplication.timeSinceStartup;
                if (now < _nextLogTimeEditor)
                {
                    return false;
                }

                _nextLogTimeEditor = now + Math.Max(0.0, logCooldownSeconds);
                return true;
#else
                return false;
#endif
            }

            float t = Time.unscaledTime;
            if (t < _nextLogTimePlay)
            {
                return false;
            }

            _nextLogTimePlay = t + Mathf.Max(0f, logCooldownSeconds);
            return true;
        }
    }
}
