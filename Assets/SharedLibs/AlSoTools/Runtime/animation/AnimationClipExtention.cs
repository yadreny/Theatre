using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public static class AnimationClipExtention
    {
        public static int NormalizedToFrame(this AnimationClip clip, float position) => (int)(position * clip.averageDuration * clip.frameRate);
    }
}
