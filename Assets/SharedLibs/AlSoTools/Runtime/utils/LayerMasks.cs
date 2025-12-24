using UnityEngine;

namespace AlSo
{
    public static class LayerMasks
    {
        public static LayerMask Default { get; private set; } = LayerMask.GetMask("Default");
        public static LayerMask IgnoreRaycast { get; private set; } = LayerMask.GetMask("Ignore Raycast");

    }
}
