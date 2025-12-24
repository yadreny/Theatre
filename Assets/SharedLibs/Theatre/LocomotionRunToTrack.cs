using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    [TrackColor(0.35f, 0.75f, 0.95f)]
    [TrackClipType(typeof(LocomotionRunToClip))]
    [TrackBindingType(typeof(LocomotionProfileTest))]
    public class LocomotionRunToTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<LocomotionRunToMixerBehaviour>.Create(graph, inputCount);
        }
    }

    [Serializable]
    public class LocomotionRunToClip : PlayableAsset, ITimelineClipAsset
    {
        public ExposedReference<Transform> from;
        public ExposedReference<Transform> to;

        [Header("Drive transform")]
        public bool drivePosition = true;
        public bool driveRotation = false;

        [Header("Locomotion speed mapping")]
        [Tooltip("Сколько м/с соответствует скорости 1.0 в Locomotion (для нормализации).")]
        public float fullSpeedMetersPerSecond = 2.0f;

        [Tooltip("Множитель на итоговый speed (после нормализации).")]
        public float speedMultiplier = 1.0f;

        [Header("Timing")]
        public AnimationCurve normalizedTimeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LocomotionRunToBehaviour>.Create(graph);
            var b = playable.GetBehaviour();

            var r = graph.GetResolver();
            b.From = from.Resolve(r);
            b.To = to.Resolve(r);

            b.DrivePosition = drivePosition;
            b.DriveRotation = driveRotation;

            b.FullSpeedMps = Mathf.Max(0.0001f, fullSpeedMetersPerSecond);
            b.SpeedMultiplier = speedMultiplier;

            b.Curve = normalizedTimeCurve;

            return playable;
        }
    }

    public class LocomotionRunToBehaviour : PlayableBehaviour
    {
        public Transform From;
        public Transform To;

        public bool DrivePosition;
        public bool DriveRotation;

        public float FullSpeedMps;
        public float SpeedMultiplier;

        public AnimationCurve Curve;
    }

    public class LocomotionRunToMixerBehaviour : PlayableBehaviour
    {
        private bool _cached;
        private Vector3 _basePos;
        private Quaternion _baseRot;

        // FIX: чтобы на WrapMode=Hold (и вообще когда playhead стоит на месте) не “шёл на месте”
        private bool _hasPrevTime;
        private double _prevTrackTime;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var locomotionTest = playerData as LocomotionProfileTest;
            if (locomotionTest == null)
            {
                return;
            }

            // Важно для скраба: гарантируем наличие LocomotionSystem в Edit Mode
            locomotionTest.EnsureLocomotionCreated();

            var tr = locomotionTest.transform;

            if (!_cached)
            {
                _cached = true;
                _basePos = tr.position;
                _baseRot = tr.rotation;
            }

            int inputCount = playable.GetInputCount();
            const float eps = 1e-6f;

            float sumW = 0f;

            Vector3 posAcc = Vector3.zero;
            bool anyPos = false;

            Quaternion rotAcc = Quaternion.identity;
            bool anyRot = false;
            float rotWAcc = 0f;

            Vector2 speedAcc = Vector2.zero;
            bool anySpeed = false;

            double trackTime = playable.GetTime(); // время Timeline

            for (int i = 0; i < inputCount; i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= eps)
                    continue;

                var input = playable.GetInput(i);
                if (!input.IsValid() || input.GetPlayableType() != typeof(LocomotionRunToBehaviour))
                    continue;

                var sp = (ScriptPlayable<LocomotionRunToBehaviour>)input;
                var b = sp.GetBehaviour();
                if (b == null || b.To == null)
                    continue;

                double dur = sp.GetDuration();
                double t = sp.GetTime();

                float nt = (dur > eps) ? (float)(t / dur) : 1f;
                nt = Mathf.Clamp01(nt);

                if (b.Curve != null && b.Curve.length > 0)
                    nt = Mathf.Clamp01(b.Curve.Evaluate(nt));

                Vector3 fromPos = b.From != null ? b.From.position : _basePos;
                Quaternion fromRot = b.From != null ? b.From.rotation : _baseRot;

                Vector3 toPos = b.To.position;
                Quaternion toRot = b.To.rotation;

                // --- pose ---
                Vector3 p = Vector3.LerpUnclamped(fromPos, toPos, nt);
                Quaternion r = Quaternion.SlerpUnclamped(fromRot, toRot, nt);

                if (b.DrivePosition)
                {
                    posAcc += p * w;
                    anyPos = true;
                }

                if (b.DriveRotation)
                {
                    float newRotW = rotWAcc + w;
                    float k = (newRotW > eps) ? (w / newRotW) : 1f;

                    if (!anyRot)
                    {
                        rotAcc = r;
                        anyRot = true;
                        rotWAcc = w;
                    }
                    else
                    {
                        rotAcc = Quaternion.Slerp(rotAcc, r, k);
                        rotWAcc = newRotW;
                    }
                }

                // --- speed from trajectory derivative (scrub-friendly) ---
                Vector2 speed = ComputeSpeedVector2(tr, fromPos, toPos, sp, b);

                speedAcc += speed * w;
                anySpeed = true;

                sumW += w;
            }

            // Если нет активных клипов — вернём позу и отдадим 0-скорость
            if (sumW <= eps)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // Откатываем только если реально ушли в начало (иначе мешает Hold’у)
                    if (trackTime <= 0.0001)
                    {
                        tr.position = _basePos;
                        tr.rotation = _baseRot;
                    }
                }
#endif

                var loco0 = locomotionTest.Locomotion;
                if (loco0 != null)
                {
                    loco0.SetAbsoluteTime(trackTime);
                    loco0.UpdateLocomotion(Vector2.zero);

#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        loco0.EvaluateGraph(0f);
                    }
#endif
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    _prevTrackTime = trackTime;
                    _hasPrevTime = true;
                }
#endif
                return;
            }

            float inv = 1f / sumW;

            if (anyPos)
            {
                float baseW = Mathf.Clamp01(1f - sumW);
                Vector3 finalPos = (posAcc * inv) * sumW + _basePos * baseW;
                tr.position = finalPos;
            }

            if (anyRot)
            {
                Quaternion finalRot = Quaternion.Slerp(_baseRot, rotAcc, sumW);
                tr.rotation = finalRot;
            }

            Vector2 finalSpeed = anySpeed ? (speedAcc * inv) : Vector2.zero;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // FIX: если playhead стоит на месте (Hold / пауза / просто не проигрываем),
                // скорость = 0, чтобы ноги не "шли на месте".
                if (_hasPrevTime && Math.Abs(trackTime - _prevTrackTime) < 1e-9)
                {
                    finalSpeed = Vector2.zero;
                }

                _prevTrackTime = trackTime;
                _hasPrevTime = true;
            }
#endif

            var loco = locomotionTest.Locomotion;
            if (loco != null)
            {
                // ВАЖНО: сначала выставляем время клипов, потом UpdateLocomotion (он сэмплит кривые ног/hip)
                loco.SetAbsoluteTime(trackTime);
                loco.UpdateLocomotion(finalSpeed);

#if UNITY_EDITOR
                // В Edit Mode Unity сама не тикает твой отдельный PlayableGraph — проталкиваем руками
                if (!Application.isPlaying)
                {
                    loco.EvaluateGraph(0f);
                }
#endif
            }
        }

        private static Vector2 ComputeSpeedVector2(
            Transform character,
            Vector3 fromPos,
            Vector3 toPos,
            ScriptPlayable<LocomotionRunToBehaviour> clipPlayable,
            LocomotionRunToBehaviour b)
        {
            const float eps = 1e-6f;

            double durD = clipPlayable.GetDuration();
            float dur = (float)durD;
            float t = (float)clipPlayable.GetTime();

            if (dur <= eps)
                return Vector2.zero;

            // стабильный дифференциал даже при scrubbing (deltaTime может быть 0)
            float h = Mathf.Min(1f / 60f, dur * 0.1f);
            if (h <= eps)
                h = 1f / 60f;

            float t0 = Mathf.Clamp(t - h, 0f, dur);
            float t1 = Mathf.Clamp(t + h, 0f, dur);

            float nt0 = t0 / dur;
            float nt1 = t1 / dur;

            if (b.Curve != null && b.Curve.length > 0)
            {
                nt0 = Mathf.Clamp01(b.Curve.Evaluate(nt0));
                nt1 = Mathf.Clamp01(b.Curve.Evaluate(nt1));
            }

            Vector3 p0 = Vector3.LerpUnclamped(fromPos, toPos, nt0);
            Vector3 p1 = Vector3.LerpUnclamped(fromPos, toPos, nt1);

            float dt = Mathf.Max(eps, (t1 - t0));
            Vector3 vWorld = (p1 - p0) / dt;

            // в локаль персонажа → Vector2(x,z)
            Vector3 vLocal3 = character.InverseTransformDirection(vWorld);
            Vector2 vLocal = new Vector2(vLocal3.x, vLocal3.z);

            float worldPlanarSpeed = new Vector2(vWorld.x, vWorld.z).magnitude;
            float mag01 = Mathf.Clamp01(worldPlanarSpeed / Mathf.Max(0.0001f, b.FullSpeedMps));

            if (vLocal.sqrMagnitude <= 1e-10f)
                return Vector2.zero;

            Vector2 dir = vLocal.normalized;
            return dir * (mag01 * b.SpeedMultiplier);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            _cached = false;
            _hasPrevTime = false;
        }
    }
}
