using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    [TrackColor(0.95f, 0.55f, 0.35f)]
    [TrackClipType(typeof(LocomotionActionClip))]
    [TrackBindingType(typeof(LocomotionProfileTest))]
    public class LocomotionActionTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<LocomotionActionMixerBehaviour>.Create(graph, inputCount);
        }
    }

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

        // фиксируем длину таймлайн-клипа по длине ассетного клипа
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

                // маленький clamp чтобы не было “нулевой длительности” из-за импорта
                return Math.Max(0.000001, len);
            }
        }

        // капсы: разрешаем blending, но запрещаем ClipIn и SpeedMultiplier,
        // чтобы таймлайн не мог “растянуть” или “сдвинуть” экшен по времени.
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
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var locomotionTest = playerData as LocomotionProfileTest;
            if (locomotionTest == null)
            {
                return;
            }

            locomotionTest.EnsureLocomotionCreated();
            var loco = locomotionTest.Locomotion;
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

            locomotionTest.SetTimelineDriven(anyActive);

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
                locomotionTest.debugSpeed = Vector2.zero;
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
    }
}
