using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    [Serializable]
    public class LocomotionActionClip : PlayableAsset, ITimelineClipAsset
    {
        public AnimationActionClipData action;

        [Header("Options")]
        [Tooltip("Если true — при активном клипе принудительно держим скорость 0.")]
        public bool setSpeedZero = true;

        [Tooltip("Если true — в Play Mode тоже драйвим экшен через PreviewAction по времени Timeline (как при скрабе).")]
        public bool driveByTimelineInPlayMode = true;

        [Header("Time remap (0..1 -> 0..1)")]
        public AnimationCurve normalizedTimeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public override double duration
        {
            get
            {
                if (action == null || action.Clip == null)
                {
                    return 0.0;
                }

                double len = action.Clip.length;
                if (len <= 0.0)
                {
                    return 0.0;
                }

                return Math.Max(0.000001, len);
            }
        }

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LocomotionActionBehaviour>.Create(graph);
            var b = playable.GetBehaviour();

            b.Action = action;
            b.SetSpeedZero = setSpeedZero;
            b.DriveByTimelineInPlayMode = driveByTimelineInPlayMode;
            b.Curve = normalizedTimeCurve;

            return playable;
        }
    }

    public class LocomotionActionBehaviour : PlayableBehaviour
    {
        public AnimationActionClipData Action;
        public bool SetSpeedZero;
        public bool DriveByTimelineInPlayMode;
        public AnimationCurve Curve;
    }

    public class LocomotionActionMixerBehaviour : PlayableBehaviour
    {
        public PlayableDirector Director;
        public TrackAsset SelfTrack;

        private Transform _targetTransform;
        private LocomotionProfileTest _locomotionTest;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!TryResolveTarget())
            {
                return;
            }

            _locomotionTest.EnsureLocomotionCreated();
            var loco = _locomotionTest.Locomotion;
            if (loco == null)
            {
                return;
            }

            int inputCount = playable.GetInputCount();
            const float eps = 1e-6f;

            bool anyActive = false;

            float bestW = 0f;
            ScriptPlayable<LocomotionActionBehaviour> bestP = default;
            LocomotionActionBehaviour bestB = null;

            for (int i = 0; i < inputCount; i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= eps)
                {
                    continue;
                }

                var input = playable.GetInput(i);
                if (!input.IsValid() || input.GetPlayableType() != typeof(LocomotionActionBehaviour))
                {
                    continue;
                }

                var sp = (ScriptPlayable<LocomotionActionBehaviour>)input;
                var b = sp.GetBehaviour();

                if (b == null || b.Action == null || b.Action.Clip == null || b.Action.Clip.length <= eps)
                {
                    continue;
                }

                anyActive = true;

                if (w > bestW)
                {
                    bestW = w;
                    bestP = sp;
                    bestB = b;
                }
            }

            _locomotionTest.SetTimelineDriven(anyActive);

            if (!anyActive || bestB == null || bestW <= eps)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    loco.ClearActionPreview();
                    loco.EvaluateGraph(0f);
                }
#endif
                return;
            }

            if (bestB.SetSpeedZero)
            {
                _locomotionTest.debugSpeed = Vector2.zero;
                loco.UpdateLocomotion(Vector2.zero, info.deltaTime);
            }

            double trackTime = playable.GetTime();
            loco.SetAbsoluteTime(trackTime);

            float clipLen = bestB.Action.Clip.length;
            float localTimeSeconds = Mathf.Clamp((float)bestP.GetTime(), 0f, Mathf.Max(0f, clipLen - 0.0001f));

            if (bestB.Curve != null && bestB.Curve.length > 0)
            {
                float nt = clipLen > eps ? Mathf.Clamp01(localTimeSeconds / clipLen) : 0f;
                nt = Mathf.Clamp01(bestB.Curve.Evaluate(nt));
                localTimeSeconds = Mathf.Clamp(nt * clipLen, 0f, Mathf.Max(0f, clipLen - 0.0001f));
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                loco.UpdateLocomotion(Vector2.zero, 0f);
                loco.PreviewAction(bestB.Action, localTimeSeconds, bestW);
                loco.EvaluateGraph(0f);
                return;
            }
#endif

            if (bestB.DriveByTimelineInPlayMode)
            {
                loco.PreviewAction(bestB.Action, localTimeSeconds, bestW);
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

            // ВАЖНО: берём binding прямо с трека
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

            return _locomotionTest != null;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            _targetTransform = null;
            _locomotionTest = null;
        }
    }
}
