using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{

    [Serializable]
    public class LocomotionStandAtClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("Target")]
        public ExposedReference<Transform> target;

        [Header("Drive transform")]
        public bool drivePosition = true;
        public bool driveRotation = false;

        [Header("Locomotion")]
        [Tooltip("Если true — при активном клипе принудительно держим скорость 0.")]
        public bool setSpeedZero = true;

        // Можно оставить Blending, но трек упадёт при overlap (мы этого хотим).
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

    // -------------------- MIXER (exclusive) --------------------

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

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!TryResolveTarget())
            {
                return;
            }

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

            // 1) Найти единственный активный input (strict).
            bool hasActive = false;
            float activeW = 0f;

            Playable activeInput = default;
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

                if (hasActive)
                {
                    string msg =
                        $"[LocomotionStateTrack] Overlapping clips are not allowed on this track. " +
                        $"Detected multiple active inputs at timeline time={trackTime:0.###}. " +
                        $"First={activeType?.Name ?? "?"}, Second={pt.Name}.";

                    UnityEngine.Debug.LogError(msg);
                    throw new InvalidOperationException(msg);
                }

                hasActive = true;
                activeW = w;
                activeInput = input;
                activeType = pt;

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
                else // action
                {
                    actionP = (ScriptPlayable<LocomotionActionBehaviour>)input;
                    actionB = actionP.GetBehaviour();
                }
            }

            _locomotionTest.SetTimelineDriven(hasActive);

            // 2) Нет активного клипа — idle (и в edit mode откатываемся в base на time=0).
            if (!hasActive)
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

            // 3) Активен один клип — выполняем его логику.
            if (activeType == typeof(LocomotionRunToBehaviour))
            {
                if (runB == null || runB.To == null)
                {
                    return;
                }

                double dur = runP.GetDuration();
                double t = runP.GetTime();

                float nt = (dur > eps) ? (float)(t / dur) : 1f;
                nt = Mathf.Clamp01(nt);

                if (runB.Curve != null && runB.Curve.length > 0)
                {
                    nt = Mathf.Clamp01(runB.Curve.Evaluate(nt));
                }

                Vector3 fromPos = runB.From != null ? runB.From.position : _basePos;
                Quaternion fromRot = runB.From != null ? runB.From.rotation : _baseRot;

                Vector3 toPos = runB.To.position;
                Quaternion toRot = runB.To.rotation;

                Vector3 p = Vector3.LerpUnclamped(fromPos, toPos, nt);
                Quaternion r = Quaternion.SlerpUnclamped(fromRot, toRot, nt);

                // Вес клипа учитываем как fade к base
                if (runB.DrivePosition)
                {
                    tr.position = Vector3.Lerp(_basePos, p, activeW);
                }

                if (runB.DriveRotation)
                {
                    tr.rotation = Quaternion.Slerp(_baseRot, r, activeW);
                }

                Vector2 finalSpeed = ComputeSpeedVector2(tr, fromPos, toPos, runP, runB);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (_hasPrevTime && Math.Abs(trackTime - _prevTrackTime) < 1e-9)
                    {
                        finalSpeed = Vector2.zero;
                    }

                    _prevTrackTime = trackTime;
                    _hasPrevTime = true;
                }
#endif

                loco.SetAbsoluteTime(trackTime);
                _locomotionTest.debugSpeed = finalSpeed;
                loco.UpdateLocomotion(finalSpeed, info.deltaTime);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    loco.ClearActionPreview();
                    loco.EvaluateGraph(0f);
                }
#endif
                return;
            }

            if (activeType == typeof(LocomotionStandAtBehaviour))
            {
                if (standB == null || standB.Target == null)
                {
                    return;
                }

                if (standB.DrivePosition)
                {
                    tr.position = Vector3.Lerp(_basePos, standB.Target.position, activeW);
                }

                if (standB.DriveRotation)
                {
                    tr.rotation = Quaternion.Slerp(_baseRot, standB.Target.rotation, activeW);
                }

                loco.SetAbsoluteTime(trackTime);

                if (standB.SetSpeedZero)
                {
                    _locomotionTest.debugSpeed = Vector2.zero;
                    loco.UpdateLocomotion(Vector2.zero, info.deltaTime);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    loco.ClearActionPreview();
                    loco.EvaluateGraph(0f);
                }
#endif
                return;
            }

            // action
            if (actionB == null || actionB.Action == null || actionB.Action.Clip == null)
            {
                return;
            }

            if (actionB.SetSpeedZero)
            {
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
                loco.UpdateLocomotion(Vector2.zero, 0f);
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

            LocomotionActorBindingTrack bindTrack = FindActorBindingTrack(SelfTrack);
            if (bindTrack == null)
            {
                return false;
            }

            _targetTransform = Director.GetGenericBinding(bindTrack) as Transform;
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

        private static LocomotionActorBindingTrack FindActorBindingTrack(TrackAsset anyTrackInGroup)
        {
            TrackAsset parent = anyTrackInGroup != null ? anyTrackInGroup.parent as TrackAsset : null;

            while (parent != null && parent is not GroupTrack)
            {
                parent = parent.parent as TrackAsset;
            }

            if (parent == null)
            {
                return null;
            }

            foreach (TrackAsset child in parent.GetChildTracks())
            {
                if (child is LocomotionActorBindingTrack bt)
                {
                    return bt;
                }
            }

            return null;
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

            // малый шаг для численной производной по времени клипа
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

        public override void OnPlayableDestroy(Playable playable)
        {
            _cached = false;
            _hasPrevTime = false;
            _targetTransform = null;
            _locomotionTest = null;
        }
    }
}
