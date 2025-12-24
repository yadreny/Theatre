using UnityEngine;

namespace AlSo
{
    public class MinMaxer
    {
        public float Max { get; private set; } = float.MinValue;
        public float Min { get; private set; } = float.MaxValue;

        public string Status => $"{Min}-{Max}";

        public void Update(float x, bool show = false)
        {
            Max = Mathf.Max(Max, x);
            Min = Mathf.Min(Min, x);
            if (show) Debug.LogError(Status);
        }
    }
}
