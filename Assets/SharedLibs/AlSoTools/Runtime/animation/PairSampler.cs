using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlSo
{
    [ExecuteInEditMode]
    public class PairSampler : MonoBehaviour
    {
        public AnimationSampler hitter;
        public AnimationSampler victim;

        public float victimOffsetInSecons;

        [Range(0, 1)]
        public float position;

        public int victimFrame;
        public int hitterFrame;

        // Update is called once per frame
        void Update()
        {
            if (Application.isPlaying) return;

            float max = Mathf.Max(hitter.clip.averageDuration, victim.clip.averageDuration);
            
            float time = position * max;

            var hitterPosition = Mathf.InverseLerp(0, hitter.clip.averageDuration, time).Clamp(); 
            var victimPosition = Mathf.InverseLerp(0, victim.clip.averageDuration, time - victimOffsetInSecons).Clamp();
            victimFrame = victim.clip.NormalizedToFrame(victimPosition);

            hitter.Sample(hitterPosition);
            victim.Sample(victimPosition);

            this.hitterFrame = hitter.clip.NormalizedToFrame(hitterPosition);
        }

        private void Start()
        {
            Debug.LogError($"sampler wasnt disabled on {this.gameObject.name}");
        }
    }
}