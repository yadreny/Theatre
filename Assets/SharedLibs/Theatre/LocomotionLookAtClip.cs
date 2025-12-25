using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    [Serializable]
    public class LocomotionLookAtClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("Look At Target")]
        public ExposedReference<Transform> target;

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LocomotionLookAtBehaviour>.Create(graph);
            var b = playable.GetBehaviour();

            b.Asset = this;

            var r = graph.GetResolver();
            b.TargetFallback = target.Resolve(r);

            return playable;
        }
    }

    public class LocomotionLookAtBehaviour : PlayableBehaviour
    {
        public LocomotionLookAtClip Asset;
        public Transform TargetFallback;
    }

    // ВАЖНО: НЕ требуем binding актёра на этом треке!
    [TrackColor(0.55f, 0.65f, 0.95f)]
    [TrackClipType(typeof(LocomotionLookAtClip))]
    public class LocomotionOrientationTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var p = ScriptPlayable<LocomotionOrientationMixerBehaviour>.Create(graph, inputCount);
            var b = p.GetBehaviour();
            b.Director = go != null ? go.GetComponent<PlayableDirector>() : null;
            b.SelfTrack = this;
            return p;
        }
    }

    public class LocomotionOrientationMixerBehaviour : PlayableBehaviour
    {
        public PlayableDirector Director;
        public TrackAsset SelfTrack;

        private Transform _actorTransform;
        private LocomotionProfileTest _locomotionTest;

        private struct LookAtRef
        {
            public Transform Target;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!TryResolveActorFromGroup())
            {
                return;
            }

            const float eps = 1e-6f;

            Vector3 sumDir = Vector3.zero;
            float sumW = 0f;

            Transform debugTarget = null;
            float debugTargetW = 0f;

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= eps)
                {
                    continue;
                }

                var input = playable.GetInput(i);
                if (!input.IsValid() || input.GetPlayableType() != typeof(LocomotionLookAtBehaviour))
                {
                    continue;
                }

                var sp = (ScriptPlayable<LocomotionLookAtBehaviour>)input;
                var b = sp.GetBehaviour();
                LookAtRef r = ResolveLookAtRef(playable, b);

                if (r.Target == null)
                {
                    continue;
                }

                Vector3 dir = r.Target.position - _actorTransform.position;
                dir.y = 0f;

                if (dir.sqrMagnitude <= 1e-8f)
                {
                    continue;
                }

                Vector3 dirN = dir.normalized;

                sumDir += dirN * w;
                sumW += w;

                if (debugTarget == null || w > debugTargetW)
                {
                    debugTarget = r.Target;
                    debugTargetW = w;
                }
            }

            if (sumW <= eps || sumDir.sqrMagnitude <= 1e-8f)
            {
                _locomotionTest.ClearOrientation();
                return;
            }

            Vector3 forward = sumDir.normalized;

            // вес — сумма весов активных клипов (в кроссфейде будет плавно)
            float weight01 = Mathf.Clamp01(sumW);

            _locomotionTest.SetOrientationForward(forward, weight01, debugTarget);
        }

        private LookAtRef ResolveLookAtRef(Playable playable, LocomotionLookAtBehaviour b)
        {
            LookAtRef r = default;
            r.Target = b != null ? b.TargetFallback : null;

            if (b == null || b.Asset == null)
            {
                return r;
            }

            var resolver = playable.GetGraph().GetResolver();
            var table = resolver as IExposedPropertyTable;
            if (table == null)
            {
                return r;
            }

            PropertyName key = b.Asset.target.exposedName;
            if (key == default)
            {
                return r;
            }

            bool valid;
            UnityEngine.Object obj = table.GetReferenceValue(key, out valid);
            if (valid && obj is Transform tr)
            {
                r.Target = tr;
            }

            return r;
        }

        private bool TryResolveActorFromGroup()
        {
            if (_actorTransform != null && _locomotionTest != null)
            {
                return true;
            }

            if (Director == null || SelfTrack == null)
            {
                return false;
            }

            // 1) берём parent-группу (GroupTrack тоже TrackAsset)
            TrackAsset parent = SelfTrack.parent as TrackAsset;

            // 2) ищем в группе LocomotionStateTrack и берём binding актёра с него
            if (parent != null)
            {
                foreach (TrackAsset child in parent.GetChildTracks())
                {
                    if (child is LocomotionStateTrack)
                    {
                        var tr = Director.GetGenericBinding(child) as Transform;
                        if (tr != null)
                        {
                            _actorTransform = tr;
                            break;
                        }
                    }
                }
            }

            // 3) fallback: если вдруг кто-то всё же забиндил этот трек — возьмём его
            if (_actorTransform == null)
            {
                _actorTransform = Director.GetGenericBinding(SelfTrack) as Transform;
            }

            if (_actorTransform == null)
            {
                return false;
            }

            _locomotionTest = _actorTransform.GetComponent<LocomotionProfileTest>();
            if (_locomotionTest == null)
            {
                _locomotionTest = _actorTransform.GetComponentInParent<LocomotionProfileTest>();
            }

            return _locomotionTest != null;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            _actorTransform = null;
            _locomotionTest = null;
        }
    }
}
