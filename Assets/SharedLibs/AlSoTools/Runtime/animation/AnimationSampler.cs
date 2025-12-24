using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlSo
{
    [ExecuteInEditMode]
    public class AnimationSampler : MonoBehaviour
    {
        public AnimationClip clip;

        [Range(0, 1)]
        public float position;
        public int frame;

        private void Start()
        {
            Debug.LogError($"sampler wasnt disabled on {this.gameObject.name}");
        }

        void Update()
        {
            if (Application.isPlaying) return;
            if (clip == null) return;

            this.gameObject.GetComponent<Animator>().applyRootMotion = false;
            Sample(position);
        }

        public void Sample(float pos)
        {
            clip.SampleAnimation(this.gameObject, pos * clip.averageDuration);
            frame = (int)(clip.frameRate * pos * clip.averageDuration);
        }


    }

}