using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlSo
{
    [RequireComponent(typeof(Animator))]
    public class LocomotionProfileBuilder : SerializedMonoBehaviour
    {
        public LocomotionProfile targetProfile;
        public Animator samplingAnimator;

        public AnimationClip idleSource;
        public AnimationClip[] moveSources;

        [Serializable]
        public class ActionSource
        {
            public string name;
            public AnimationClip clip;
            public float fadeInPercent = 0.1f;
            public float fadeOutPercent = 0.1f;
        }

        public ActionSource[] actionSources;

        public bool retimeMoveClips = true;
        public string preparedFolderSuffix = "_Prepared";

        public int curveSamplesPerSecond = 30;

        // Нормализованная высота (0..1), ниже которой магнит = 1
        public float totalMagnet = 0.1f;

        // Нормализованная высота (0..1), выше которой магнит = 0
        public float partialMagnet = 0.2f;

#if UNITY_EDITOR

        [Serializable]
        private class FootAndHipBakeResult
        {
            public AnimationCurve leftFootMagnet;
            public AnimationCurve rightFootMagnet;
            public AnimationCurve hipMaxOffset;

            public float leftLegYDelta;
            public float rightLegYDelta;

            public static FootAndHipBakeResult Empty()
            {
                return new FootAndHipBakeResult
                {
                    leftFootMagnet = new AnimationCurve(),
                    rightFootMagnet = new AnimationCurve(),
                    hipMaxOffset = new AnimationCurve(),
                    leftLegYDelta = 0f,
                    rightLegYDelta = 0f
                };
            }
        }

        private Animator ResolveSamplingAnimator()
        {
            if (samplingAnimator != null)
            {
                return samplingAnimator;
            }

            samplingAnimator = GetComponent<Animator>();
            return samplingAnimator;
        }

        [Button("Build Profile", ButtonSizes.Medium)]
        [ContextMenu("Build Profile")]
        public void BuildProfile()
        {
            if (targetProfile == null)
            {
                UnityEngine.Debug.LogError("[LocomotionProfileBuilder] Target profile is null.");
                return;
            }

            Animator anim = ResolveSamplingAnimator();
            if (anim == null)
            {
                UnityEngine.Debug.LogError("[LocomotionProfileBuilder] Sampling Animator is null.");
                return;
            }

            if (!anim.isHuman || anim.avatar == null || !anim.avatar.isValid)
            {
                UnityEngine.Debug.LogError("[LocomotionProfileBuilder] Sampling Animator must be humanoid with valid Avatar.");
                return;
            }

            string preparedFolder = ResolvePreparedFolder();
            if (string.IsNullOrEmpty(preparedFolder))
            {
                UnityEngine.Debug.LogError("[LocomotionProfileBuilder] Cannot resolve prepared folder.");
                return;
            }

            // ---- средняя длина move-клипов ----
            float targetMoveLength = 0f;
            if (retimeMoveClips)
            {
                targetMoveLength = ComputeAverageMoveLength();
                if (targetMoveLength <= 0f)
                {
                    UnityEngine.Debug.LogWarning("[LocomotionProfileBuilder] No valid move clips for retime.");
                }
                else
                {
                    UnityEngine.Debug.Log(
                        "[LocomotionProfileBuilder] Target move length = " +
                        targetMoveLength.ToString("F3") + "s");
                }
            }

            // ----- Idle -----
            if (idleSource != null)
            {
                AnimationClip idlePrepared = DuplicateAndRetimeClip(
                    idleSource,
                    idleSource.length, // idle длину не меняем
                    preparedFolder,
                    "_IdlePrepared");

                float idleOffset = FindCycleOffset(idlePrepared, anim);
                SetClipCycleOffset(idlePrepared, idleOffset);

                if (targetProfile.idle == null)
                {
                    targetProfile.idle = new AnimationIdleClip();
                }

                targetProfile.idle.Clip = idlePrepared;

                FootAndHipBakeResult idleBake = BakeFootAndHipCurves(anim, idlePrepared);
                targetProfile.idle.LeftFootMagnet = idleBake.leftFootMagnet;
                targetProfile.idle.RightFootMagnet = idleBake.rightFootMagnet;
                targetProfile.idle.HipMaxOffset = idleBake.hipMaxOffset;
                targetProfile.idle.LeftLegYDelta = idleBake.leftLegYDelta;
                targetProfile.idle.RightLegYDelta = idleBake.rightLegYDelta;
            }
            else
            {
                targetProfile.idle = null;
            }

            // ----- Moves (все приводим к targetMoveLength) -----
            List<AnimationMoveClip> moveList = new List<AnimationMoveClip>();

            if (moveSources != null)
            {
                for (int i = 0; i < moveSources.Length; i++)
                {
                    AnimationClip src = moveSources[i];
                    if (src == null)
                    {
                        continue;
                    }

                    float targetLength = src.length;
                    if (retimeMoveClips && targetMoveLength > 0f)
                    {
                        targetLength = targetMoveLength;
                    }

                    AnimationClip prepared = DuplicateAndRetimeClip(
                        src,
                        targetLength,
                        preparedFolder,
                        "_Move" + i);

                    float moveOffset = FindCycleOffset(prepared, anim);
                    SetClipCycleOffset(prepared, moveOffset);

                    AnimationMoveClip moveData = new AnimationMoveClip
                    {
                        Clip = prepared
                    };

                    FootAndHipBakeResult moveBake = BakeFootAndHipCurves(anim, prepared);
                    moveData.LeftFootMagnet = moveBake.leftFootMagnet;
                    moveData.RightFootMagnet = moveBake.rightFootMagnet;
                    moveData.HipMaxOffset = moveBake.hipMaxOffset;
                    moveData.LeftLegYDelta = moveBake.leftLegYDelta;
                    moveData.RightLegYDelta = moveBake.rightLegYDelta;

                    moveList.Add(moveData);
                }
            }

            targetProfile.moves = moveList.Count > 0 ? moveList.ToArray() : null;

            // ----- Actions (длина как в исходнике) -----
            List<AnimationActionClip> actionList = new List<AnimationActionClip>();

            if (actionSources != null)
            {
                for (int i = 0; i < actionSources.Length; i++)
                {
                    ActionSource src = actionSources[i];
                    if (src == null || src.clip == null)
                    {
                        continue;
                    }

                    AnimationClip prepared = DuplicateAndRetimeClip(
                        src.clip,
                        src.clip.length,
                        preparedFolder,
                        "_Action" + i);

                    float actionOffset = FindCycleOffset(prepared, anim);
                    SetClipCycleOffset(prepared, actionOffset);

                    AnimationActionClip actionData = new AnimationActionClip
                    {
                        name = src.name,
                        Clip = prepared,
                        fadeInPercent = src.fadeInPercent,
                        fadeOutPercent = src.fadeOutPercent
                    };

                    FootAndHipBakeResult actionBake = BakeFootAndHipCurves(anim, prepared);
                    actionData.LeftFootMagnet = actionBake.leftFootMagnet;
                    actionData.RightFootMagnet = actionBake.rightFootMagnet;
                    actionData.HipMaxOffset = actionBake.hipMaxOffset;
                    actionData.LeftLegYDelta = actionBake.leftLegYDelta;
                    actionData.RightLegYDelta = actionBake.rightLegYDelta;

                    actionList.Add(actionData);
                }
            }

            targetProfile.actions = actionList.Count > 0 ? actionList.ToArray() : null;

            EditorUtility.SetDirty(targetProfile);
            AssetDatabase.SaveAssets();

            UnityEngine.Debug.Log(
                "[LocomotionProfileBuilder] BuildProfile complete for '" +
                targetProfile.name + "'.");
        }

        private string ResolvePreparedFolder()
        {
            string profilePath = AssetDatabase.GetAssetPath(targetProfile);
            if (string.IsNullOrEmpty(profilePath))
            {
                UnityEngine.Debug.LogError("[LocomotionProfileBuilder] Cannot get Asset path for LocomotionProfile.");
                return null;
            }

            string dir = System.IO.Path.GetDirectoryName(profilePath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(profilePath);

            string suffix = string.IsNullOrEmpty(preparedFolderSuffix)
                ? "_Prepared"
                : preparedFolderSuffix;

            string folderName = fileName + suffix;
            string fullPath = System.IO.Path.Combine(dir, folderName).Replace("\\", "/");

            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(dir, folderName);
            }

            return fullPath;
        }

        private float ComputeAverageMoveLength()
        {
            if (moveSources == null || moveSources.Length == 0)
            {
                return 0f;
            }

            float sum = 0f;
            int count = 0;

            for (int i = 0; i < moveSources.Length; i++)
            {
                AnimationClip c = moveSources[i];
                if (c == null)
                {
                    continue;
                }

                float len = c.length;
                if (len <= 0f)
                {
                    continue;
                }

                sum += len;
                count++;
            }

            if (count == 0)
            {
                return 0f;
            }

            return sum / count;
        }

        private static AnimationClip DuplicateAndRetimeClip(
            AnimationClip source,
            float targetLength,
            string folderPath,
            string nameSuffix)
        {
            if (source == null)
            {
                return null;
            }

            if (targetLength <= 0f)
            {
                targetLength = source.length;
            }

            string srcName = source.name;
            string sanitizedFolder = folderPath.Replace("\\", "/");
            string newFileName = srcName + nameSuffix + ".anim";
            string newPath = System.IO.Path.Combine(sanitizedFolder, newFileName).Replace("\\", "/");
            string newObjectName = System.IO.Path.GetFileNameWithoutExtension(newFileName);

            AnimationClip newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newPath);
            if (newClip == null)
            {
                newClip = new AnimationClip();
                AssetDatabase.CreateAsset(newClip, newPath);
            }

            EditorUtility.CopySerialized(source, newClip);
            newClip.name = newObjectName;

            if (source.length > 0f && Mathf.Abs(source.length - targetLength) > 0.0001f)
            {
                float scale = targetLength / source.length;

                var bindings = AnimationUtility.GetCurveBindings(newClip);
                for (int i = 0; i < bindings.Length; i++)
                {
                    var b = bindings[i];
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(newClip, b);
                    if (curve == null)
                    {
                        continue;
                    }

                    var keys = curve.keys;
                    for (int k = 0; k < keys.Length; k++)
                    {
                        var key = keys[k];
                        key.time *= scale;
                        keys[k] = key;
                    }

                    curve.keys = keys;
                    newClip.SetCurve(b.path, b.type, b.propertyName, curve);
                }

                var refBindings = AnimationUtility.GetObjectReferenceCurveBindings(newClip);
                for (int i = 0; i < refBindings.Length; i++)
                {
                    var rb = refBindings[i];
                    var refCurve = AnimationUtility.GetObjectReferenceCurve(newClip, rb);
                    if (refCurve == null)
                    {
                        continue;
                    }

                    for (int k = 0; k < refCurve.Length; k++)
                    {
                        var key = refCurve[k];
                        key.time *= scale;
                        refCurve[k] = key;
                    }

                    AnimationUtility.SetObjectReferenceCurve(newClip, rb, refCurve);
                }

                var events = AnimationUtility.GetAnimationEvents(newClip);
                if (events != null && events.Length > 0)
                {
                    for (int i = 0; i < events.Length; i++)
                    {
                        events[i].time *= scale;
                    }

                    AnimationUtility.SetAnimationEvents(newClip, events);
                }

                UnityEngine.Debug.Log(
                    "[LocomotionProfileBuilder] Retime " +
                    srcName + ": " +
                    source.length.ToString("F3") + "s -> " +
                    targetLength.ToString("F3") + "s");
            }

            newClip.frameRate = source.frameRate;
            EditorUtility.SetDirty(newClip);
            AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceUpdate);

            return newClip;
        }

        private FootAndHipBakeResult BakeFootAndHipCurves(Animator animator, AnimationClip clip)
        {
            FootAndHipBakeResult result = FootAndHipBakeResult.Empty();

            if (animator == null || clip == null)
            {
                return result;
            }

            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (leftFoot == null || rightFoot == null || hips == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[LocomotionProfileBuilder] BakeFootAndHipCurves: missing humanoid bones for clip " +
                    clip.name);
                return result;
            }

            float length = clip.length;
            if (length <= 0f)
            {
                return result;
            }

            int samplesPerSec = Mathf.Max(1, curveSamplesPerSecond);
            int sampleCount = Mathf.Clamp(
                Mathf.RoundToInt(length * samplesPerSec),
                4,
                300);

            float[] leftY = new float[sampleCount];
            float[] rightY = new float[sampleCount];
            float[] hipY = new float[sampleCount];

            float minLeft = float.PositiveInfinity;
            float maxLeft = float.NegativeInfinity;
            float minRight = float.PositiveInfinity;
            float maxRight = float.NegativeInfinity;

            for (int i = 0; i < sampleCount; i++)
            {
                float tNorm = sampleCount == 1 ? 0f : (float)i / (sampleCount - 1);
                float t = length * tNorm;

                clip.SampleAnimation(animator.gameObject, t);

                float ly = leftFoot.position.y;
                float ry = rightFoot.position.y;
                float hy = hips.position.y;

                leftY[i] = ly;
                rightY[i] = ry;
                hipY[i] = hy;

                if (ly < minLeft) minLeft = ly;
                if (ly > maxLeft) maxLeft = ly;

                if (ry < minRight) minRight = ry;
                if (ry > maxRight) maxRight = ry;
            }

            result.leftLegYDelta = maxLeft - minLeft;
            result.rightLegYDelta = maxRight - minRight;

            float eps = 1e-5f;

            float tTotal = Mathf.Clamp01(totalMagnet);
            float tPartial = Mathf.Clamp01(partialMagnet);
            if (tPartial < tTotal)
            {
                float tmp = tPartial;
                tPartial = tTotal;
                tTotal = tmp;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                float tNorm = sampleCount == 1 ? 0f : (float)i / (sampleCount - 1);
                float t = length * tNorm;

                float ly = leftY[i];
                float ry = rightY[i];
                float hy = hipY[i];

                float leftMagValue = ComputeMagnetFromHeight(ly, minLeft, maxLeft, tTotal, tPartial, eps);
                float rightMagValue = ComputeMagnetFromHeight(ry, minRight, maxRight, tTotal, tPartial, eps);

                result.leftFootMagnet.AddKey(new Keyframe(t, leftMagValue));
                result.rightFootMagnet.AddKey(new Keyframe(t, rightMagValue));

                float f = Mathf.Max(hy - ly, hy - ry);
                result.hipMaxOffset.AddKey(new Keyframe(t, f));
            }

            return result;
        }

        private static float ComputeMagnetFromHeight(
            float value,
            float min,
            float max,
            float totalP,
            float partialP,
            float eps)
        {
            float range = max - min;
            if (range < eps)
            {
                return 1f;
            }

            float norm = Mathf.InverseLerp(min, max, value);

            if (norm <= totalP)
            {
                return 1f;
            }

            if (norm >= partialP)
            {
                return 0f;
            }

            float span = partialP - totalP;
            if (span < eps)
            {
                return 0f;
            }

            float k = (norm - totalP) / span;
            return 1f - k;
        }

        private float FindCycleOffset(AnimationClip clip, Animator animator)
        {
            if (clip == null || animator == null || clip.length <= 0f)
            {
                return 0f;
            }

            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            if (leftFoot == null || rightFoot == null)
            {
                return 0f;
            }

            int samples = Mathf.Clamp(
                Mathf.RoundToInt(clip.length * curveSamplesPerSecond),
                8,
                600);

            float bestScore = float.PositiveInfinity;
            float bestNormTime = 0f;

            for (int i = 0; i < samples; i++)
            {
                float tNorm = (float)i / (samples - 1);
                float t = clip.length * tNorm;

                clip.SampleAnimation(animator.gameObject, t);

                float ly = leftFoot.position.y;
                float ry = rightFoot.position.y;

                float score = (-ly) + (ry);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestNormTime = tNorm;
                }
            }

            return Mathf.Clamp01(bestNormTime);
        }

        private static void SetClipCycleOffset(AnimationClip clip, float offset)
        {
            offset = Mathf.Repeat(offset, 1f);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.cycleOffset = offset;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
        }

#endif
    }
}
