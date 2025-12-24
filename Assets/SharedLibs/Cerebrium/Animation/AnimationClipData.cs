using UnityEngine;
using Sirenix.OdinInspector;

namespace AlSo
{
    [CreateAssetMenu(fileName = "AnimationClipData", menuName = "AlSo/Animation/Clip Data", order = 10)]
    public class AnimationClipData : SerializedScriptableObject, IAnimationClip
    {
        [SerializeField] private AnimationClip _clip;

        [SerializeField] private AnimationCurve _leftFootMagnet = new AnimationCurve();
        [SerializeField] private AnimationCurve _rightFootMagnet = new AnimationCurve();
        [SerializeField] private AnimationCurve _hipMaxOffset = new AnimationCurve();

        [SerializeField] private float _leftLegYDelta;
        [SerializeField] private float _rightLegYDelta;

        public AnimationClip Clip
        {
            get => _clip;
            set => _clip = value;
        }

        public AnimationCurve LeftFootMagnet
        {
            get => _leftFootMagnet;
            set => _leftFootMagnet = value ?? new AnimationCurve();
        }

        public AnimationCurve RightFootMagnet
        {
            get => _rightFootMagnet;
            set => _rightFootMagnet = value ?? new AnimationCurve();
        }

        public AnimationCurve HipMaxOffset
        {
            get => _hipMaxOffset;
            set => _hipMaxOffset = value ?? new AnimationCurve();
        }

        public float LeftLegYDelta
        {
            get => _leftLegYDelta;
            set => _leftLegYDelta = value;
        }

        public float RightLegYDelta
        {
            get => _rightLegYDelta;
            set => _rightLegYDelta = value;
        }
    }

}
