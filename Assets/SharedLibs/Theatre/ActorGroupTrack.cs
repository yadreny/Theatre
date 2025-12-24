using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    /// <summary>
    /// Контейнер-группа для актёра.
    /// В Timeline биндим Transform актёра через ExposedReference (в инспекторе группы).
    /// Дочерние треки (Move/Action/Emotion/...) читают его и не требуют собственного binding field.
    /// </summary>
    [Serializable]
    [TrackColor(0.18f, 0.18f, 0.18f)]
    public class LocomotionActorGroupTrack : GroupTrack
    {
        [Header("Actor (set on the group)")]
        public ExposedReference<Transform> actor;
    }
}
