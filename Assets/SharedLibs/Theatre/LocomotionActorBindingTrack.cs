using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    /// <summary>
    /// Единственный трек в группе, который хранит binding актёра.
    /// Остальные треки в этой группе читают binding отсюда через PlayableDirector.GetGenericBinding().
    /// </summary>
    [TrackColor(0.20f, 0.20f, 0.20f)]
    [TrackBindingType(typeof(Transform))]
    public class LocomotionActorBindingTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            // Трек не проигрывает ничего. Нужен только как "слот биндинга".
            return Playable.Create(graph);
        }
    }
}
