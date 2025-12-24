using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace AlSo
{
    [CreateAssetMenu(
        fileName = "LocomotionProfile",
        menuName = "AlSo/Locomotion/Profile",
        order = 10)]
    public class LocomotionProfile : SerializedScriptableObject, IFreedomWeightedSource
    {
        public AnimationIdleClip idle;
        public AnimationMoveClip[] moves;
        public AnimationActionClip[] actions;

        [NonSerialized] private Vector2[] _cachedPoints;
        [NonSerialized] private bool _pointsComputed;

        public AnimationClip[] RuntimeClips
        {
            get
            {
                int moveCount = moves != null ? moves.Length : 0;
                var result = new AnimationClip[1 + moveCount];

                result[0] = idle != null ? idle.Clip : null;

                for (int i = 0; i < moveCount; i++)
                {
                    AnimationMoveClip m = moves[i];
                    result[1 + i] = m != null ? m.Clip : null;
                }

                return result;
            }
        }

        public Vector2[] Points
        {
            get
            {
                EnsurePointsComputed();
                return _cachedPoints ?? Array.Empty<Vector2>();
            }
        }

        public void EnsurePointsComputed()
        {
            int moveCount = moves != null ? moves.Length : 0;
            int total = 1 + moveCount;

            if (_pointsComputed &&
                _cachedPoints != null &&
                _cachedPoints.Length == total)
            {
                return;
            }

            _cachedPoints = new Vector2[total];

            // idle всегда (0,0)
            _cachedPoints[0] = Vector2.zero;

            for (int i = 0; i < moveCount; i++)
            {
                AnimationMoveClip m = moves[i];
                if (m == null || m.Clip == null)
                {
                    _cachedPoints[1 + i] = Vector2.zero;
                    continue;
                }

                Vector3 avg = m.Clip.averageSpeed;
                _cachedPoints[1 + i] = new Vector2(avg.x, avg.z);
            }

            _pointsComputed = true;
        }

        public AnimationActionClip FindAction(string name)
        {
            if (string.IsNullOrEmpty(name) || actions == null)
            {
                return null;
            }

            for (int i = 0; i < actions.Length; i++)
            {
                AnimationActionClip a = actions[i];
                if (a != null && a.Clip != null && a.Name == name)
                {
                    return a;
                }
            }

            return null;
        }

        public LocomotionSystem CreateLocomotion(Animator animator)
        {
            if (animator == null)
            {
                UnityEngine.Debug.LogError("[LocomotionProfile] Animator is null.");
                return null;
            }

            AnimationClip[] clips = RuntimeClips;
            bool hasAny = false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    hasAny = true;
                    break;
                }
            }

            if (!hasAny)
            {
                UnityEngine.Debug.LogError("[LocomotionProfile] No prepared clips. Build profile first.");
                return null;
            }

            _pointsComputed = false;
            EnsurePointsComputed();
            return new LocomotionSystem(this, animator);
        }
    }
}
