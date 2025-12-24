#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AlSo
{
    /// <summary>
    /// Простое окно в редакторе для ручного запуска бейкера.
    /// </summary>
    public class IdleActionBlendBakerWindow : EditorWindow
    {
        private AnimationClip _idleClip;
        private AnimationClip _actionClip;
        private float _fadeInSeconds = 0.1f;
        private float _fadeOutSeconds = 0.1f;

        [MenuItem("AlSo/Animation/Idle-Action Blend Baker")]
        public static void Open()
        {
            var window = GetWindow<IdleActionBlendBakerWindow>("Idle-Action Baker");
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Idle-Action Blend Baker", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _idleClip = (AnimationClip)EditorGUILayout.ObjectField("Idle Clip", _idleClip, typeof(AnimationClip), false);
            _actionClip = (AnimationClip)EditorGUILayout.ObjectField("Action Clip", _actionClip, typeof(AnimationClip), false);

            _fadeInSeconds = EditorGUILayout.FloatField("Fade In (sec)", _fadeInSeconds);
            _fadeOutSeconds = EditorGUILayout.FloatField("Fade Out (sec)", _fadeOutSeconds);

            _fadeInSeconds = Mathf.Max(0f, _fadeInSeconds);
            _fadeOutSeconds = Mathf.Max(0f, _fadeOutSeconds);

            EditorGUILayout.Space();

            GUI.enabled = (_idleClip != null && _actionClip != null);
            if (GUILayout.Button("Bake Idle-Blended Action"))
            {
                var result = IdleActionBlendBaker.BakeIdleBlendedAction(
                    _idleClip,
                    _actionClip,
                    _fadeInSeconds,
                    _fadeOutSeconds);

                if (result != null)
                {
                    UnityEngine.Debug.Log(
                        $"[IdleActionBlendBakerWindow] Baked clip: {AssetDatabase.GetAssetPath(result)}");
                    Selection.activeObject = result;
                }
            }

            GUI.enabled = true;
        }
    }

    /// <summary>
    /// Берёт idleClip и actionClip и создаёт новый клип:
    /// - в начале клипа плавный переход от позы айдла (кадр t=0 idle) к action
    ///   за fadeInSeconds;
    /// - в конце клипа плавный переход от action к позе айдла (тот же кадр t=0 idle)
    ///   за fadeOutSeconds.
    /// Внутри фейдов значения считаются ЯВНО через lerp(idle, action).
    /// </summary>
    public static class IdleActionBlendBaker
    {
        public static AnimationClip BakeIdleBlendedAction(
            AnimationClip idleClip,
            AnimationClip actionClip,
            float fadeInSeconds,
            float fadeOutSeconds)
        {
            if (idleClip == null)
            {
                UnityEngine.Debug.LogError("[IdleActionBlendBaker] idleClip is null.");
                return null;
            }

            if (actionClip == null)
            {
                UnityEngine.Debug.LogError("[IdleActionBlendBaker] actionClip is null.");
                return null;
            }

            float length = actionClip.length;
            if (length <= 0f)
            {
                UnityEngine.Debug.LogError($"[IdleActionBlendBaker] actionClip '{actionClip.name}' has zero length.");
                return null;
            }

            fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);

            if (fadeInSeconds + fadeOutSeconds > length)
            {
                float scale = length / (fadeInSeconds + fadeOutSeconds + 0.0001f);
                float oldIn = fadeInSeconds;
                float oldOut = fadeOutSeconds;

                fadeInSeconds *= scale;
                fadeOutSeconds *= scale;

                UnityEngine.Debug.LogWarning(
                    $"[IdleActionBlendBaker] fadeIn+fadeOut > clip length. " +
                    $"Rescaled: in {oldIn:F3}->{fadeInSeconds:F3}, out {oldOut:F3}->{fadeOutSeconds:F3}, length={length:F3}");
            }

            string actionPath = AssetDatabase.GetAssetPath(actionClip);
            if (string.IsNullOrEmpty(actionPath))
            {
                UnityEngine.Debug.LogError("[IdleActionBlendBaker] actionClip is not an asset on disk.");
                return null;
            }

            string dir = System.IO.Path.GetDirectoryName(actionPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(actionPath);
            string newPath = System.IO.Path.Combine(dir, fileName + "_IdleBlend.anim").Replace("\\", "/");

            // Создаём/обновляем целевой клип
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newPath);
            if (targetClip == null)
            {
                targetClip = new AnimationClip();
                targetClip.name = fileName + "_IdleBlend";
                AssetDatabase.CreateAsset(targetClip, newPath);
            }

            // Полностью копируем action в target
            EditorUtility.CopySerialized(actionClip, targetClip);

            // Кривые из idle и action
            var idleBindings = AnimationUtility.GetCurveBindings(idleClip);
            var actionBindings = AnimationUtility.GetCurveBindings(actionClip);

            // Для быстрого доступа к idle-кривым делаем словарь
            var idleCurves = new Dictionary<EditorCurveBinding, AnimationCurve>(new EditorCurveBindingComparer());
            foreach (var b in idleBindings)
            {
                var c = AnimationUtility.GetEditorCurve(idleClip, b);
                if (c != null)
                {
                    idleCurves[b] = c;
                }
            }

            float fadeInEndTime = fadeInSeconds;
            float fadeOutStartTime = length - fadeOutSeconds;

            foreach (var binding in actionBindings)
            {
                AnimationCurve actionCurve = AnimationUtility.GetEditorCurve(actionClip, binding);
                if (actionCurve == null)
                {
                    continue;
                }

                // Ищем соответствующую кривую в idle
                idleCurves.TryGetValue(binding, out var idleCurve);

                // idle-значение берём в t=0 (первый кадр)
                float idleValue = GetIdleValueAtZero(idleCurve, actionCurve);

                // Создаём новую кривую на основе ключей action
                AnimationCurve newCurve = new AnimationCurve(actionCurve.keys);

                // Гарантируем ключи в контрольных точках, чтобы лерп сработал точно
                EnsureKeyExact(ref newCurve, 0f);
                if (fadeInSeconds > 0f)
                {
                    EnsureKeyExact(ref newCurve, fadeInEndTime);
                }
                if (fadeOutSeconds > 0f)
                {
                    EnsureKeyExact(ref newCurve, fadeOutStartTime);
                }
                EnsureKeyExact(ref newCurve, length);

                // Пересчитываем значения ключей с явным lerp
                var keys = newCurve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    float t = keys[i].time;
                    float actionValue = actionCurve.Evaluate(t);
                    float finalValue;

                    if (fadeInSeconds > 0f && t >= 0f && t <= fadeInEndTime)
                    {
                        float k = fadeInSeconds > 0f ? (t / fadeInSeconds) : 1f;
                        k = Mathf.Clamp01(k);
                        finalValue = Mathf.Lerp(idleValue, actionValue, k);
                    }
                    else if (fadeOutSeconds > 0f && t >= fadeOutStartTime && t <= length)
                    {
                        float k = fadeOutSeconds > 0f ? ((t - fadeOutStartTime) / fadeOutSeconds) : 1f;
                        k = Mathf.Clamp01(k);
                        finalValue = Mathf.Lerp(actionValue, idleValue, k);
                    }
                    else
                    {
                        // В середине клипа — чистое значение action-кривой
                        finalValue = actionValue;
                    }

                    keys[i].value = finalValue;
                }

                newCurve.keys = keys;

                // Лёгкое сглаживание тангентов (не ломает наш лерп, просто убирает ступеньки)
                SmoothTangents(newCurve);

                targetClip.SetCurve(binding.path, binding.type, binding.propertyName, newCurve);
            }

            // ObjectReference-кривые просто копируем как есть
            var refBindings = AnimationUtility.GetObjectReferenceCurveBindings(actionClip);
            foreach (var rb in refBindings)
            {
                var refCurve = AnimationUtility.GetObjectReferenceCurve(actionClip, rb);
                AnimationUtility.SetObjectReferenceCurve(targetClip, rb, refCurve);
            }

            EditorUtility.SetDirty(targetClip);
            AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceUpdate);

            UnityEngine.Debug.Log(
                $"[IdleActionBlendBaker] Created/updated '{newPath}' from idle '{idleClip.name}' and action '{actionClip.name}'. " +
                $"fadeIn={fadeInSeconds:F3}, fadeOut={fadeOutSeconds:F3}");

            return targetClip;
        }

        private static float GetIdleValueAtZero(AnimationCurve idleCurve, AnimationCurve actionCurve)
        {
            if (idleCurve != null && idleCurve.keys != null && idleCurve.keys.Length > 0)
            {
                return idleCurve.Evaluate(0f);
            }

            if (actionCurve != null && actionCurve.keys != null && actionCurve.keys.Length > 0)
            {
                return actionCurve.Evaluate(0f);
            }

            return 0f;
        }

        /// <summary>
        /// Гарантирует, что в кривой есть ключ ровно в момент time.
        /// Если ключ был — оставляем время и тангенты, только не трогаем value сейчас.
        /// </summary>
        private static void EnsureKeyExact(ref AnimationCurve curve, float time)
        {
            const float eps = 1e-4f;
            var keys = curve.keys;

            for (int i = 0; i < keys.Length; i++)
            {
                if (Mathf.Abs(keys[i].time - time) <= eps)
                {
                    return;
                }
            }

            float value = curve.Evaluate(time);
            var kf = new Keyframe(time, value);
            int index = curve.AddKey(kf);
            if (index >= 0 && index < curve.keys.Length)
            {
                var k = curve.keys[index];
                curve.MoveKey(index, k);
            }
        }

        private static void SmoothTangents(AnimationCurve curve)
        {
            for (int i = 0; i < curve.keys.Length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }
        }

        /// <summary>
        /// Сравнение EditorCurveBinding по path/type/propertyName.
        /// </summary>
        private class EditorCurveBindingComparer : IEqualityComparer<EditorCurveBinding>
        {
            public bool Equals(EditorCurveBinding x, EditorCurveBinding y)
            {
                return x.path == y.path &&
                       x.type == y.type &&
                       x.propertyName == y.propertyName;
            }

            public int GetHashCode(EditorCurveBinding obj)
            {
                int hash = 17;
                hash = hash * 23 + (obj.path != null ? obj.path.GetHashCode() : 0);
                hash = hash * 23 + (obj.type != null ? obj.type.GetHashCode() : 0);
                hash = hash * 23 + (obj.propertyName != null ? obj.propertyName.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
#endif
