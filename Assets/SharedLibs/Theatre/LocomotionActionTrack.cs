using System;
using System.Collections.Generic;
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
        public bool setSpeedZero = true;

        [Header("Time remap")]
        public AnimationCurve normalizedTimeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LocomotionActionBehaviour>.Create(graph);
            var b = playable.GetBehaviour();

            b.Action = action;
            b.SetSpeedZero = setSpeedZero;
            b.Curve = normalizedTimeCurve;

            return playable;
        }
    }

    public class LocomotionActionBehaviour : PlayableBehaviour
    {
        public AnimationActionClipData Action;
        public bool SetSpeedZero;
        public AnimationCurve Curve;
    }

    public class LocomotionActionMixerBehaviour : PlayableBehaviour
    {
        // чтобы в PlayMode не спамить PerformAction каждый кадр
        private readonly Dictionary<int, double> _lastLocalTimeByInput = new Dictionary<int, double>();

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

            // берём самый “сильный” клип (по весу таймлайна) — этого достаточно для preview
            float bestW = 0f;
            ScriptPlayable<LocomotionActionBehaviour> bestP = default;
            LocomotionActionBehaviour bestB = null;

            for (int i = 0; i < inputCount; i++)
            {
                float w = playable.GetInputWeight(i);

                var input = playable.GetInput(i);
                if (!input.IsValid() || input.GetPlayableType() != typeof(LocomotionActionBehaviour))
                {
                    _lastLocalTimeByInput.Remove(i);
                    continue;
                }

                var sp = (ScriptPlayable<LocomotionActionBehaviour>)input;
                var b = sp.GetBehaviour();

                if (w > eps && b != null && b.Action != null && b.Action.Clip != null && b.Action.Clip.length > eps)
                {
                    anyActive = true;

                    if (w > bestW)
                    {
                        bestW = w;
                        bestP = sp;
                        bestB = b;
                    }
                }
                else
                {
                    _lastLocalTimeByInput.Remove(i);
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

            // --- общие вещи: при action обычно держим скорость 0 ---
            if (bestB.SetSpeedZero)
            {
                locomotionTest.debugSpeed = Vector2.zero;
                loco.UpdateLocomotion(Vector2.zero, info.deltaTime);
            }

            // абсолютное время таймлайна (нужно, чтобы базовые клипы синхронизировались при скрабе)
            double trackTime = playable.GetTime();
            loco.SetAbsoluteTime(trackTime);

            // локальное время внутри клипа action
            double dur = bestP.GetDuration();
            double t = bestP.GetTime();

            float nt = (dur > eps) ? (float)(t / dur) : 1f;
            nt = Mathf.Clamp01(nt);

            if (bestB.Curve != null && bestB.Curve.length > 0)
            {
                nt = Mathf.Clamp01(bestB.Curve.Evaluate(nt));
            }

            float clipLen = bestB.Action.Clip.length;
            float localTime = nt * clipLen;
            if (localTime >= clipLen)
            {
                localTime = Mathf.Max(0f, clipLen - 0.0001f);
            }

#if UNITY_EDITOR
            // ===== EDIT MODE: scrub через PreviewAction + EvaluateGraph(0) =====
            if (!Application.isPlaying)
            {
                // Важно: веса базового миксера должны быть выставлены (хотя бы нулём),
                // иначе у тебя может не обновляться часть кривых/hip-логики.
                loco.UpdateLocomotion(Vector2.zero, 0f);

                loco.PreviewAction(bestB.Action, localTime, bestW);
                loco.EvaluateGraph(0f);
                return;
            }
#endif

            // ===== PLAY MODE: один раз триггерим PerformAction на входе в клип =====
            for (int i = 0; i < inputCount; i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= eps)
                    continue;

                var input = playable.GetInput(i);
                if (!input.IsValid() || input.GetPlayableType() != typeof(LocomotionActionBehaviour))
                    continue;

                var sp = (ScriptPlayable<LocomotionActionBehaviour>)input;
                var b = sp.GetBehaviour();
                if (b == null || b.Action == null || b.Action.Clip == null)
                    continue;

                double lt = sp.GetTime();

                bool had = _lastLocalTimeByInput.TryGetValue(i, out double prevLt);
                bool entered =
                    !had ||
                    (prevLt <= 0.000001 && lt > 0.000001) ||
                    (lt < prevLt);

                _lastLocalTimeByInput[i] = lt;

                if (entered)
                {
                    locomotionTest.PerformAction(b.Action);
                }
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            _lastLocalTimeByInput.Clear();
        }
    }
}
