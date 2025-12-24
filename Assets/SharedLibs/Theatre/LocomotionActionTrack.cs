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

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LocomotionActionBehaviour>.Create(graph);
            var b = playable.GetBehaviour();
            b.Action = action;
            return playable;
        }
    }

    public class LocomotionActionBehaviour : PlayableBehaviour
    {
        public AnimationActionClipData Action;
    }

    public class LocomotionActionMixerBehaviour : PlayableBehaviour
    {
        // чтобы не триггерить PerformAction каждый кадр — запоминаем, какие инпуты уже стартовали
        private readonly Dictionary<int, double> _lastLocalTimeByInput = new Dictionary<int, double>();

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var locomotionTest = playerData as LocomotionProfileTest;
            if (locomotionTest == null)
                return;

            locomotionTest.EnsureLocomotionCreated();

            int inputCount = playable.GetInputCount();
            const float eps = 1e-6f;

            bool anyActive = false;

            for (int i = 0; i < inputCount; i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= eps)
                {
                    // если клип “ушёл” — сбросим память
                    _lastLocalTimeByInput.Remove(i);
                    continue;
                }

                var input = playable.GetInput(i);
                if (!input.IsValid() || input.GetPlayableType() != typeof(LocomotionActionBehaviour))
                    continue;

                var sp = (ScriptPlayable<LocomotionActionBehaviour>)input;
                var b = sp.GetBehaviour();
                if (b == null || b.Action == null || b.Action.Clip == null)
                    continue;

                anyActive = true;

                double t = sp.GetTime(); // локальное время внутри клипа таймлайна

                // “Вход в клип”: прошлое время было около 0 или записи не было
                bool had = _lastLocalTimeByInput.TryGetValue(i, out double prevT);
                bool entered =
                    !had ||
                    (prevT <= 0.000001 && t > 0.000001) ||
                    (t < prevT); // на всякий, если что-то перемотали назад внутри клипа

                _lastLocalTimeByInput[i] = t;

                if (Application.isPlaying && entered)
                {
                    // реальный запуск действия (в Play Mode)
                    locomotionTest.PerformAction(b.Action);
                }
            }

            locomotionTest.SetTimelineDriven(anyActive);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            _lastLocalTimeByInput.Clear();
        }
    }
}
