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

        [Header("Pivot (eyes)")]
        [Min(0f)]
        public float headPivotUpOffset = 0.08f;

        [Header("Close target stabilization")]
        [Tooltip("Если planar (XZ) слишком маленький, yaw становится неопределённым. В этом случае берём XZ от более 'крупной' кости (Head->Neck->Chest...)")]
        [Min(0f)]
        public float yawUndefinedPlanarEps = 0.05f;

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

        private float[] _tmpWeights = Array.Empty<float>();
        private float[] _tmpYawApplied = Array.Empty<float>();
        private float[] _tmpPitchApplied = Array.Empty<float>();

        private Vector3 _lastPivot;
        private Vector3 _lastLookDirWorld;
        private bool _hasLastLook;

        // стабильный yaw, когда yaw неопределён (цель почти строго вверх/вниз относительно выбранной XZ-точки)
        private bool _hasStableYaw;
        private float _stableYawDeg;

#if UNITY_EDITOR
        private double _nextLogTimeEditor;
#endif
        private float _nextLogTimePlay;

        private readonly struct PlanarPick
        {
            public readonly bool IsValid;
            public readonly Vector2 DirXZ;   // нормализованный (x,z)
            public readonly float Planar;    // длина XZ у НОРМАЛИЗОВАННОГО направления (0..1)

            public PlanarPick(bool isValid, Vector2 dirXZ, float planar)
            {
                IsValid = isValid;
                DirXZ = dirXZ;
                Planar = planar;
            }
        }

        private void OnEnable()
        {
            EnsureSetup();
            CaptureRestPoseInternal();

            _hasStableYaw = false;
            _stableYawDeg = 0f;

#if UNITY_EDITOR
            _nextLogTimeEditor = 0.0;
#endif
            _nextLogTimePlay = 0f;
        }

        private void LateUpdate()
        {
            EnsureSetup();
            ApplyLook();
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

            bool needResize =
                _boneTransforms.Length != count ||
                _restLocalRotations.Length != count ||
                _tmpWeights.Length != count ||
                _tmpYawApplied.Length != count ||
                _tmpPitchApplied.Length != count;

            if (needResize)
            {
                _boneTransforms = new Transform[count];
                _restLocalRotations = new Quaternion[count];

                _tmpWeights = new float[count];
                _tmpYawApplied = new float[count];
                _tmpPitchApplied = new float[count];

                _restCaptured = false;

                for (int i = 0; i < count; i++)
                {
                    _boneTransforms[i] = null;
                    _restLocalRotations[i] = Quaternion.identity;
                    _tmpWeights[i] = 0f;
                    _tmpYawApplied[i] = 0f;
                    _tmpPitchApplied[i] = 0f;
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

        private void ApplyLook()
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
                return;
            }

            Transform head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head == null)
            {
                return;
            }

            // ==== 1) PIVOT "ГЛАЗА" ====
            Vector3 pivotEyes = head.position;
            float pivotOffset = Mathf.Max(0f, headPivotUpOffset);
            if (pivotOffset > 1e-6f)
            {
                pivotEyes += head.up * pivotOffset;
            }

            Vector3 dirEyesW = lookTarget.position - pivotEyes;
            if (dirEyesW.sqrMagnitude <= 1e-10f)
            {
                return;
            }

            Vector3 dirEyesWN = dirEyesW.normalized;

            _lastPivot = pivotEyes;
            _lastLookDirWorld = dirEyesWN;
            _hasLastLook = true;

            // В локале корпуса (body): это главный “вертикальный” компонент
            Vector3 dirEyesLocalN = bodyTransform.InverseTransformDirection(dirEyesWN);

            // ==== 2) XZ берём не обязательно от глаз: Head->Neck->Chest->Spine... ====
            float eps = Mathf.Max(1e-6f, yawUndefinedPlanarEps);

            PlanarPick planarPick = PickPlanarFromFallbackChain(eps, lookTarget.position);

            float yawDeg;
            float planarForPitch;

            if (planarPick.IsValid)
            {
                yawDeg = Mathf.Atan2(planarPick.DirXZ.x, planarPick.DirXZ.y) * Mathf.Rad2Deg;
                planarForPitch = planarPick.Planar;

                _stableYawDeg = yawDeg;
                _hasStableYaw = true;
            }
            else
            {
                yawDeg = _hasStableYaw ? _stableYawDeg : 0f;
                planarForPitch = eps;
            }

            // ==== 3) pitch: Y от глаз, XZ от более стабильной точки ====
            float yEyes = dirEyesLocalN.y;
            float pitchDeg = -Mathf.Atan2(yEyes, Mathf.Max(1e-6f, planarForPitch)) * Mathf.Rad2Deg;

            // ==== 4) ось pitch — стабильная (body.right, повернутый yaw’ом) ====
            Vector3 bodyUpW = bodyTransform.up;
            Quaternion yawRot = Quaternion.AngleAxis(yawDeg, bodyUpW);
            Vector3 rightAxis = yawRot * bodyTransform.right;
            if (rightAxis.sqrMagnitude <= 1e-10f)
            {
                rightAxis = bodyTransform.right;
            }
            else
            {
                rightAxis.Normalize();
            }

            // ==== debug out-of-range ====
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

            // ==== веса ====
            int n = settings.bones.Length;

            float wSum = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = Mathf.Max(0f, settings.bones[i].weight);
                _tmpWeights[i] = w;
                wSum += w;
            }

            if (wSum <= 1e-8f)
            {
                for (int i = 0; i < n; i++)
                {
                    _tmpWeights[i] = 1f;
                }

                wSum = Mathf.Max(1, n);
            }

            for (int i = 0; i < n; i++)
            {
                float s = _tmpWeights[i] / wSum;
                _tmpYawApplied[i] = yawDeg * s;
                _tmpPitchApplied[i] = pitchDeg * s;
            }

            for (int i = 0; i < n; i++)
            {
                float hMax = Mathf.Max(0f, settings.bones[i].horizontalMaxDeg);
                _tmpYawApplied[i] = Mathf.Clamp(_tmpYawApplied[i], -hMax, +hMax);

                Vector2 v = settings.bones[i].verticalMaxDeg;
                float vMin = Mathf.Min(v.x, v.y);
                float vMax = Mathf.Max(v.x, v.y);
                _tmpPitchApplied[i] = Mathf.Clamp(_tmpPitchApplied[i], vMin, vMax);
            }

            RedistributeYaw(yawDeg);
            RedistributePitch(pitchDeg);

            // ==== применяем без накопления ====
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
                    boneTr.localRotation = _restLocalRotations[i];
                    continue;
                }

                float y = _tmpYawApplied[i];
                float p = _tmpPitchApplied[i];

                if (Mathf.Abs(y) <= 1e-6f && Mathf.Abs(p) <= 1e-6f)
                {
                    if (useRest)
                    {
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

        private PlanarPick PickPlanarFromFallbackChain(float eps, Vector3 targetWorldPos)
        {
            // Важно: тут мы намеренно берём XZ от "более стабильных" точек.
            // Порядок можно менять под риг.
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            Transform upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            PlanarPick r;

            r = SamplePlanarAtPivot(head, eps, targetWorldPos);
            if (r.IsValid) return r;

            r = SamplePlanarAtPivot(neck, eps, targetWorldPos);
            if (r.IsValid) return r;

            r = SamplePlanarAtPivot(upperChest, eps, targetWorldPos);
            if (r.IsValid) return r;

            r = SamplePlanarAtPivot(chest, eps, targetWorldPos);
            if (r.IsValid) return r;

            r = SamplePlanarAtPivot(spine, eps, targetWorldPos);
            if (r.IsValid) return r;

            r = SamplePlanarAtPivot(hips, eps, targetWorldPos);
            if (r.IsValid) return r;

            return new PlanarPick(false, default, 0f);
        }

        private PlanarPick SamplePlanarAtPivot(Transform pivot, float eps, Vector3 targetWorldPos)
        {
            if (pivot == null)
            {
                return new PlanarPick(false, default, 0f);
            }

            Vector3 dirW = targetWorldPos - pivot.position;
            if (dirW.sqrMagnitude <= 1e-10f)
            {
                return new PlanarPick(false, default, 0f);
            }

            Vector3 dirWN = dirW.normalized;
            Vector3 dirLocalN = bodyTransform.InverseTransformDirection(dirWN);

            Vector2 xz = new Vector2(dirLocalN.x, dirLocalN.z);
            float planar = xz.magnitude;

            if (planar < eps)
            {
                return new PlanarPick(false, default, 0f);
            }

            xz /= planar;
            return new PlanarPick(true, xz, planar);
        }

        private void RedistributeYaw(float yawTarget)
        {
            const int iters = 8;
            const float eps = 1e-4f;

            int n = _tmpYawApplied.Length;

            for (int iter = 0; iter < iters; iter++)
            {
                float sum = 0f;
                for (int i = 0; i < n; i++) sum += _tmpYawApplied[i];

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
                        if (_tmpYawApplied[i] < hMax - eps)
                            freeW += Mathf.Max(0f, _tmpWeights[i]);
                    }
                    else
                    {
                        if (_tmpYawApplied[i] > -hMax + eps)
                            freeW += Mathf.Max(0f, _tmpWeights[i]);
                    }
                }

                if (freeW <= 1e-8f)
                {
                    return;
                }

                for (int i = 0; i < n; i++)
                {
                    float hMax = Mathf.Max(0f, settings.bones[i].horizontalMaxDeg);
                    float wi = Mathf.Max(0f, _tmpWeights[i]);

                    if (wi <= 1e-8f)
                        continue;

                    if (positive)
                    {
                        float cap = hMax - _tmpYawApplied[i];
                        if (cap <= eps)
                            continue;

                        float add = rem * (wi / freeW);
                        add = Mathf.Min(add, cap);
                        _tmpYawApplied[i] += add;
                    }
                    else
                    {
                        float cap = (-hMax) - _tmpYawApplied[i];
                        if (cap >= -eps)
                            continue;

                        float add = rem * (wi / freeW);
                        add = Mathf.Max(add, cap);
                        _tmpYawApplied[i] += add;
                    }
                }
            }
        }

        private void RedistributePitch(float pitchTarget)
        {
            const int iters = 8;
            const float eps = 1e-4f;

            int n = _tmpPitchApplied.Length;

            for (int iter = 0; iter < iters; iter++)
            {
                float sum = 0f;
                for (int i = 0; i < n; i++) sum += _tmpPitchApplied[i];

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
                        if (_tmpPitchApplied[i] < vMax - eps)
                            freeW += Mathf.Max(0f, _tmpWeights[i]);
                    }
                    else
                    {
                        if (_tmpPitchApplied[i] > vMin + eps)
                            freeW += Mathf.Max(0f, _tmpWeights[i]);
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

                    float wi = Mathf.Max(0f, _tmpWeights[i]);
                    if (wi <= 1e-8f)
                        continue;

                    if (positive)
                    {
                        float cap = vMax - _tmpPitchApplied[i];
                        if (cap <= eps)
                            continue;

                        float add = rem * (wi / freeW);
                        add = Mathf.Min(add, cap);
                        _tmpPitchApplied[i] += add;
                    }
                    else
                    {
                        float cap = vMin - _tmpPitchApplied[i];
                        if (cap >= -eps)
                            continue;

                        float add = rem * (wi / freeW);
                        add = Mathf.Max(add, cap);
                        _tmpPitchApplied[i] += add;
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
