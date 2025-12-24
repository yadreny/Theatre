using System;
using UnityEngine;

namespace AlSo
{
    [Serializable]
    public class AbsAnimationClip
    {
        public AnimationClip clip;

        public AnimationCurve leftFootMagnet = new AnimationCurve();
        public AnimationCurve rightFootMagnet = new AnimationCurve();
        public AnimationCurve hipMaxOffset = new AnimationCurve();

        public float leftLegYDelta;
        public float rightLegYDelta;
    }


    [Serializable]
    public  class AnimationIdleClip : AbsAnimationClip
    {
    }

    [Serializable]
    public class AnimationMoveClip : AbsAnimationClip
    {
    }

    [Serializable]
    public class AnimationActionClip : AbsAnimationClip
    {
        public string name;

        public float fadeInPercent = 0.1f;
        public float fadeOutPercent = 0.1f;
    }
}
