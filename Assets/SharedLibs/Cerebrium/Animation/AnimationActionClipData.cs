using UnityEngine;

namespace AlSo
{
    [CreateAssetMenu(fileName = "AnimationActionClipData", menuName = "AlSo/Animation/Action Clip Data", order = 11)]
    public class AnimationActionClipData : AnimationClipData, IAnimationActionClip
    {
        [SerializeField] private string _name;

        [SerializeField, Range(0f, 1f)] private float _fadeInPercent = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _fadeOutPercent = 0.1f;

        public string Name => _name;

        public float FadeInPercent => _fadeInPercent;
        public float FadeOutPercent => _fadeOutPercent;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_fadeInPercent < 0f) _fadeInPercent = 0f;
            if (_fadeInPercent > 1f) _fadeInPercent = 1f;

            if (_fadeOutPercent < 0f) _fadeOutPercent = 0f;
            if (_fadeOutPercent > 1f) _fadeOutPercent = 1f;
        }
#endif
    }

}