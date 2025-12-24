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

                targetProfile.idle.clip = idlePrepared;

                BakeFootAndHipCurves(
                    anim,
                    idlePrepared,
                    out targetProfile.idle.leftFootMagnet,
                    out targetProfile.idle.rightFootMagnet,
                    out targetProfile.idle.hipMaxOffset,
                    out targetProfile.idle.leftLegYDelta,
                    out targetProfile.idle.rightLegYDelta);
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
                        clip = prepared
                    };

                    BakeFootAndHipCurves(
                        anim,
                        prepared,
                        out moveData.leftFootMagnet,
                        out moveData.rightFootMagnet,
                        out moveData.hipMaxOffset,
                        out moveData.leftLegYDelta,
                        out moveData.rightLegYDelta);

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
                        clip = prepared,
                        fadeInPercent = src.fadeInPercent,
                        fadeOutPercent = src.fadeOutPercent
                    };

                    BakeFootAndHipCurves(
                        anim,
                        prepared,
                        out actionData.leftFootMagnet,
                        out actionData.rightFootMagnet,
                        out actionData.hipMaxOffset,
                        out actionData.leftLegYDelta,
                        out actionData.rightLegYDelta);

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

            // Копируем всё содержимое из source
            EditorUtility.CopySerialized(source, newClip);

            // ВАЖНО: выравниваем имя объекта под имя файла, чтобы Unity не предлагал "Fix object name"
            newClip.name = newObjectName;

            // --- РЕТАЙМ КРИВЫХ БЕЗ УДАЛЕНИЯ (как в старой рабочей версии) ---
            if (source.length > 0f && Mathf.Abs(source.length - targetLength) > 0.0001f)
            {
                float scale = targetLength / source.length;

                // Обычные кривые
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

                // Reference-кривые (ObjectReferenceKeyframe)
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

                // Events
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

        private void BakeFootAndHipCurves(
            Animator animator,
            AnimationClip clip,
            out AnimationCurve leftMag,
            out AnimationCurve rightMag,
            out AnimationCurve hipCurve,
            out float leftLegYDelta,
            out float rightLegYDelta)
        {
            leftMag = new AnimationCurve();
            rightMag = new AnimationCurve();
            hipCurve = new AnimationCurve();

            leftLegYDelta = 0f;
            rightLegYDelta = 0f;

            if (animator == null || clip == null)
            {
                return;
            }

            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (leftFoot == null || rightFoot == null || hips == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[LocomotionProfileBuilder] BakeFootAndHipCurves: missing humanoid bones for clip " +
                    clip.name);
                return;
            }

            float length = clip.length;
            if (length <= 0f)
            {
                return;
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

            // первый проход — снимаем высоты и ищем min/max
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

            leftLegYDelta = maxLeft - minLeft;
            rightLegYDelta = maxRight - minRight;

            float eps = 1e-5f;

            float tTotal = Mathf.Clamp01(totalMagnet);
            float tPartial = Mathf.Clamp01(partialMagnet);
            if (tPartial < tTotal)
            {
                float tmp = tPartial;
                tPartial = tTotal;
                tTotal = tmp;
            }

            // второй проход — строим кривые
            for (int i = 0; i < sampleCount; i++)
            {
                float tNorm = sampleCount == 1 ? 0f : (float)i / (sampleCount - 1);
                float t = length * tNorm;

                float ly = leftY[i];
                float ry = rightY[i];
                float hy = hipY[i];

                float leftMagValue = ComputeMagnetFromHeight(ly, minLeft, maxLeft, tTotal, tPartial, eps);
                float rightMagValue = ComputeMagnetFromHeight(ry, minRight, maxRight, tTotal, tPartial, eps);

                leftMag.AddKey(new Keyframe(t, leftMagValue));
                rightMag.AddKey(new Keyframe(t, rightMagValue));

                float f = Mathf.Max(hy - ly, hy - ry);
                hipCurve.AddKey(new Keyframe(t, f));
            }
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
                // нога почти не двигается по высоте — считаем, что всегда на земле
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

            float k = (norm - totalP) / span; // 0..1
            return 1f - k;                    // 1 -> 0
        }

        /// <summary>
        /// Ищем опорный кадр для cycleOffset:
        /// левая нога максимально низко, правая максимально высоко.
        /// Возвращает нормализованное время 0..1.
        /// </summary>
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

                float ly = leftFoot.position.y;  // хотим ниже
                float ry = rightFoot.position.y; // хотим выше

                float score = (-ly) + (ry);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestNormTime = tNorm;
                }
            }

            return Mathf.Clamp01(bestNormTime);
        }

        /// <summary>
        /// Применяем cycleOffset к импортным настройкам клипа.
        /// </summary>
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
