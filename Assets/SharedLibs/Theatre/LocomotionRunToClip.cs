using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    [Serializable]
    public class LocomotionRunToClip : PlayableAsset, ITimelineClipAsset
    {
        public ExposedReference<Transform> from;
        public ExposedReference<Transform> to;

        [Header("Drive transform")]
        public bool drivePosition = true;
        public bool driveRotation = false;

        [Header("Speed")]
        [Tooltip("Номинальная скорость (м/с), при которой speed=1. Эта величина используется для нормализации.")]
        public float fullSpeedMetersPerSecond = 2.5f;

        [Tooltip("Множитель на итоговый speed (после нормализации).")]
        public float speedMultiplier = 1.0f;

        [Header("Timing")]
        public AnimationCurve normalizedTimeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LocomotionRunToBehaviour>.Create(graph);
            var b = playable.GetBehaviour();

            var r = graph.GetResolver();
            b.From = from.Resolve(r);
            b.To = to.Resolve(r);

            b.DrivePosition = drivePosition;
            b.DriveRotation = driveRotation;

            b.FullSpeedMps = Mathf.Max(0.0001f, fullSpeedMetersPerSecond);
            b.SpeedMultiplier = speedMultiplier;

            b.Curve = normalizedTimeCurve;

            return playable;
        }
    }

    public class LocomotionRunToBehaviour : PlayableBehaviour
    {
        public Transform From;
        public Transform To;

        public bool DrivePosition;
        public bool DriveRotation;

        public float FullSpeedMps;
        public float SpeedMultiplier;

        public AnimationCurve Curve;
    }

    public class LocomotionRunToMixerBehaviour : PlayableBehaviour
    {
        public PlayableDirector Director;
        public TrackAsset SelfTrack;

        private Transform _targetTransform;
        private LocomotionProfileTest _locomotionTest;

        private bool _cached;
        private Vector3 _basePos;
        private Quaternion _baseRot;

#if UNITY_EDITOR
        private Animator _animator;
        private bool _cachedAnimatorSpeedValid;
        private float _cachedAnimatorSpeed = 1f;
#endif

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!TryResolveTarget())
            {
                return;
            }

#if UNITY_EDITOR
            // В Edit Mode запрещаем "самотёк" анимации: поза меняется только когда меняется время Timeline.
            SetAnimatorFrozenInEditMode(true);
#endif

            _locomotionTest.EnsureLocomotionCreated();

            var tr = _targetTransform;
            if (!_cached)
            {
                _cached = true;
                _basePos = tr.position;
                _baseRot = tr.rotation;
            }

            var loco = _locomotionTest.Locomotion;
            if (loco == null)
            {
                return;
            }

            const float eps = 1e-6f;
            double trackTime = playable.GetTime();

            // Ищем один активный input (max weight)
            bool hasActive = false;
            float activeW = 0f;

            ScriptPlayable<LocomotionRunToBehaviour> sp = default;
            LocomotionRunToBehaviour b = null;

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= eps)
                {
                    continue;
                }

                var input = playable.GetInput(i);
                if (!input.IsValid())
                {
                    continue;
                }

                if (!hasActive || w > activeW)
                {
                    hasActive = true;
                    activeW = w;

                    sp = (ScriptPlayable<LocomotionRunToBehaviour>)input;
                    b = sp.GetBehaviour();
                }
            }

            _locomotionTest.SetTimelineDriven(hasActive);

            if (!hasActive || b == null || b.To == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (trackTime <= 0.0001)
                    {
                        tr.position = _basePos;
                        tr.rotation = _baseRot;
                    }

                    loco.ClearActionPreview();
                    loco.SetAbsoluteTime(trackTime);
                    loco.UpdateLocomotion(Vector2.zero, 0f);
                    loco.EvaluateGraph(0f);
                    return;
                }
#endif
                loco.SetAbsoluteTime(trackTime);
                _locomotionTest.debugSpeed = Vector2.zero;
                loco.UpdateLocomotion(Vector2.zero, info.deltaTime);
                return;
            }

            double dur = sp.GetDuration();
            double t = sp.GetTime();

            float nt = (dur > eps) ? (float)(t / dur) : 1f;
            nt = Mathf.Clamp01(nt);

            if (b.Curve != null && b.Curve.length > 0)
            {
                nt = Mathf.Clamp01(b.Curve.Evaluate(nt));
            }

            Vector3 fromPos = b.From != null ? b.From.position : _basePos;
            Quaternion fromRot = b.From != null ? b.From.rotation : _baseRot;

            Vector3 toPos = b.To.position;
            Quaternion toRot = b.To.rotation;

            Vector3 p = Vector3.LerpUnclamped(fromPos, toPos, nt);
            Quaternion r = Quaternion.SlerpUnclamped(fromRot, toRot, nt);

            if (b.DrivePosition)
            {
                tr.position = Vector3.Lerp(_basePos, p, activeW);
            }

            if (b.DriveRotation)
            {
                tr.rotation = Quaternion.Slerp(_baseRot, r, activeW);
            }

            Vector2 finalSpeed = ComputeSpeedVector2(tr, fromPos, toPos, sp, b);

            loco.ClearActionPreview();

            // Фаза локомоции привязана к пройденной дистанции (детерминировано для скраба).
            Vector3 planarDelta = toPos - fromPos;
            planarDelta.y = 0f;

            float totalDist = planarDelta.magnitude;
            float distTraveled = totalDist * nt;

            float fullSpeed = Mathf.Max(0.0001f, b.FullSpeedMps);
            float speedMul = Mathf.Max(0.0001f, b.SpeedMultiplier);

            double locomotionTime = (distTraveled / fullSpeed) * speedMul;
            loco.SetAbsoluteTime(locomotionTime);

            _locomotionTest.debugSpeed = finalSpeed;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // В Edit Mode dt=0, и Animator.speed=0 — поза меняется только при изменении времени Timeline.
                loco.UpdateLocomotion(finalSpeed, 0f);
                loco.EvaluateGraph(0f);
                return;
            }
#endif

            loco.UpdateLocomotion(finalSpeed, info.deltaTime);
        }

        private static Vector2 ComputeSpeedVector2(
            Transform character,
            Vector3 fromPos,
            Vector3 toPos,
            ScriptPlayable<LocomotionRunToBehaviour> clipPlayable,
            LocomotionRunToBehaviour b)
        {
            const float eps = 1e-6f;

            float dur = (float)clipPlayable.GetDuration();
            float t = (float)clipPlayable.GetTime();

            if (dur <= eps)
            {
                return Vector2.zero;
            }

            float h = Mathf.Clamp(dur * 0.01f, 0.001f, 0.05f);

            float t0 = Mathf.Clamp(t - h, 0f, dur);
            float t1 = Mathf.Clamp(t + h, 0f, dur);

            float nt0 = t0 / dur;
            float nt1 = t1 / dur;

            if (b.Curve != null && b.Curve.length > 0)
            {
                nt0 = Mathf.Clamp01(b.Curve.Evaluate(nt0));
                nt1 = Mathf.Clamp01(b.Curve.Evaluate(nt1));
            }

            Vector3 p0 = Vector3.LerpUnclamped(fromPos, toPos, nt0);
            Vector3 p1 = Vector3.LerpUnclamped(fromPos, toPos, nt1);

            float dt = Mathf.Max(eps, (t1 - t0));
            Vector3 vWorld = (p1 - p0) / dt;

            Vector3 vLocal3 = character.InverseTransformDirection(vWorld);
            Vector2 vLocal = new Vector2(vLocal3.x, vLocal3.z);

            float worldPlanarSpeed = new Vector2(vWorld.x, vWorld.z).magnitude;
            float mag01 = Mathf.Clamp01(worldPlanarSpeed / Mathf.Max(0.0001f, b.FullSpeedMps));

            if (vLocal.sqrMagnitude <= 1e-10f)
            {
                return Vector2.zero;
            }

            return vLocal.normalized * (mag01 * b.SpeedMultiplier);
        }

        private bool TryResolveTarget()
        {
            if (_targetTransform != null && _locomotionTest != null)
            {
                return true;
            }

            if (Director == null || SelfTrack == null)
            {
                return false;
            }

            _targetTransform = Director.GetGenericBinding(SelfTrack) as Transform;
            if (_targetTransform == null)
            {
                return false;
            }

            _locomotionTest = _targetTransform.GetComponent<LocomotionProfileTest>();
            if (_locomotionTest == null)
            {
                _locomotionTest = _targetTransform.GetComponentInParent<LocomotionProfileTest>();
            }

            if (_locomotionTest == null)
            {
                return false;
            }

#if UNITY_EDITOR
            if (_animator == null)
            {
                _animator = _targetTransform.GetComponent<Animator>();
                if (_animator == null)
                {
                    _animator = _targetTransform.GetComponentInParent<Animator>();
                }
            }
#endif

            _cached = false;
            return true;
        }

#if UNITY_EDITOR
        private void SetAnimatorFrozenInEditMode(bool freeze)
        {
            if (Application.isPlaying)
            {
                RestoreAnimatorSpeed();
                return;
            }

            if (_animator == null)
            {
                return;
            }

            if (!_cachedAnimatorSpeedValid)
            {
                _cachedAnimatorSpeed = _animator.speed;
                _cachedAnimatorSpeedValid = true;
            }

            if (freeze)
            {
                if (Mathf.Abs(_animator.speed) > 1e-6f)
                {
                    _animator.speed = 0f;
                }
            }
            else
            {
                RestoreAnimatorSpeed();
            }
        }

        private void RestoreAnimatorSpeed()
        {
            if (_animator == null)
            {
                return;
            }

            if (!_cachedAnimatorSpeedValid)
            {
                return;
            }

            if (Mathf.Abs(_animator.speed - _cachedAnimatorSpeed) > 1e-6f)
            {
                _animator.speed = _cachedAnimatorSpeed;
            }
        }
#endif

        public override void OnPlayableDestroy(Playable playable)
        {
#if UNITY_EDITOR
            RestoreAnimatorSpeed();
            _animator = null;
            _cachedAnimatorSpeedValid = false;
#endif

            _cached = false;
            _targetTransform = null;
            _locomotionTest = null;
        }
    }
}
