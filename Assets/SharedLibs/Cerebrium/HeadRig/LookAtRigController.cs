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
    [DefaultExecutionOrder(10000)]
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
        [Min(0f)]
        public float yawUndefinedPlanarEps = 0.05f;

        [Header("Blend (time-based)")]
        [Tooltip("Смысл как у per-frame lerp на 60fps, но реально считается от dt:\nalpha = 1 - (1 - lerpFactor01)^(dt*60).")]
        [PropertyRange(0f, 1f)]
        public float lerpFactor01 = 0.15f;

        [Header("Gizmo")]
        public bool drawGizmo = true;

        [Min(0f)]
        public float gizmoLength = 0.75f;

        [Header("Debug")]
        public bool logWhenOutOfRange = true;

        [Min(0f)]
        public float logCooldownSeconds = 0.5f;

        private Transform[] _boneTransforms = Array.Empty<Transform>();

        private float[] _tmpWeights = Array.Empty<float>();
        private float[] _tmpYawApplied = Array.Empty<float>();
        private float[] _tmpPitchApplied = Array.Empty<float>();

        private Quaternion[] _targetDeltaWorld = Array.Empty<Quaternion>();
        private Quaternion[] _currentDeltaWorld = Array.Empty<Quaternion>();

        private Vector3 _lastPivot;
        private Vector3 _lastLookDirWorld;
        private bool _hasLastLook;

        private bool _hasStableYaw;
        private float _stableYawDeg;

        // ===== External time (Timeline) =====
        private bool _externalTimeValid;
        private double _externalPrevTime;
        private float _externalDt;
        private bool _externalDtProvidedThisFrame;

        // ===== dt fallback (Editor) =====
#if UNITY_EDITOR
        private bool _editorTimeValid;
        private double _editorPrevTime;
#endif

#if UNITY_EDITOR
        private double _nextLogTimeEditor;
#else
        private float _nextLogTimePlay;
#endif

        private struct Caps
        {
            public float maxYawTotal;
            public float maxPitchUpTotal;
            public float maxPitchDownTotal;
        }

        private readonly struct PlanarPick
        {
            public readonly bool IsValid;
            public readonly Vector2 DirXZ;
            public readonly float Planar;

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

            _hasStableYaw = false;
            _stableYawDeg = 0f;
            _hasLastLook = false;

            for (int i = 0; i < _currentDeltaWorld.Length; i++)
            {
                _currentDeltaWorld[i] = Quaternion.identity;
                _targetDeltaWorld[i] = Quaternion.identity;
            }

            _externalTimeValid = false;
            _externalDtProvidedThisFrame = false;

#if UNITY_EDITOR
            _editorTimeValid = false;
            _nextLogTimeEditor = 0.0;
#else
            _nextLogTimePlay = 0f;
#endif
        }

        private void LateUpdate()
        {
            EnsureSetup();
            ApplyLook_TimeBased();
        }

        /// <summary>
        /// Вызывай из Timeline (в ProcessFrame) перед Apply.
        /// Тогда dt берётся строго из trackTime, и поведение в Play/Scrub становится максимально одинаковым.
        /// </summary>
        public void SetExternalTimeSeconds(double absoluteTimeSeconds)
        {
            if (!_externalTimeValid)
            {
                _externalTimeValid = true;
                _externalPrevTime = absoluteTimeSeconds;
                _externalDt = 0f;
            }
            else
            {
                double dt = absoluteTimeSeconds - _externalPrevTime;
                _externalPrevTime = absoluteTimeSeconds;

                if (dt < 0.0)
                {
                    // при перемотке назад не "догоняем" — просто считаем dt=0
                    dt = 0.0;
                }

                if (dt > 0.25) dt = 0.25; // разумный clamp от резких прыжков
                _externalDt = (float)dt;
            }

            _externalDtProvidedThisFrame = true;
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
                _tmpWeights.Length != count ||
                _tmpYawApplied.Length != count ||
                _tmpPitchApplied.Length != count ||
                _targetDeltaWorld.Length != count ||
                _currentDeltaWorld.Length != count;

            if (needResize)
            {
                _boneTransforms = new Transform[count];

                _tmpWeights = new float[count];
                _tmpYawApplied = new float[count];
                _tmpPitchApplied = new float[count];

                _targetDeltaWorld = new Quaternion[count];
                _currentDeltaWorld = new Quaternion[count];

                for (int i = 0; i < count; i++)
                {
                    _boneTransforms[i] = null;

                    _tmpWeights[i] = 0f;
                    _tmpYawApplied[i] = 0f;
                    _tmpPitchApplied[i] = 0f;

                    _targetDeltaWorld[i] = Quaternion.identity;
                    _currentDeltaWorld[i] = Quaternion.identity;
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
        }

        private void ApplyLook_TimeBased()
        {
            if (settings == null || settings.bones == null || settings.bones.Length == 0)
            {
                return;
            }

            if (animator == null || bodyTransform == null)
            {
                return;
            }

            int n = settings.bones.Length;

            bool hasTarget = lookTarget != null;
            BuildTargetDeltaWorld(hasTarget);

            float dt = ResolveDt();
            float alpha = ComputeAlpha(dt, lerpFactor01);

            // 1) лерпим ДЕЛЬТЫ во времени
            for (int i = 0; i < n; i++)
            {
                _currentDeltaWorld[i] = Quaternion.Slerp(_currentDeltaWorld[i], _targetDeltaWorld[i], alpha);

                float dot = Mathf.Abs(Quaternion.Dot(_currentDeltaWorld[i], _targetDeltaWorld[i]));
                if (dot > 0.9999995f)
                {
                    _currentDeltaWorld[i] = _targetDeltaWorld[i];
                }
            }

            // 2) применяем поверх позы анимации (Animator может менять кости каждый кадр)
            for (int i = 0; i < n; i++)
            {
                Transform bt = _boneTransforms[i];
                if (bt == null)
                {
                    continue;
                }

                Quaternion animWorld = bt.rotation;
                bt.rotation = _currentDeltaWorld[i] * animWorld;
            }
        }

        private float ResolveDt()
        {
            if (_externalDtProvidedThisFrame)
            {
                _externalDtProvidedThisFrame = false;
                return _externalDt;
            }

            if (Application.isPlaying)
            {
                float dt = Time.unscaledDeltaTime;
                if (dt < 0f) dt = 0f;
                if (dt > 0.25f) dt = 0.25f;
                return dt;
            }

#if UNITY_EDITOR
            double now = EditorApplication.timeSinceStartup;

            if (!_editorTimeValid)
            {
                _editorTimeValid = true;
                _editorPrevTime = now;
                return 0f;
            }

            double d = now - _editorPrevTime;
            _editorPrevTime = now;

            if (d < 0.0) d = 0.0;
            if (d > 0.25) d = 0.25;

            return (float)d;
#else
            return 0f;
#endif
        }

        private static float ComputeAlpha(float dt, float perFrameFactorAt60Fps)
        {
            float f = Mathf.Clamp01(perFrameFactorAt60Fps);

            if (f >= 0.999999f)
            {
                return 1f;
            }

            if (f <= 0.000001f || dt <= 0f)
            {
                return 0f;
            }

            // делаем эквивалент "пер-кадрового" лерпа, но во времени:
            // alpha = 1 - (1 - f)^(dt * 60)
            float a = 1f - Mathf.Pow(1f - f, dt * 60f);
            return Mathf.Clamp01(a);
        }

        private void BuildTargetDeltaWorld(bool targetActive)
        {
            _hasLastLook = false;

            int n = settings.bones.Length;

            for (int i = 0; i < n; i++)
            {
                _targetDeltaWorld[i] = Quaternion.identity;
                _tmpYawApplied[i] = 0f;
                _tmpPitchApplied[i] = 0f;
                _tmpWeights[i] = Mathf.Max(0f, settings.bones[i].weight);
            }

            if (!targetActive)
            {
                // нет цели -> хотим вернуться в анимацию (identity delta)
                return;
            }

            Transform head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head == null)
            {
                return;
            }

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

            Vector3 dirEyesLocalN = bodyTransform.InverseTransformDirection(dirEyesWN);

            float wSum = 0f;
            for (int i = 0; i < n; i++)
            {
                wSum += _tmpWeights[i];
            }

            if (wSum <= 1e-8f)
            {
                float inv = 1f / Mathf.Max(1, n);
                for (int i = 0; i < n; i++)
                {
                    _tmpWeights[i] = inv;
                }

                wSum = 1f;
            }

            float eps = Mathf.Max(1e-6f, yawUndefinedPlanarEps);
            PlanarPick planarPick = PickPlanarFromFallbackChain(eps, lookTarget.position);

            float yawDegRaw;
            float planarForPitch;

            if (planarPick.IsValid)
            {
                yawDegRaw = Mathf.Atan2(planarPick.DirXZ.x, planarPick.DirXZ.y) * Mathf.Rad2Deg;
                planarForPitch = planarPick.Planar;

                _hasStableYaw = true;
                _stableYawDeg = yawDegRaw;
            }
            else
            {
                yawDegRaw = _hasStableYaw ? _stableYawDeg : 0f;
                planarForPitch = eps;
            }

            // цель сверху -> pitch положительный (вверх)
            float pitchDegRaw = -Mathf.Atan2(dirEyesLocalN.y, Mathf.Max(1e-6f, planarForPitch)) * Mathf.Rad2Deg;

            Caps caps = ComputeCapsAll();

            float yawDeg = Mathf.Clamp(yawDegRaw, -caps.maxYawTotal, +caps.maxYawTotal);
            float pitchDeg = Mathf.Clamp(pitchDegRaw, -caps.maxPitchDownTotal, +caps.maxPitchUpTotal);

            if (_hasStableYaw)
            {
                _stableYawDeg = yawDeg;
            }

            if (logWhenOutOfRange)
            {
                bool outYaw = Mathf.Abs(yawDegRaw) > (caps.maxYawTotal + 1e-3f);
                bool outPitchUp = pitchDegRaw > (caps.maxPitchUpTotal + 1e-3f);
                bool outPitchDown = pitchDegRaw < -(caps.maxPitchDownTotal + 1e-3f);

                if (outYaw || outPitchUp || outPitchDown)
                {
                    if (CanLogNow())
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[LookAtRigController] Target out of range. yaw={yawDegRaw:F1}->{yawDeg:F1} (±{caps.maxYawTotal:F1}), pitch={pitchDegRaw:F1}->{pitchDeg:F1} (down {caps.maxPitchDownTotal:F1}, up {caps.maxPitchUpTotal:F1}).",
                            this);
                    }
                }
            }

            Vector3 bodyUpW = bodyTransform.up;

            // pitch axis после yaw
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

            for (int i = 0; i < n; i++)
            {
                float w = Mathf.Max(0f, settings.bones[i].weight);
                if (w <= 1e-6f)
                {
                    _targetDeltaWorld[i] = Quaternion.identity;
                    continue;
                }

                float y = _tmpYawApplied[i];
                float p = _tmpPitchApplied[i];

                if (Mathf.Abs(y) <= 1e-6f && Mathf.Abs(p) <= 1e-6f)
                {
                    _targetDeltaWorld[i] = Quaternion.identity;
                    continue;
                }

                Quaternion yawQ = Quaternion.AngleAxis(y, bodyUpW);
                Quaternion pitchQ = Quaternion.AngleAxis(p, rightAxis);

                _targetDeltaWorld[i] = pitchQ * yawQ;
            }
        }

        private Caps ComputeCapsAll()
        {
            Caps caps = default;

            for (int i = 0; i < settings.bones.Length; i++)
            {
                caps.maxYawTotal += Mathf.Max(0f, settings.bones[i].horizontalMaxDeg);

                Vector2 v = settings.bones[i].verticalMaxDeg;
                float vMin = Mathf.Min(v.x, v.y);
                float vMax = Mathf.Max(v.x, v.y);

                caps.maxPitchDownTotal += Mathf.Max(0f, -vMin);
                caps.maxPitchUpTotal += Mathf.Max(0f, vMax);
            }

            return caps;
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
                if (Mathf.Abs(rem) <= eps) return;

                bool positive = rem > 0f;

                float freeW = 0f;
                for (int i = 0; i < n; i++)
                {
                    float hMax = Mathf.Max(0f, settings.bones[i].horizontalMaxDeg);
                    float wi = Mathf.Max(0f, _tmpWeights[i]);
                    if (wi <= 1e-8f) continue;

                    if (positive)
                    {
                        if (_tmpYawApplied[i] < hMax - eps) freeW += wi;
                    }
                    else
                    {
                        if (_tmpYawApplied[i] > -hMax + eps) freeW += wi;
                    }
                }

                if (freeW <= 1e-8f) return;

                for (int i = 0; i < n; i++)
                {
                    float hMax = Mathf.Max(0f, settings.bones[i].horizontalMaxDeg);
                    float wi = Mathf.Max(0f, _tmpWeights[i]);
                    if (wi <= 1e-8f) continue;

                    if (positive)
                    {
                        float cap = hMax - _tmpYawApplied[i];
                        if (cap <= eps) continue;

                        float add = rem * (wi / freeW);
                        add = Mathf.Min(add, cap);
                        _tmpYawApplied[i] += add;
                    }
                    else
                    {
                        float cap = (-hMax) - _tmpYawApplied[i];
                        if (cap >= -eps) continue;

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
                if (Mathf.Abs(rem) <= eps) return;

                bool positive = rem > 0f;

                float freeW = 0f;
                for (int i = 0; i < n; i++)
                {
                    Vector2 v = settings.bones[i].verticalMaxDeg;
                    float vMin = Mathf.Min(v.x, v.y);
                    float vMax = Mathf.Max(v.x, v.y);

                    float wi = Mathf.Max(0f, _tmpWeights[i]);
                    if (wi <= 1e-8f) continue;

                    if (positive)
                    {
                        if (_tmpPitchApplied[i] < vMax - eps) freeW += wi;
                    }
                    else
                    {
                        if (_tmpPitchApplied[i] > vMin + eps) freeW += wi;
                    }
                }

                if (freeW <= 1e-8f) return;

                for (int i = 0; i < n; i++)
                {
                    Vector2 v = settings.bones[i].verticalMaxDeg;
                    float vMin = Mathf.Min(v.x, v.y);
                    float vMax = Mathf.Max(v.x, v.y);

                    float wi = Mathf.Max(0f, _tmpWeights[i]);
                    if (wi <= 1e-8f) continue;

                    if (positive)
                    {
                        float cap = vMax - _tmpPitchApplied[i];
                        if (cap <= eps) continue;

                        float add = rem * (wi / freeW);
                        add = Mathf.Min(add, cap);
                        _tmpPitchApplied[i] += add;
                    }
                    else
                    {
                        float cap = vMin - _tmpPitchApplied[i];
                        if (cap >= -eps) continue;

                        float add = rem * (wi / freeW);
                        add = Mathf.Max(add, cap);
                        _tmpPitchApplied[i] += add;
                    }
                }
            }
        }

        private PlanarPick PickPlanarFromFallbackChain(float eps, Vector3 targetWorldPos)
        {
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

        private bool CanLogNow()
        {
#if UNITY_EDITOR
            double now = EditorApplication.timeSinceStartup;
            if (now < _nextLogTimeEditor) return false;

            _nextLogTimeEditor = now + Math.Max(0.0, logCooldownSeconds);
            return true;
#else
            float t = Time.unscaledTime;
            if (t < _nextLogTimePlay) return false;

            _nextLogTimePlay = t + Mathf.Max(0f, logCooldownSeconds);
            return true;
#endif
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmo) return;
            if (!_hasLastLook) return;

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
    }
}
