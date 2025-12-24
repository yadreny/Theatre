#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace AlSo
{
    /// <summary>
    /// Бейкер трёх кривых в клипы:
    ///  - LeftFootMagnet  (0..1)
    ///  - RightFootMagnet (0..1)
    ///  - HipMaxOffset    (float, в юнитах)
    ///
    /// Алгоритм:
    ///  1) На временном клон-объекте с тем же humanoid-аватаром сэмплируем клип.
    ///  2) Для каждой ноги считаем minY / maxY по всем сэмплам.
    ///  3) Для каждого кадра нормализуем высоту ступни:
    ///         hNorm = (Y - minY) / (maxY - minY)
    ///     и превращаем в магнит по схеме:
    ///         h <= total   -> 1
    ///         h >= partial -> 0
    ///         total < h < partial -> 1 - InverseLerp(total, partial, h)
    ///  4) HipMaxOffset(t) = hipsY(t) - min(leftFootY(t), rightFootY(t)).
    ///  5) Пишем кривые в сам клип как Animator-параметры.
    /// </summary>
    public static class FootMagnetCurveBaker
    {
        public const string DefaultLeftFootMagnetName = "LeftFootMagnet";
        public const string DefaultRightFootMagnetName = "RightFootMagnet";
        public const string DefaultHipMaxOffsetName = "HipMaxOffset";

        /// <summary>
        /// Бейк одного клипа.
        /// </summary>
        public static void BakeSingle(
            AnimationClip clip,
            Animator referenceAnimator,
            int samplesPerSecond,
            float totalMagnetPercent,
            float partialMagnetPercent,
            string leftFootMagnetName = DefaultLeftFootMagnetName,
            string rightFootMagnetName = DefaultRightFootMagnetName,
            string hipMaxOffsetName = DefaultHipMaxOffsetName)
        {
            if (clip == null)
            {
                UnityEngine.Debug.LogError("[FootMagnetCurveBaker] clip is null.");
                return;
            }

            if (referenceAnimator == null)
            {
                UnityEngine.Debug.LogError("[FootMagnetCurveBaker] referenceAnimator is null.");
                return;
            }

            if (!referenceAnimator.isHuman || referenceAnimator.avatar == null || !referenceAnimator.avatar.isValid)
            {
                UnityEngine.Debug.LogError("[FootMagnetCurveBaker] referenceAnimator must be a valid humanoid avatar.");
                return;
            }

            float length = clip.length;
            if (length <= 0f)
            {
                UnityEngine.Debug.LogError($"[FootMagnetCurveBaker] clip '{clip.name}' has zero length.");
                return;
            }

            // Нормализуем проценты и гарантируем total <= partial
            totalMagnetPercent = Mathf.Clamp01(totalMagnetPercent);
            partialMagnetPercent = Mathf.Clamp01(partialMagnetPercent);

            if (partialMagnetPercent < totalMagnetPercent)
            {
                float tmp = totalMagnetPercent;
                totalMagnetPercent = partialMagnetPercent;
                partialMagnetPercent = tmp;
            }

            samplesPerSecond = Mathf.Max(10, samplesPerSecond);
            float dt = 1f / samplesPerSecond;
            int totalSamples = Mathf.CeilToInt(length * samplesPerSecond) + 1;

            // Временный клон персонажа, чтобы не трогать реального
            GameObject tempGO = Object.Instantiate(referenceAnimator.gameObject);
            tempGO.name = referenceAnimator.gameObject.name + "_FootMagnetBaker_TMP";

            Animator tempAnimator = tempGO.GetComponent<Animator>();
            if (tempAnimator == null)
            {
                UnityEngine.Debug.LogError("[FootMagnetCurveBaker] referenceAnimator GameObject has no Animator.");
                Object.DestroyImmediate(tempGO);
                return;
            }

            tempAnimator.runtimeAnimatorController = null; // чтобы контроллер не анимировал
            tempAnimator.avatar = referenceAnimator.avatar;

            Transform leftFoot = tempAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = tempAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            Transform hips = tempAnimator.GetBoneTransform(HumanBodyBones.Hips);

            if (leftFoot == null || rightFoot == null || hips == null)
            {
                UnityEngine.Debug.LogError("[FootMagnetCurveBaker] Cannot find humanoid LeftFoot/RightFoot/Hips bones.");
                Object.DestroyImmediate(tempGO);
                return;
            }

            var leftY = new float[totalSamples];
            var rightY = new float[totalSamples];
            var hipY = new float[totalSamples];

            // 1) Сэмплируем всё
            for (int i = 0; i < totalSamples; i++)
            {
                float t = Mathf.Min(i * dt, length);
                clip.SampleAnimation(tempGO, t);

                leftY[i] = leftFoot.position.y;
                rightY[i] = rightFoot.position.y;
                hipY[i] = hips.position.y;
            }

            // 2) Min/Max для каждой ноги
            float leftMinY = float.MaxValue;
            float leftMaxY = float.MinValue;
            float rightMinY = float.MaxValue;
            float rightMaxY = float.MinValue;

            for (int i = 0; i < totalSamples; i++)
            {
                float ly = leftY[i];
                float ry = rightY[i];

                if (ly < leftMinY) leftMinY = ly;
                if (ly > leftMaxY) leftMaxY = ly;

                if (ry < rightMinY) rightMinY = ry;
                if (ry > rightMaxY) rightMaxY = ry;
            }

            // защита от деления на ноль
            if (Mathf.Abs(leftMaxY - leftMinY) < 1e-5f)
            {
                leftMaxY = leftMinY + 1e-5f;
            }

            if (Mathf.Abs(rightMaxY - rightMinY) < 1e-5f)
            {
                rightMaxY = rightMinY + 1e-5f;
            }

            // 3) Строим кривые
            AnimationCurve leftCurve = new AnimationCurve();
            AnimationCurve rightCurve = new AnimationCurve();
            AnimationCurve hipCurve = new AnimationCurve();

            for (int i = 0; i < totalSamples; i++)
            {
                float t = Mathf.Min(i * dt, length + 1e-5f);

                // нормализованная высота: 0 = низ позиции ноги, 1 = верх
                float hNormL = Mathf.InverseLerp(leftMinY, leftMaxY, leftY[i]);
                float hNormR = Mathf.InverseLerp(rightMinY, rightMaxY, rightY[i]);

                float weightL = ComputeMagnetWeight(hNormL, totalMagnetPercent, partialMagnetPercent);
                float weightR = ComputeMagnetWeight(hNormR, totalMagnetPercent, partialMagnetPercent);

                leftCurve.AddKey(new Keyframe(t, weightL));
                rightCurve.AddKey(new Keyframe(t, weightR));

                float footMinY = Mathf.Min(leftY[i], rightY[i]);
                float hipOffset = hipY[i] - footMinY; // насколько таз выше нижней ноги

                hipCurve.AddKey(new Keyframe(t, hipOffset));
            }

            SmoothCurve(leftCurve);
            SmoothCurve(rightCurve);
            SmoothCurve(hipCurve);

            // 4) Пишем кривые как Animator-параметры
            var leftBinding = new EditorCurveBinding
            {
                path = "",
                type = typeof(Animator),
                propertyName = leftFootMagnetName
            };

            var rightBinding = new EditorCurveBinding
            {
                path = "",
                type = typeof(Animator),
                propertyName = rightFootMagnetName
            };

            var hipBinding = new EditorCurveBinding
            {
                path = "",
                type = typeof(Animator),
                propertyName = hipMaxOffsetName
            };

            AnimationUtility.SetEditorCurve(clip, leftBinding, leftCurve);
            AnimationUtility.SetEditorCurve(clip, rightBinding, rightCurve);
            AnimationUtility.SetEditorCurve(clip, hipBinding, hipCurve);

            EditorUtility.SetDirty(clip);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(clip), ImportAssetOptions.ForceUpdate);

            Object.DestroyImmediate(tempGO);

            UnityEngine.Debug.Log(
                $"[FootMagnetCurveBaker] Baked for '{clip.name}': " +
                $"Total={totalMagnetPercent * 100f:F1}%, Partial={partialMagnetPercent * 100f:F1}%, " +
                $"samples={samplesPerSecond}");
        }

        /// <summary>
        /// Бейк списка клипов.
        /// </summary>
        public static void BakeMultiple(
            IList<AnimationClip> clips,
            Animator referenceAnimator,
            int samplesPerSecond,
            float totalMagnetPercent,
            float partialMagnetPercent,
            string leftFootMagnetName = DefaultLeftFootMagnetName,
            string rightFootMagnetName = DefaultRightFootMagnetName,
            string hipMaxOffsetName = DefaultHipMaxOffsetName)
        {
            if (clips == null || clips.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[FootMagnetCurveBaker] No clips to bake.");
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                if (clip == null)
                {
                    continue;
                }

                BakeSingle(
                    clip,
                    referenceAnimator,
                    samplesPerSecond,
                    totalMagnetPercent,
                    partialMagnetPercent,
                    leftFootMagnetName,
                    rightFootMagnetName,
                    hipMaxOffsetName);
            }
        }

        /// <summary>
        /// hNorm в [0..1], total и partial тоже в [0..1].
        ///  h <= total        -> 1
        ///  h >= partial      -> 0
        ///  total < h < part  -> плавный спад 1→0
        /// </summary>
        private static float ComputeMagnetWeight(float hNorm, float total, float partial)
        {
            if (hNorm <= total)
            {
                return 1f;
            }

            if (hNorm >= partial)
            {
                return 0f;
            }

            // t = 0 при h = total, t = 1 при h = partial
            float t = Mathf.InverseLerp(total, partial, hNorm);
            return 1f - t; // 1 -> 0
        }

        private static void SmoothCurve(AnimationCurve curve)
        {
            if (curve == null)
            {
                return;
            }

            for (int i = 0; i < curve.keys.Length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }
        }
    }

    /// <summary>
    /// EditorWindow с поддержкой списка клипов.
    /// </summary>
    public class FootMagnetCurveBakerWindow : EditorWindow
    {
        private Animator _referenceAnimator;

        private int _samplesPerSecond = 60;
        private float _totalMagnetPercent = 0.05f;  // 5%
        private float _partialMagnetPercent = 0.20f;  // 20%

        private string _leftFootMagnetName = FootMagnetCurveBaker.DefaultLeftFootMagnetName;
        private string _rightFootMagnetName = FootMagnetCurveBaker.DefaultRightFootMagnetName;
        private string _hipMaxOffsetName = FootMagnetCurveBaker.DefaultHipMaxOffsetName;

        private readonly List<AnimationClip> _clips = new List<AnimationClip>();

        [MenuItem("AlSo/Animation/Foot Magnet Curve Baker")]
        public static void Open()
        {
            var w = GetWindow<FootMagnetCurveBakerWindow>("Foot Magnet Baker");
            w.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Foot Magnet Curve Baker", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _referenceAnimator = (Animator)EditorGUILayout.ObjectField(
                "Reference Animator (Humanoid)", _referenceAnimator, typeof(Animator), true);

            _samplesPerSecond = EditorGUILayout.IntField("Samples per second", _samplesPerSecond);

            _totalMagnetPercent = EditorGUILayout.Slider("TotalFootMagnet (%)",
                _totalMagnetPercent * 100f, 0f, 100f) / 100f;

            _partialMagnetPercent = EditorGUILayout.Slider("PartialFootMagnet (%)",
                _partialMagnetPercent * 100f, 0f, 100f) / 100f;

            _leftFootMagnetName = EditorGUILayout.TextField("LeftFootMagnet param", _leftFootMagnetName);
            _rightFootMagnetName = EditorGUILayout.TextField("RightFootMagnet param", _rightFootMagnetName);
            _hipMaxOffsetName = EditorGUILayout.TextField("HipMaxOffset param", _hipMaxOffsetName);

            EditorGUILayout.HelpBox(
                "Ниже TotalFootMagnet ступня всегда прилипает (вес = 1).\n" +
                "Между Total и Partial – плавный спад 1 → 0.\n" +
                "Выше Partial – магнит выключен.\n\n" +
                "Параллельно пишется HipMaxOffset(t) – расстояние таза от нижней ноги по Y.",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawClipsSection();
            EditorGUILayout.Space();

            bool canBakeAll = _clips.Count > 0 && _referenceAnimator != null;

            GUI.enabled = canBakeAll;
            if (GUILayout.Button("Bake All Clips"))
            {
                FootMagnetCurveBaker.BakeMultiple(
                    _clips,
                    _referenceAnimator,
                    _samplesPerSecond,
                    _totalMagnetPercent,
                    _partialMagnetPercent,
                    _leftFootMagnetName,
                    _rightFootMagnetName,
                    _hipMaxOffsetName);
            }
            GUI.enabled = true;
        }

        private void DrawClipsSection()
        {
            GUILayout.Label("Clips", EditorStyles.boldLabel);

            if (GUILayout.Button("Use selected clips from Project"))
            {
                _clips.Clear();
                var selection = Selection.objects;
                for (int i = 0; i < selection.Length; i++)
                {
                    if (selection[i] is AnimationClip clip)
                    {
                        _clips.Add(clip);
                    }
                }

                UnityEngine.Debug.Log($"[FootMagnetCurveBakerWindow] Collected {_clips.Count} clips from selection.");
            }

            EditorGUILayout.LabelField($"Clips in list: {_clips.Count}");

            int removeIndex = -1;

            for (int i = 0; i < _clips.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _clips[i] = (AnimationClip)EditorGUILayout.ObjectField(
                    $"[{i}]", _clips[i], typeof(AnimationClip), false);

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0 && removeIndex < _clips.Count)
            {
                _clips.RemoveAt(removeIndex);
            }

            if (GUILayout.Button("Add empty slot"))
            {
                _clips.Add(null);
            }

            if (GUILayout.Button("Clear list"))
            {
                _clips.Clear();
            }
        }
    }
}
#endif
