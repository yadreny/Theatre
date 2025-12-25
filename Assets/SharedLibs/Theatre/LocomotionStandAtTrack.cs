//using System;
//using UnityEngine;
//using UnityEngine.Playables;
//using UnityEngine.Timeline;

//namespace AlSo
//{
//    [TrackColor(0.55f, 0.95f, 0.55f)]
//    [TrackClipType(typeof(LocomotionStandAtClip))]
//    public class LocomotionStandAtTrack : TrackAsset
//    {
//        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
//        {
//            var playable = ScriptPlayable<LocomotionStandAtMixerBehaviour>.Create(graph, inputCount);
//            var b = playable.GetBehaviour();

//            b.Director = go != null ? go.GetComponent<PlayableDirector>() : null;
//            b.SelfTrack = this;

//            return playable;
//        }
//    }

//    [Serializable]
//    public class LocomotionStandAtClip : PlayableAsset, ITimelineClipAsset
//    {
//        [Header("Target")]
//        public ExposedReference<Transform> target;

//        [Header("Drive transform")]
//        public bool drivePosition = true;
//        public bool driveRotation = false;

//        [Header("Locomotion")]
//        [Tooltip("Если true — при активном клипе принудительно держим скорость 0.")]
//        public bool setSpeedZero = true;

//        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn;

//        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
//        {
//            var playable = ScriptPlayable<LocomotionStandAtBehaviour>.Create(graph);
//            var b = playable.GetBehaviour();

//            var r = graph.GetResolver();
//            b.Target = target.Resolve(r);

//            b.DrivePosition = drivePosition;
//            b.DriveRotation = driveRotation;
//            b.SetSpeedZero = setSpeedZero;

//            return playable;
//        }
//    }

//    public class LocomotionStandAtBehaviour : PlayableBehaviour
//    {
//        public Transform Target;

//        public bool DrivePosition;
//        public bool DriveRotation;

//        public bool SetSpeedZero;
//    }

//    public class LocomotionStandAtMixerBehaviour : PlayableBehaviour
//    {
//        public PlayableDirector Director;
//        public TrackAsset SelfTrack;

//        private Transform _targetTransform;
//        private LocomotionProfileTest _locomotionTest;

//        private bool _cached;
//        private Vector3 _basePos;
//        private Quaternion _baseRot;

//        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
//        {
//            if (!TryResolveTarget())
//            {
//                return;
//            }

//            _locomotionTest.EnsureLocomotionCreated();

//            var tr = _targetTransform;

//            if (!_cached)
//            {
//                _cached = true;
//                _basePos = tr.position;
//                _baseRot = tr.rotation;
//            }

//            int inputCount = playable.GetInputCount();
//            const float eps = 1e-6f;

//            float sumW = 0f;

//            Vector3 posAcc = Vector3.zero;
//            bool anyPos = false;

//            Quaternion rotAcc = Quaternion.identity;
//            bool anyRot = false;
//            float rotWAcc = 0f;

//            double trackTime = playable.GetTime();

//            for (int i = 0; i < inputCount; i++)
//            {
//                float w = playable.GetInputWeight(i);
//                if (w <= eps)
//                {
//                    continue;
//                }

//                var input = playable.GetInput(i);
//                if (!input.IsValid() || input.GetPlayableType() != typeof(LocomotionStandAtBehaviour))
//                {
//                    continue;
//                }

//                var sp = (ScriptPlayable<LocomotionStandAtBehaviour>)input;
//                var b = sp.GetBehaviour();
//                if (b == null || b.Target == null)
//                {
//                    continue;
//                }

//                if (b.DrivePosition)
//                {
//                    posAcc += b.Target.position * w;
//                    anyPos = true;
//                }

//                if (b.DriveRotation)
//                {
//                    Quaternion r = b.Target.rotation;

//                    float newRotW = rotWAcc + w;
//                    float k = (newRotW > eps) ? (w / newRotW) : 1f;

//                    if (!anyRot)
//                    {
//                        rotAcc = r;
//                        anyRot = true;
//                        rotWAcc = w;
//                    }
//                    else
//                    {
//                        rotAcc = Quaternion.Slerp(rotAcc, r, k);
//                        rotWAcc = newRotW;
//                    }
//                }

//                sumW += w;
//            }

//            _locomotionTest.SetTimelineDriven(sumW > eps);

//            // Всегда стоим на месте при активном клипе, поэтому скорость = 0.
//            // Если клипов нет — тоже 0 (локомоция будет idle).
//            var loco = _locomotionTest.Locomotion;
//            if (loco != null)
//            {
//                loco.SetAbsoluteTime(trackTime);

//                _locomotionTest.debugSpeed = Vector2.zero;
//                loco.UpdateLocomotion(Vector2.zero, info.deltaTime);

//#if UNITY_EDITOR
//                if (!Application.isPlaying)
//                {
//                    loco.EvaluateGraph(0f);
//                }
//#endif
//            }

//            if (sumW <= eps)
//            {
//#if UNITY_EDITOR
//                if (!Application.isPlaying)
//                {
//                    // как в RunTo: в начале таймлайна возвращаем базу
//                    if (trackTime <= 0.0001)
//                    {
//                        tr.position = _basePos;
//                        tr.rotation = _baseRot;
//                    }
//                }
//#endif
//                return;
//            }

//            // Блендим в базовую позу при частичном весе (fade in/out)
//            if (anyPos)
//            {
//                float baseW = Mathf.Clamp01(1f - sumW);
//                Vector3 finalPos = posAcc + _basePos * baseW;
//                tr.position = finalPos;
//            }

//            if (anyRot)
//            {
//                Quaternion finalRot = Quaternion.Slerp(_baseRot, rotAcc, sumW);
//                tr.rotation = finalRot;
//            }
//        }

//        private bool TryResolveTarget()
//        {
//            if (_targetTransform != null && _locomotionTest != null)
//            {
//                return true;
//            }

//            if (Director == null || SelfTrack == null)
//            {
//                return false;
//            }

//            LocomotionActorBindingTrack bindTrack = FindActorBindingTrack(SelfTrack);
//            if (bindTrack == null)
//            {
//                return false;
//            }

//            _targetTransform = Director.GetGenericBinding(bindTrack) as Transform;
//            if (_targetTransform == null)
//            {
//                return false;
//            }

//            _locomotionTest = _targetTransform.GetComponent<LocomotionProfileTest>();
//            if (_locomotionTest == null)
//            {
//                _locomotionTest = _targetTransform.GetComponentInParent<LocomotionProfileTest>();
//            }

//            if (_locomotionTest == null)
//            {
//                return false;
//            }

//            _cached = false;
//            return true;
//        }

//        private static LocomotionActorBindingTrack FindActorBindingTrack(TrackAsset anyTrackInGroup)
//        {
//            TrackAsset parent = anyTrackInGroup != null ? anyTrackInGroup.parent as TrackAsset : null;

//            while (parent != null && parent is not GroupTrack)
//            {
//                parent = parent.parent as TrackAsset;
//            }

//            if (parent == null)
//            {
//                return null;
//            }

//            foreach (TrackAsset child in parent.GetChildTracks())
//            {
//                if (child is LocomotionActorBindingTrack bt)
//                {
//                    return bt;
//                }
//            }

//            return null;
//        }

//        public override void OnPlayableDestroy(Playable playable)
//        {
//            _cached = false;
//            _targetTransform = null;
//            _locomotionTest = null;
//        }
//    }
//}
