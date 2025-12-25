using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    [TrackColor(0.65f, 0.65f, 0.65f)]
    [TrackBindingType(typeof(Transform))] // <-- ВАЖНО: binding актёра прямо на этом треке
    [TrackClipType(typeof(LocomotionRunToClip))]
    [TrackClipType(typeof(LocomotionStandAtClip))]
    [TrackClipType(typeof(LocomotionActionClip))]
    public class LocomotionStateTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = ScriptPlayable<LocomotionStateMixerBehaviour>.Create(graph, inputCount);
            var b = playable.GetBehaviour();

            b.Director = go != null ? go.GetComponent<PlayableDirector>() : null;
            b.SelfTrack = this;

            return playable;
        }
    }
}
