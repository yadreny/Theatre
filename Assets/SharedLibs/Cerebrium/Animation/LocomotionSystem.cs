using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Codice.CM.Common;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlSo
{
    public class LocomotionSystem
    {
        private enum ActionState
        {
            None,
            FadingIn,
            Playing,
            FadingOut
        }

        private readonly LocomotionProfile _profile;
        private readonly Animator _animator;

        private readonly FreedomWeightCalculator _weightCalculator;
        private readonly bool _useCycleOffsetPhase;

        private PlayableGraph _graph;

        private AnimationMixerPlayable _locomotionMixer;
        private AnimationClipPlayable[] _clipPlayables;

        private AnimationLayerMixerPlayable _layerMixer;
        private AnimationClipPlayable _actionPlayable;

        private float[] _lastWeights;
        private float[] _cycleOffsets;

        private ActionState _actionState = ActionState.None;
        private float _actionWeight;
        private float _actionFadeIn;
        private float _actionFadeOut;
        private float _actionTime;
        private float _actionDuration;

        private AnimationActionClip _currentActionData;

        // === —бленденные значени€ кривых ===
        public float CurrentLeftFootMagnet { get; private set; }
        public float CurrentRightFootMagnet { get; private set; }
        public float CurrentHipMaxOffset { get; private set; }

        public LocomotionProfile Profile => _profile;
        public float[] Weights => _lastWeights;
        public float[] CycleOffsets => _cycleOffsets;

        public bool IsActionPlaying => _actionState != ActionState.None;

        public LocomotionSystem(LocomotionProfile profile, Animator animator)
        {
            _profile = profile;
            _animator = animator;

            _useCycleOffsetPhase = false;

            _weightCalculator = new FreedomWeightCalculator(_profile);
            InitializeGraph();
        }

        private void InitializeGraph()
        {
            var clips = _profile.RuntimeClips;
            int count = clips != null ? clips.Length : 0;
            if (count == 0)
            {
                UnityEngine.Debug.LogError("[LocomotionSystem] Profile has no runtime clips.");
                return;
            }

            _graph = PlayableGraph.Create("LocomotionGraph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _locomotionMixer = AnimationMixerPlayable.Create(_graph, count, true);
            _clipPlayables = new AnimationClipPlayable[count];
            _cycleOffsets = new float[count];
            _lastWeights = new float[count];

            for (int i = 0; i < count; i++)
            {
                var clip = clips[i];
                if (clip == null)
                {
                    _cycleOffsets[i] = 0f;
                    continue;
                }

                var playable = AnimationClipPlayable.Create(_graph, clip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);

                _clipPlayables[i] = playable;
                _graph.Connect(playable, 0, _locomotionMixer, i);
                _locomotionMixer.SetInputWeight(i, 0f);

#if UNITY_EDITOR
                float co = 0f;
                try
                {
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    co = settings.cycleOffset;
                }
                catch
                {
                    co = 0f;
                }
                _cycleOffsets[i] = co;
#else
                _cycleOffsets[i] = 0f;
#endif

                if (_useCycleOffsetPhase && clip.length > 0f)
                {
                    float offsetTime = _cycleOffsets[i] * clip.length;
                    playable.SetTime(offsetTime);
                }
            }

            _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 2);
            _graph.Connect(_locomotionMixer, 0, _layerMixer, 0);
            _layerMixer.SetInputWeight(0, 1f);
            _layerMixer.SetInputWeight(1, 0f);

            var output = AnimationPlayableOutput.Create(_graph, "LocomotionOutput", _animator);
            output.SetSourcePlayable(_layerMixer);

            _graph.Play();
        }

        //public void UpdateLocomotion(Vector2 speed)
        //{
        //    if (!_graph.IsValid())
        //    {
        //        return;
        //    }

        //    var clips = _profile.RuntimeClips;
        //    if (clips == null || clips.Length == 0)
        //    {
        //        return;
        //    }

        //    float[] weights = _weightCalculator.GetWeights(speed);
        //    if (weights == null || weights.Length == 0)
        //    {
        //        return;
        //    }

        //    int mixerInputs = _locomotionMixer.GetInputCount();
        //    int count = Mathf.Min(weights.Length, mixerInputs);

        //    if (_lastWeights == null || _lastWeights.Length != weights.Length)
        //    {
        //        _lastWeights = new float[weights.Length];
        //    }

        //    for (int i = 0; i < count; i++)
        //    {
        //        float w = weights[i];
        //        _lastWeights[i] = w;
        //        _locomotionMixer.SetInputWeight(i, w);
        //    }

        //    for (int i = count; i < mixerInputs; i++)
        //    {
        //        _locomotionMixer.SetInputWeight(i, 0f);
        //    }

        //    UpdateAction(Time.deltaTime);
        //    UpdateFootAndHipFromCurves();
        //}

        // --- Ёкшены ---

        public void PerformClip(AnimationClip clip, float fadeIn = 0.1f, float fadeOut = 0.1f)
        {
            if (!_graph.IsValid())
            {
                UnityEngine.Debug.LogWarning("[LocomotionSystem] PerformClip called but graph is invalid.");
                return;
            }

            if (clip == null)
            {
                UnityEngine.Debug.LogWarning("[LocomotionSystem] PerformClip called with null clip.");
                return;
            }

            if (clip.length <= 0f)
            {
                UnityEngine.Debug.LogWarning($"[LocomotionSystem] PerformClip: clip '{clip.name}' has zero length.");
                return;
            }

            if (_actionPlayable.IsValid())
            {
                _graph.Disconnect(_layerMixer, 1);
                _actionPlayable.Destroy();
                _actionPlayable = default;
                _currentActionData = null;
            }

            _actionPlayable = AnimationClipPlayable.Create(_graph, clip);
            _actionPlayable.SetApplyFootIK(false);
            _actionPlayable.SetApplyPlayableIK(false);
            _actionPlayable.SetTime(0.0);
            _actionPlayable.SetSpeed(1.0);

            _graph.Connect(_actionPlayable, 0, _layerMixer, 1);
            _layerMixer.SetInputWeight(1, 0f);

            _actionDuration = clip.length;
            _actionFadeIn = Mathf.Max(0f, fadeIn);
            _actionFadeOut = Mathf.Max(0f, fadeOut);
            _actionTime = 0f;
            _actionWeight = 0f;

            _actionState = _actionFadeIn > 0f ? ActionState.FadingIn : ActionState.Playing;

            UnityEngine.Debug.Log(
                $"[LocomotionSystem] PerformClip: '{clip.name}', fadeIn={_actionFadeIn:F3}, fadeOut={_actionFadeOut:F3}, duration={_actionDuration:F3}.");
        }

        public void PerformAction(AnimationActionClip action)
        {
            if (action == null || action.Clip == null)
            {
                UnityEngine.Debug.LogWarning("[LocomotionSystem] PerformAction: action or clip is null.");
                return;
            }

            float len = action.Clip.length;
            if (len <= 0f)
            {
                UnityEngine.Debug.LogWarning($"[LocomotionSystem] PerformAction: clip '{action.Clip.name}' has zero length.");
                return;
            }

            float fadeIn = Mathf.Max(0f, action.fadeInPercent) * len;
            float fadeOut = Mathf.Max(0f, action.fadeOutPercent) * len;

            _currentActionData = action;
            PerformClip(action.Clip, fadeIn, fadeOut);
        }

        public void Execute(string actionName)
        {
            if (_profile == null)
            {
                UnityEngine.Debug.LogWarning("[LocomotionSystem] Execute: profile is null.");
                return;
            }

            if (string.IsNullOrEmpty(actionName))
            {
                UnityEngine.Debug.LogWarning("[LocomotionSystem] Execute: actionName is null or empty.");
                return;
            }

            var action = _profile.FindAction(actionName);
            if (action == null)
            {
                UnityEngine.Debug.LogWarning($"[LocomotionSystem] Execute: action '{actionName}' not found in profile.");
                return;
            }

            PerformAction(action);
        }

        private void UpdateAction(float dt)
        {
            if (!_actionPlayable.IsValid())
            {
                _actionState = ActionState.None;
                _layerMixer.SetInputWeight(1, 0f);
                _layerMixer.SetInputWeight(0, 1f);
                _actionWeight = 0f;
                _currentActionData = null;
                return;
            }

            if (_actionState == ActionState.None)
            {
                _layerMixer.SetInputWeight(1, 0f);
                _layerMixer.SetInputWeight(0, 1f);
                _actionWeight = 0f;
                return;
            }

            _actionTime += dt;

            switch (_actionState)
            {
                case ActionState.FadingIn:
                    if (_actionFadeIn <= 0f)
                    {
                        _actionWeight = 1f;
                        _actionState = ActionState.Playing;
                    }
                    else
                    {
                        _actionWeight += dt / _actionFadeIn;
                        if (_actionWeight >= 1f)
                        {
                            _actionWeight = 1f;
                            _actionState = ActionState.Playing;
                        }
                    }
                    break;

                case ActionState.Playing:
                    if (_actionDuration > 0f && _actionFadeOut > 0f)
                    {
                        float fadeStart = _actionDuration - _actionFadeOut;
                        if (_actionTime >= fadeStart)
                        {
                            _actionState = ActionState.FadingOut;
                        }
                    }
                    else if (_actionDuration > 0f && _actionTime >= _actionDuration)
                    {
                        _actionWeight = 0f;
                        StopAction();
                    }
                    break;

                case ActionState.FadingOut:
                    if (_actionFadeOut <= 0f)
                    {
                        _actionWeight = 0f;
                        StopAction();
                    }
                    else
                    {
                        _actionWeight -= dt / _actionFadeOut;
                        if (_actionWeight <= 0f)
                        {
                            _actionWeight = 0f;
                            StopAction();
                        }
                    }
                    break;
            }

            _actionWeight = Mathf.Clamp01(_actionWeight);

            _layerMixer.SetInputWeight(0, 1f);
            _layerMixer.SetInputWeight(1, _actionWeight);
        }

        private void StopAction()
        {
            _actionState = ActionState.None;

            if (_actionPlayable.IsValid())
            {
                _graph.Disconnect(_layerMixer, 1);
                _actionPlayable.Destroy();
                _actionPlayable = default;
            }

            _layerMixer.SetInputWeight(1, 0f);
            _layerMixer.SetInputWeight(0, 1f);
            _currentActionData = null;
            _actionWeight = 0f;
        }

        // === —читаем сбленденные значени€ кривых ===

        private void UpdateFootAndHipFromCurves()
        {
            CurrentLeftFootMagnet = 0f;
            CurrentRightFootMagnet = 0f;
            CurrentHipMaxOffset = 0f;

            var clips = _profile.RuntimeClips;
            if (clips == null || _lastWeights == null)
            {
                return;
            }

            int count = Mathf.Min(clips.Length, _clipPlayables.Length, _lastWeights.Length);
            const float eps = 1e-5f;

            float baseLeft = 0f;
            float baseRight = 0f;
            float baseHip = 0f;

            // Ѕазовый слой: idle + moves
            for (int i = 0; i < count; i++)
            {
                float w = _lastWeights[i];
                if (w <= eps)
                {
                    continue;
                }

                AnimationClip clip = clips[i];
                if (clip == null || clip.length <= 0f)
                {
                    continue;
                }

                var playable = _clipPlayables[i];
                if (!playable.IsValid())
                {
                    continue;
                }

                double time = playable.GetTime();
                float len = clip.length;
                float localTime = len > 0f
                    ? (float)(time % len)
                    : 0f;

                AnimationCurve lfCurve = null;
                AnimationCurve rfCurve = null;
                AnimationCurve hipCurve = null;

                if (i == 0)
                {
                    var idle = _profile.idle;
                    if (idle != null)
                    {
                        lfCurve = idle.LeftFootMagnet;
                        rfCurve = idle.RightFootMagnet;
                        hipCurve = idle.HipMaxOffset;
                    }
                }
                else
                {
                    int moveIndex = i - 1;
                    if (_profile.moves != null &&
                        moveIndex >= 0 &&
                        moveIndex < _profile.moves.Length)
                    {
                        var move = _profile.moves[moveIndex];
                        if (move != null)
                        {
                            lfCurve = move.LeftFootMagnet;
                            rfCurve = move.RightFootMagnet;
                            hipCurve = move.HipMaxOffset;
                        }
                    }
                }

                float lf = EvaluateCurve(lfCurve, localTime);
                float rf = EvaluateCurve(rfCurve, localTime);
                float hh = EvaluateCurve(hipCurve, localTime);

                baseLeft += w * lf;
                baseRight += w * rf;
                baseHip += w * hh;
            }

            float finalLeft = baseLeft;
            float finalRight = baseRight;
            float finalHip = baseHip;

            // Ёкшен поверх локомоушена
            if (_currentActionData != null &&
                _actionPlayable.IsValid() &&
                _currentActionData.Clip != null &&
                _currentActionData.Clip.length > 0f &&
                _actionWeight > eps)
            {
                float len = _currentActionData.Clip.length;
                double t = _actionPlayable.GetTime();
                float localTime = len > 0f
                    ? (float)(t % len)
                    : 0f;

                float actLeft = EvaluateCurve(_currentActionData.LeftFootMagnet, localTime);
                float actRight = EvaluateCurve(_currentActionData.RightFootMagnet, localTime);
                float actHip = EvaluateCurve(_currentActionData.HipMaxOffset, localTime);

                float aw = _actionWeight;

                finalLeft = Mathf.Lerp(baseLeft, actLeft, aw);
                finalRight = Mathf.Lerp(baseRight, actRight, aw);
                finalHip = Mathf.Lerp(baseHip, actHip, aw);
            }

            CurrentLeftFootMagnet = finalLeft;
            CurrentRightFootMagnet = finalRight;
            CurrentHipMaxOffset = finalHip;
        }

        private static float EvaluateCurve(AnimationCurve curve, float time)
        {
            if (curve == null || curve.length == 0)
            {
                return 0f;
            }

            return curve.Evaluate(time);
        }

        // ¬нешнему коду удобно дЄргать так
        public void EvaluateFootAndHip(out float left, out float right, out float hip)
        {
            left = CurrentLeftFootMagnet;
            right = CurrentRightFootMagnet;
            hip = CurrentHipMaxOffset;
        }

        public void Destroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        //public void UpdateLocomotion(Vector2 speed)

        public void UpdateLocomotion(Vector2 speed)
        {
            UpdateLocomotion(speed, Time.deltaTime);
        }

        public void UpdateLocomotion(Vector2 speed, float dt)
        {
            if (!_graph.IsValid())
            {
                return;
            }

            var clips = _profile.RuntimeClips;
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            float[] weights = _weightCalculator.GetWeights(speed);
            if (weights == null || weights.Length == 0)
            {
                return;
            }

            int mixerInputs = _locomotionMixer.GetInputCount();
            int count = Mathf.Min(weights.Length, mixerInputs);

            if (_lastWeights == null || _lastWeights.Length != weights.Length)
            {
                _lastWeights = new float[weights.Length];
            }

            for (int i = 0; i < count; i++)
            {
                float w = weights[i];
                _lastWeights[i] = w;
                _locomotionMixer.SetInputWeight(i, w);
            }

            for (int i = count; i < mixerInputs; i++)
            {
                _locomotionMixer.SetInputWeight(i, 0f);
            }

            UpdateAction(dt);
            UpdateFootAndHipFromCurves();
        }

        public void EvaluateGraph(float deltaTime)
        {
            if (!_graph.IsValid())
            {
                return;
            }

            // ¬ажно: даже Evaluate(0) полезен дл€ скраба (проталкивает сэмплинг).
            _graph.Evaluate(deltaTime);
        }

        /// <summary>
        /// ƒл€ Timeline/скраба: выставл€ет абсолютное врем€ фазы дл€ всех locomotion-клипов (idle+moves).
        /// Ёто нужно, чтобы при скраббинге ноги/кривые попадали в нужный кадр, даже если deltaTime=0.
        /// </summary>
        public void SetAbsoluteTime(double timeSeconds)
        {
            if (!_graph.IsValid())
            {
                return;
            }

            if (_clipPlayables == null || _clipPlayables.Length == 0)
            {
                return;
            }

            var clips = _profile.RuntimeClips;
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            int count = Mathf.Min(clips.Length, _clipPlayables.Length);

            for (int i = 0; i < count; i++)
            {
                var clip = clips[i];
                if (clip == null)
                {
                    continue;
                }

                var p = _clipPlayables[i];
                if (!p.IsValid())
                {
                    continue;
                }

                double len = clip.length;
                if (len <= 0.0)
                {
                    p.SetTime(0.0);
                    continue;
                }

                // ‘аза по кругу + (опционально) cycleOffset, если ты когда-нибудь включишь _useCycleOffsetPhase.
                double t = timeSeconds % len;

                double offset = 0.0;
                if (_useCycleOffsetPhase && _cycleOffsets != null && i >= 0 && i < _cycleOffsets.Length)
                {
                    offset = _cycleOffsets[i] * len;
                }

                p.SetTime(offset + t);
            }

            // ќпционально: если сейчас играет action-слой Ч тоже синхронизируем фазу.
            // Ёто Ќ≈ управл€ет fade state/логикой _actionTime, только ставит врем€ клипа.
            if (_currentActionData != null && _currentActionData.Clip != null && _actionPlayable.IsValid())
            {
                double len = _currentActionData.Clip.length;
                if (len > 0.0)
                {
                    double t = timeSeconds % len;
                    _actionPlayable.SetTime(t);
                }
                else
                {
                    _actionPlayable.SetTime(0.0);
                }
            }
        }
    }
}
