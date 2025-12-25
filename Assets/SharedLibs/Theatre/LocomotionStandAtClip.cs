using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    [Serializable]
    public class LocomotionStandAtClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("Target (optional)")]
        public ExposedReference<Transform> target;

        [Header("Drive transform")]
        public bool drivePosition = true;
        public bool driveRotation = false;

        [Header("Locomotion")]
        [Tooltip("Если true — при активном клипе принудительно держим скорость 0.")]
        public bool setSpeedZero = true;

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LocomotionStandAtBehaviour>.Create(graph);
            var b = playable.GetBehaviour();

            var r = graph.GetResolver();
            b.Target = target.Resolve(r);

            b.DrivePosition = drivePosition;
            b.DriveRotation = driveRotation;
            b.SetSpeedZero = setSpeedZero;

            return playable;
        }
    }

    public class LocomotionStandAtBehaviour : PlayableBehaviour
    {
        public Transform Target;

        public bool DrivePosition;
        public bool DriveRotation;

        public bool SetSpeedZero;
    }

    public class LocomotionStateMixerBehaviour : PlayableBehaviour
    {
        public PlayableDirector Director;
        public TrackAsset SelfTrack;

        private Transform _targetTransform;
        private LocomotionProfileTest _locomotionTest;

        private bool _cached;
        private Vector3 _basePos;
        private Quaternion _baseRot;

        private bool _hasPrevTime;
        private double _prevTrackTime;

        private const float AccelNorm = 0.15f;
        private const float DecelNorm = 0.15f;

        private struct RunToRefs
        {
            public Transform From;
            public Transform To;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!TryResolveTarget())
            {
                return;
            }

            _locomotionTest.EnsureLocomotionCreated();

            Transform tr = _targetTransform;
            if (!_cached)
            {
                _cached = true;
                _basePos = tr.position;
                _baseRot = tr.rotation;
            }

            LocomotionSystem loco = _locomotionTest.Locomotion;
            if (loco == null)
            {
                return;
            }

            const float eps = 1e-6f;
            double trackTime = playable.GetTime();

            bool hasActive = false;
            float activeW = 0f;
            Type activeType = null;

            ScriptPlayable<LocomotionRunToBehaviour> runP = default;
            LocomotionRunToBehaviour runB = null;

            ScriptPlayable<LocomotionStandAtBehaviour> standP = default;
            LocomotionStandAtBehaviour standB = null;

            ScriptPlayable<LocomotionActionBehaviour> actionP = default;
            LocomotionActionBehaviour actionB = null;

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

                var pt = input.GetPlayableType();
                if (pt != typeof(LocomotionRunToBehaviour) &&
                    pt != typeof(LocomotionStandAtBehaviour) &&
                    pt != typeof(LocomotionActionBehaviour))
                {
                    continue;
                }

                if (!hasActive || w > activeW)
                {
                    hasActive = true;
                    activeW = w;
                    activeType = pt;

                    runP = default;
                    runB = null;
                    standP = default;
                    standB = null;
                    actionP = default;
                    actionB = null;

                    if (pt == typeof(LocomotionRunToBehaviour))
                    {
                        runP = (ScriptPlayable<LocomotionRunToBehaviour>)input;
                        runB = runP.GetBehaviour();
                    }
                    else if (pt == typeof(LocomotionStandAtBehaviour))
                    {
                        standP = (ScriptPlayable<LocomotionStandAtBehaviour>)input;
                        standB = standP.GetBehaviour();
                    }
                    else
                    {
                        actionP = (ScriptPlayable<LocomotionActionBehaviour>)input;
                        actionB = actionP.GetBehaviour();
                    }
                }
            }

            _locomotionTest.SetTimelineDriven(hasActive);

            float dt = Application.isPlaying ? info.deltaTime : 0f;

            if (!hasActive)
            {
                _locomotionTest.ClearTimelineWorldVelocity();

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

            // ===================== RunTo =====================
            if (activeType == typeof(LocomotionRunToBehaviour))
            {
                if (runB == null)
                {
                    _locomotionTest.ClearTimelineWorldVelocity();
                    return;
                }

                RunToRefs refs = ResolveRunToRefs(playable, runB);
                if (refs.To == null)
                {
                    _locomotionTest.ClearTimelineWorldVelocity();
                    return;
                }

                double dur = runP.GetDuration();
                double t = runP.GetTime();

                float ntTime = (dur > eps) ? (float)(t / dur) : 1f;
                ntTime = Mathf.Clamp01(ntTime);

                float nt = EvaluateMotionNt(runB, ntTime);

                Vector3 fromPos = refs.From != null ? refs.From.position : _basePos;
                Quaternion fromRot = refs.From != null ? refs.From.rotation : _baseRot;

                Vector3 toPos = refs.To.position;
                Quaternion toRot = refs.To.rotation;

                Vector3 p = Vector3.LerpUnclamped(fromPos, toPos, nt);
                Quaternion r = Quaternion.SlerpUnclamped(fromRot, toRot, nt);

                if (runB.DrivePosition)
                {
                    tr.position = Vector3.Lerp(_basePos, p, activeW);
                }

                if (runB.DriveRotation)
                {
                    tr.rotation = Quaternion.Slerp(_baseRot, r, activeW);
                }
                else
                {
                    _locomotionTest.ApplyTimelineOrientation(_baseRot, dt);
                }

                Vector3 vWorldPlanar = ComputeWorldVelocityPlanar(fromPos, toPos, (float)dur, (float)t, runB);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (_hasPrevTime && Math.Abs(trackTime - _prevTrackTime) < 1e-9)
                    {
                        vWorldPlanar = Vector3.zero;
                    }

                    _prevTrackTime = trackTime;
                    _hasPrevTime = true;
                }
#endif

                _locomotionTest.SetTimelineWorldVelocity(vWorldPlanar, activeW);

                Vector3 planarDelta = toPos - fromPos;
                planarDelta.y = 0f;

                float totalDist = planarDelta.magnitude;
                float distTraveled = totalDist * nt;

                float fullSpeed = Mathf.Max(0.0001f, runB.FullSpeedMps);
                float speedMul = Mathf.Max(0.0001f, runB.SpeedMultiplier);

                double locomotionTime = (distTraveled / fullSpeed) * speedMul;
                loco.SetAbsoluteTime(locomotionTime);

                Vector2 localSpeed = _locomotionTest.GetTimelineLocalSpeedForGraph();
                _locomotionTest.debugSpeed = localSpeed;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    loco.ClearActionPreview();
                    loco.UpdateLocomotion(localSpeed, 0f);
                    loco.EvaluateGraph(0f);
                    return;
                }
#endif

                loco.ClearActionPreview();
                loco.UpdateLocomotion(localSpeed, info.deltaTime);
                return;
            }

            // ===================== StandAt =====================
            if (activeType == typeof(LocomotionStandAtBehaviour))
            {
                _locomotionTest.SetTimelineWorldVelocity(Vector3.zero, 0f);

                if (standB != null && standB.Target != null)
                {
                    if (standB.DrivePosition)
                    {
                        tr.position = Vector3.Lerp(_basePos, standB.Target.position, activeW);
                    }

                    if (standB.DriveRotation)
                    {
                        tr.rotation = Quaternion.Slerp(_baseRot, standB.Target.rotation, activeW);
                    }
                    else
                    {
                        _locomotionTest.ApplyTimelineOrientation(_baseRot, dt);
                    }
                }
                else
                {
                    if (standB == null || !standB.DriveRotation)
                    {
                        _locomotionTest.ApplyTimelineOrientation(_baseRot, dt);
                    }
                }

                loco.ClearActionPreview();
                loco.SetAbsoluteTime(trackTime);

                Vector2 standSpeed = Vector2.zero;
                if (standB != null && !standB.SetSpeedZero)
                {
                    standSpeed = _locomotionTest.debugSpeed;
                }
                else
                {
                    _locomotionTest.debugSpeed = Vector2.zero;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    loco.UpdateLocomotion(standSpeed, 0f);
                    loco.EvaluateGraph(0f);
                    return;
                }
#endif

                loco.UpdateLocomotion(standSpeed, info.deltaTime);
                return;
            }

            // ===================== Action =====================
            if (actionB == null || actionB.Action == null || actionB.Action.Clip == null)
            {
                _locomotionTest.ClearTimelineWorldVelocity();
                return;
            }

            // во время action тоже можно держать “смотрим на цель”
            _locomotionTest.ApplyTimelineOrientation(_baseRot, dt);

            if (actionB.SetSpeedZero)
            {
                _locomotionTest.SetTimelineWorldVelocity(Vector3.zero, 0f);
                _locomotionTest.debugSpeed = Vector2.zero;
                loco.UpdateLocomotion(Vector2.zero, info.deltaTime);
            }

            loco.SetAbsoluteTime(trackTime);

            float clipLen = actionB.Action.Clip.length;
            float localTimeSeconds = Mathf.Clamp((float)actionP.GetTime(), 0f, Mathf.Max(0f, clipLen - 0.0001f));

            if (actionB.Curve != null && actionB.Curve.length > 0)
            {
                float nt = clipLen > eps ? Mathf.Clamp01(localTimeSeconds / clipLen) : 0f;
                nt = Mathf.Clamp01(actionB.Curve.Evaluate(nt));
                localTimeSeconds = Mathf.Clamp(nt * clipLen, 0f, Mathf.Max(0f, clipLen - 0.0001f));
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                loco.UpdateLocomotion(_locomotionTest.debugSpeed, 0f);
                loco.PreviewAction(actionB.Action, localTimeSeconds, activeW);
                loco.EvaluateGraph(0f);
                return;
            }
#endif

            if (actionB.DriveByTimelineInPlayMode)
            {
                loco.PreviewAction(actionB.Action, localTimeSeconds, activeW);
                loco.EvaluateGraph(0f);
                return;
            }
        }

        private static float EvaluateMotionNt(LocomotionRunToBehaviour b, float ntTime01)
        {
            ntTime01 = Mathf.Clamp01(ntTime01);

            float nt = ApplyAccelDecelProgress(ntTime01);

            if (b.Curve != null && b.Curve.length > 0)
            {
                nt = Mathf.Clamp01(b.Curve.Evaluate(nt));
            }

            return Mathf.Clamp01(nt);
        }

        private static float ApplyAccelDecelProgress(float t01)
        {
            t01 = Mathf.Clamp01(t01);

            float a = Mathf.Clamp01(AccelNorm);
            float d = Mathf.Clamp01(DecelNorm);

            if (a + d >= 0.999f)
            {
                float k = 0.999f / Mathf.Max(1e-6f, (a + d));
                a *= k;
                d *= k;
            }

            float plateauEnd = 1f - d;

            float totalArea = 1f - 0.5f * (a + d);
            if (totalArea <= 1e-6f)
            {
                return t01;
            }

            float area;

            if (t01 <= a)
            {
                if (a <= 1e-6f) return 0f;
                area = 0.5f * (t01 * t01) / a;
            }
            else if (t01 <= plateauEnd)
            {
                area = 0.5f * a + (t01 - a);
            }
            else
            {
                float u = t01 - plateauEnd;
                if (d <= 1e-6f) return 1f;

                float areaToPlateauEnd = 0.5f * a + (plateauEnd - a);
                float areaDecel = u - 0.5f * (u * u) / d;
                area = areaToPlateauEnd + areaDecel;
            }

            return Mathf.Clamp01(area / totalArea);
        }

        private static Vector3 ComputeWorldVelocityPlanar(
            Vector3 fromPos,
            Vector3 toPos,
            float clipDuration,
            float clipTime,
            LocomotionRunToBehaviour b)
        {
            const float eps = 1e-6f;

            if (clipDuration <= eps)
            {
                return Vector3.zero;
            }

            float h = Mathf.Clamp(clipDuration * 0.01f, 0.001f, 0.05f);

            float t0 = Mathf.Clamp(clipTime - h, 0f, clipDuration);
            float t1 = Mathf.Clamp(clipTime + h, 0f, clipDuration);

            float ntTime0 = t0 / clipDuration;
            float ntTime1 = t1 / clipDuration;

            float nt0 = EvaluateMotionNt(b, ntTime0);
            float nt1 = EvaluateMotionNt(b, ntTime1);

            Vector3 p0 = Vector3.LerpUnclamped(fromPos, toPos, nt0);
            Vector3 p1 = Vector3.LerpUnclamped(fromPos, toPos, nt1);

            float dt = Mathf.Max(eps, (t1 - t0));
            Vector3 v = (p1 - p0) / dt;
            v.y = 0f;
            return v;
        }

        private RunToRefs ResolveRunToRefs(Playable playable, LocomotionRunToBehaviour b)
        {
            RunToRefs r = default;
            r.From = b.FromFallback;
            r.To = b.ToFallback;

            if (b.Asset == null)
            {
                return r;
            }

            var resolver = playable.GetGraph().GetResolver();
            var table = resolver as IExposedPropertyTable;
            if (table == null)
            {
                return r;
            }

            PropertyName fromKey = b.Asset.from.exposedName;
            PropertyName toKey = b.Asset.to.exposedName;

            if (fromKey != default)
            {
                bool valid;
                UnityEngine.Object obj = table.GetReferenceValue(fromKey, out valid);
                if (valid && obj is Transform tr)
                {
                    r.From = tr;
                }
            }

            if (toKey != default)
            {
                bool valid;
                UnityEngine.Object obj = table.GetReferenceValue(toKey, out valid);
                if (valid && obj is Transform tr)
                {
                    r.To = tr;
                }
            }

            return r;
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

            _cached = false;
            _hasPrevTime = false;
            return true;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            _cached = false;
            _hasPrevTime = false;
            _targetTransform = null;
            _locomotionTest = null;
        }
    }
}
