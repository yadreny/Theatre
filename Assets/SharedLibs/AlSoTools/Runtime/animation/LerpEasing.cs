using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public enum EasingType
    {
        Linear,
        CircleIn,
        CircleOut,
        CubicIn,
        CubicOut,
        CubicInOut,
        QuarticOut,
        QuintOut,
        SinIn,
        SinOut,
        SquaredIn
    }

    public static class Easings
    {
        public static float Ease(float t, EasingType easingType)
            => easingType switch
            {
                EasingType.CubicOut => CubicOut(t),
                EasingType.CubicInOut => CubicInOut(t),
                EasingType.QuarticOut => QuarticOut(t),
                EasingType.QuintOut => QuintOut(t),
                EasingType.SinOut => SinOut(t),
                EasingType.CircleOut => CircleOut(t),
                EasingType.CubicIn => CubicIn(t),
                EasingType.CircleIn => CircleIn(t),
                EasingType.SinIn => SinIn(t),
                EasingType.SquaredIn => SquaredIn(t),
                _ => t,
            };

        private static float CubicOut(float t)
        {
            t -= 1f;
            return t * t * t + 1f;
        }

        private static float CubicIn(float t)
        {
            return t * t * t;
        }

        private static float SquaredIn(float t)
        {
            return t * t;
        }

        private static float CircleIn(float t)
        {
            return 1f - Mathf.Sqrt(1f - t * t);
        }

        private static float CircleOut(float t)
        {
            return Mathf.Sqrt((2f - t) * t);
        }

        private static float CubicInOut(float t)
        {
            if (t < 0.5f)
            {
                return 4f * t * t * t;
            }
            t = 2f * t - 2f;
            return 0.5f * t * t * t + 1f;
        }

        private static float QuintOut(float t)
        {
            t -= 1f;
            return t * t * t * t * t + 1f;
        }

        private static float QuarticOut(float t)
        {
            float num = t - 1f;
            return num * num * num * (1f - t) + 1f;
        }

        private static float SinIn(float t)
        {
            return Mathf.Sin((t - 1) * Mathf.PI / 2) + 1f;
        }

        private static float SinOut(float t)
        {
            return Mathf.Sin(t * Mathf.PI * 0.5f);
        }
    }
}