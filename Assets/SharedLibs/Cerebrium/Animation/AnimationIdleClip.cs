using System;
using UnityEngine;

namespace AlSo
{
    public interface IAnimationClip
    {
        public AnimationClip Clip { get; set; }

        public AnimationCurve LeftFootMagnet { get; set; }
        public AnimationCurve RightFootMagnet { get; set; }
        public AnimationCurve HipMaxOffset { get; set; }

        public float LeftLegYDelta { get; set; }
        public float RightLegYDelta { get; set; }
    }

    public interface IAnimationActionClip : IAnimationClip
    {
        public string Name { get; }

        public float FadeInPercent { get; }
        public float FadeOutPercent { get; }
    }


    [Serializable]
    public class AbsAnimationClip : IAnimationClip
    {
        public AnimationClip clip;
        public AnimationClip Clip
        {
            get => clip;
            set => clip = value;
        }


        public AnimationCurve leftFootMagnet = new AnimationCurve();
        public AnimationCurve LeftFootMagnet
        {
            get => leftFootMagnet;
            set => leftFootMagnet = value;
        }


        public AnimationCurve rightFootMagnet = new AnimationCurve();
        public AnimationCurve RightFootMagnet
        {
            get => rightFootMagnet;
            set => rightFootMagnet = value;
        }


        public AnimationCurve hipMaxOffset = new AnimationCurve();
        public AnimationCurve HipMaxOffset
        {
            get =>hipMaxOffset;
            set =>hipMaxOffset = value; 
        } 

        public float leftLegYDelta;
        public float LeftLegYDelta
        { 
            get => leftLegYDelta;
            set => leftLegYDelta = value;
        }
        
        public float rightLegYDelta;
        public float RightLegYDelta
        { 
            get => rightLegYDelta;
            set => rightLegYDelta = value;
        } 
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
    public class AnimationActionClip : AbsAnimationClip, IAnimationActionClip
    {
        public string name;
        public string Name => name;

        public float fadeInPercent = 0.1f;
        public float FadeInPercent => fadeInPercent;

        public float fadeOutPercent = 0.1f;
        public float FadeOutPercent => fadeOutPercent;
    }
}
