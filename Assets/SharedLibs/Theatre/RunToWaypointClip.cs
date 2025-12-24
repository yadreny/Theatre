using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    [Serializable]
    public class RunTransformClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("Откуда (Transform из сцены).")]
        public ExposedReference<Transform> from;

        [Tooltip("Куда (Transform из сцены).")]
        public ExposedReference<Transform> to;

        [Tooltip("Применять позицию.")]
        public bool applyPosition = true;

        [Tooltip("Применять rotation.")]
        public bool applyRotation = true;

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<RunTransformBehaviour>.Create(graph);
            var b = playable.GetBehaviour();

            b.From = from.Resolve(graph.GetResolver());
            b.To = to.Resolve(graph.GetResolver());
            b.ApplyPosition = applyPosition;
            b.ApplyRotation = applyRotation;

            return playable;
        }
    }

    public class RunTransformBehaviour : PlayableBehaviour
    {
        public Transform From;
        public Transform To;
        public bool ApplyPosition = true;
        public bool ApplyRotation = true;
    }

    [TrackColor(0.2f, 0.7f, 1.0f)]
    [TrackClipType(typeof(RunTransformClip))]
    [TrackBindingType(typeof(Transform))]
    public class RunTransformTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<RunTransformMixer>.Create(graph, inputCount);
        }
    }

    public class RunTransformMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var actor = playerData as Transform;
            if (actor == null) return;

            int inputCount = playable.GetInputCount();

            float weightSum = 0f;
            Vector3 posSum = default;
            Quaternion rotBlend = default;
            bool haveRot = false;

            bool anyPos = false;
            bool anyRot = false;

            for (int i = 0; i < inputCount; i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= 0f) continue;

                var inputPlayable = (ScriptPlayable<RunTransformBehaviour>)playable.GetInput(i);
                var b = inputPlayable.GetBehaviour();
                if (b == null) continue;

                if (b.From == null || b.To == null) continue;

                double dur = inputPlayable.GetDuration();
                double t = inputPlayable.GetTime();
                float nt = (dur > 1e-6) ? Mathf.Clamp01((float)(t / dur)) : 0f;

                Vector3 p = Vector3.Lerp(b.From.position, b.To.position, nt);
                Quaternion r = Quaternion.Slerp(b.From.rotation, b.To.rotation, nt);

                if (b.ApplyPosition)
                {
                    posSum += p * w;
                    anyPos = true;
                }

                if (b.ApplyRotation)
                {
                    if (!haveRot)
                    {
                        rotBlend = r;
                        haveRot = true;
                    }
                    else
                    {
                        // постепенное смешивание кватернионов по весам
                        float lerpT = w / (weightSum + w);
                        rotBlend = Quaternion.Slerp(rotBlend, r, lerpT);
                    }

                    anyRot = true;
                }

                weightSum += w;
            }

            if (weightSum <= 0f) return;

            if (anyPos)
                actor.position = posSum / weightSum;

            if (anyRot && haveRot)
                actor.rotation = rotBlend;
        }
    }
}
