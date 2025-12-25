using Sirenix.OdinInspector;
using UnityEngine;

namespace AlSo
{
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    [RequireComponent(typeof(Animator))]
    public class LocomotionProfileTest : SerializedMonoBehaviour
    {
        [Header("Profile")]
        [InlineEditor]
        public LocomotionProfile profile;

        [Header("Input")]
        [Tooltip("Если true, берём скорость из WASD/Arrow (Horizontal/Vertical).")]
        public bool useInput = true;

        [Tooltip("Если true и useInput == false, используем модуль скорости и угол вместо X/Z.")]
        public bool usePolarInput = false;

        [Header("Manual speed (when useInput == false & usePolarInput == false)")]
        [PropertyRange(-2f, 2f)]
        [LabelText("Speed X")]
        [ShowIf(nameof(ShowCartesianSpeedFields))]
        public float speedX;

        [PropertyRange(-2f, 2f)]
        [LabelText("Speed Z")]
        [ShowIf(nameof(ShowCartesianSpeedFields))]
        public float speedZ;

        [Header("Polar speed (when useInput == false & usePolarInput == true)")]
        [LabelText("Speed Magnitude")]
        [PropertyRange(0f, 2f)]
        [ShowIf(nameof(ShowPolarSpeedFields))]
        public float speedMagnitude = 1f;

        [LabelText("Angle (deg, 0..360)")]
        [PropertyRange(0f, 720)]
        [ShowIf(nameof(ShowPolarSpeedFields))]
        public float speedAngleDeg = 0f;

        [ReadOnly]
        [LabelText("Debug Speed (local X,Z)")]
        public Vector2 debugSpeed;

        [Header("Orientation")]
        [Tooltip("За сколько секунд в среднем доворачиваемся к desired-ориентации (меньше = резче).")]
        [Min(0f)]
        public float orientationBlendSeconds = 0.15f;

        [Header("Gizmo")]
        [Tooltip("Рисовать ли гизмо-направление скорости.")]
        public bool drawGizmo = true;

        [Tooltip("Множитель длины стрелки для визуализации.")]
        public float gizmoScale = 1.0f;

        [Header("Unity BlendTree comparison")]
        [Tooltip("Аниматор, на котором крутится Unity BlendTree (для сравнения).")]
        public Animator unityBlendTreeAnimator;

        [Tooltip("Имя параметра X в Unity BlendTree.")]
        public string unitySpeedXParam = "SpeedX";

        [Tooltip("Имя параметра Z в Unity BlendTree.")]
        public string unitySpeedZParam = "SpeedZ";

        [Header("Actions")]
        [Tooltip("Клип атаки, который будет запускаться по нажатию Q.")]
        public AnimationClip attackClip;

        private Animator _animator;
        private LocomotionSystem _locomotion;

        public LocomotionSystem Locomotion => _locomotion;

        [ShowInInspector, ReadOnly]
        public bool TimelineDriven { get; private set; }

        // ===== Timeline: ориентация (не одна цель, а вычисленное направление) =====

        [ShowInInspector, ReadOnly]
        public Transform OrientationDebugTarget { get; private set; }

        [ShowInInspector, ReadOnly]
        public Vector3 OrientationForwardWorldPlanar { get; private set; }

        [ShowInInspector, ReadOnly]
        public float OrientationWeight01 { get; private set; }

        // ===== Timeline: absolute (world) planar velocity =====

        [ShowInInspector, ReadOnly]
        public Vector3 TimelineWorldVelocityPlanar { get; private set; }

        [ShowInInspector, ReadOnly]
        public float TimelineWorldVelocityWeight01 { get; private set; }

        private bool ShowCartesianSpeedFields => !useInput && !usePolarInput;
        private bool ShowPolarSpeedFields => !useInput && usePolarInput;

        private void Awake()
        {
            _locomotion = null;
            _animator = GetComponent<Animator>();

            if (profile != null)
            {
                _locomotion = profile.CreateLocomotion(_animator);
            }
            else
            {
                UnityEngine.Debug.LogError("[LocomotionProfileTest] Profile is not assigned.");
            }
        }

        private void Update()
        {
            if (_locomotion == null)
                return;

            if (TimelineDriven)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            Vector2 speed;

            if (useInput)
            {
                float x = Input.GetAxisRaw("Horizontal");
                float z = Input.GetAxisRaw("Vertical");
                speed = new Vector2(x, z);
            }
            else if (usePolarInput)
            {
                float mag = speedMagnitude;
                float angleRad = speedAngleDeg * Mathf.Deg2Rad;

                float x = Mathf.Sin(angleRad) * mag;
                float z = Mathf.Cos(angleRad) * mag;

                speed = new Vector2(x, z);
            }
            else
            {
                speed = new Vector2(speedX, speedZ);
            }

            debugSpeed = speed;

            _locomotion.UpdateLocomotion(speed);

            if (unityBlendTreeAnimator != null)
            {
                if (!string.IsNullOrEmpty(unitySpeedXParam))
                    unityBlendTreeAnimator.SetFloat(unitySpeedXParam, speed.x);

                if (!string.IsNullOrEmpty(unitySpeedZParam))
                    unityBlendTreeAnimator.SetFloat(unitySpeedZParam, speed.y);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (attackClip != null)
                {
                    _locomotion.PerformClip(attackClip, 0.1f, 0.1f);
                    UnityEngine.Debug.Log("[LocomotionProfileTest] Q pressed: PerformClip(attackClip).");

                    if (unityBlendTreeAnimator != null)
                        unityBlendTreeAnimator.SetTrigger("Fire");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[LocomotionProfileTest] Q pressed, but attackClip is not assigned.");
                }
            }
        }

        private void OnDestroy()
        {
            if (_locomotion != null)
            {
                _locomotion.Destroy();
                _locomotion = null;
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmo)
                return;

            if (!Application.isPlaying)
                return;

            Vector3 v = new Vector3(debugSpeed.x, 0f, debugSpeed.y);
            if (v.sqrMagnitude < 0.0001f)
                return;

            Vector3 origin = transform.position;
            Vector3 dir = v * gizmoScale;
            Vector3 end = origin + dir;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, end);

            const float headLength = 0.25f;
            const float headAngle = 20f;

            Vector3 dirNorm = dir.normalized;
            if (dirNorm.sqrMagnitude > 0.0001f)
            {
                Quaternion rotLeft = Quaternion.AngleAxis(headAngle, Vector3.up);
                Quaternion rotRight = Quaternion.AngleAxis(-headAngle, Vector3.up);

                Vector3 left = rotLeft * (-dirNorm);
                Vector3 right = rotRight * (-dirNorm);

                Gizmos.DrawLine(end, end + left * headLength * gizmoScale);
                Gizmos.DrawLine(end, end + right * headLength * gizmoScale);
            }
        }

        // ==== Timeline API ====

        public void SetTimelineDriven(bool driven)
        {
            TimelineDriven = driven;
        }

        // Ориентация: трек вычисляет планарный forward (мировой) и weight.
        public void SetOrientationForward(Vector3 forwardWorldPlanar, float weight01, Transform debugTarget)
        {
            forwardWorldPlanar.y = 0f;

            OrientationDebugTarget = debugTarget;

            if (forwardWorldPlanar.sqrMagnitude > 1e-8f)
            {
                OrientationForwardWorldPlanar = forwardWorldPlanar.normalized;
            }
            else
            {
                OrientationForwardWorldPlanar = Vector3.zero;
            }

            OrientationWeight01 = Mathf.Clamp01(weight01);
        }

        public void ClearOrientation()
        {
            OrientationDebugTarget = null;
            OrientationForwardWorldPlanar = Vector3.zero;
            OrientationWeight01 = 0f;
        }

        // world velocity: миксер RunTo/StandAt задаёт абсолютную планарную скорость
        public void SetTimelineWorldVelocity(Vector3 worldVelocityPlanar, float weight01)
        {
            worldVelocityPlanar.y = 0f;
            TimelineWorldVelocityPlanar = worldVelocityPlanar;
            TimelineWorldVelocityWeight01 = Mathf.Clamp01(weight01);
        }

        public void ClearTimelineWorldVelocity()
        {
            TimelineWorldVelocityPlanar = Vector3.zero;
            TimelineWorldVelocityWeight01 = 0f;
        }

        // перевод world planar velocity -> local XZ для графа (LocomotionSystem)
        public Vector2 GetTimelineLocalSpeedForGraph()
        {
            Vector3 vWorld = TimelineWorldVelocityPlanar * Mathf.Clamp01(TimelineWorldVelocityWeight01);
            Vector3 vLocal3 = transform.InverseTransformDirection(vWorld);
            return new Vector2(vLocal3.x, vLocal3.z);
        }

        // Применяем ориентацию (если есть), с плавным поворотом в PlayMode.
        // В Edit Mode / Scrub — dtSeconds обычно 0, тогда делаем детерминированный “бленд” от baseRotation.
        public bool ApplyTimelineOrientation(Quaternion baseRotation, float dtSeconds)
        {
            const float eps = 1e-6f;

            float w = OrientationWeight01;
            if (w <= eps)
            {
                return false;
            }

            Vector3 fwd = OrientationForwardWorldPlanar;
            if (fwd.sqrMagnitude <= 1e-8f)
            {
                return false;
            }

            Quaternion desired = Quaternion.LookRotation(fwd.normalized, Vector3.up);

            // EditMode/Scrub: dt=0 -> не накапливаем, а просто “задаём позу” через вес.
            if (dtSeconds <= 0f)
            {
                transform.rotation = Quaternion.Slerp(baseRotation, desired, w);
                return true;
            }

            // PlayMode: плавное приближение к desired + сила от weight.
            float blend = Mathf.Max(0.0001f, orientationBlendSeconds);
            float alpha = 1f - Mathf.Exp(-dtSeconds / blend);
            float k = Mathf.Clamp01(alpha * w);

            transform.rotation = Quaternion.Slerp(transform.rotation, desired, k);
            return true;
        }

        public void PerformAction(AnimationActionClipData actionData)
        {
            EnsureLocomotionCreated();

            if (_locomotion == null)
            {
                UnityEngine.Debug.LogWarning("[LocomotionProfileTest] PerformAction: locomotion is null.");
                return;
            }

            if (actionData == null || actionData.Clip == null)
            {
                UnityEngine.Debug.LogWarning("[LocomotionProfileTest] PerformAction: actionData or Clip is null.");
                return;
            }

            debugSpeed = Vector2.zero;
            _locomotion.UpdateLocomotion(Vector2.zero);

            float len = actionData.Clip.length;
            float fadeIn = Mathf.Clamp01(actionData.FadeInPercent) * len;
            float fadeOut = Mathf.Clamp01(actionData.FadeOutPercent) * len;

            _locomotion.PerformClip(actionData.Clip, fadeIn, fadeOut);
        }

        private void OnEnable()
        {
            EnsureLocomotionCreated();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ReleaseLocomotion();
            }
#endif
        }

        public void EnsureLocomotionCreated()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_locomotion != null)
            {
                return;
            }

            if (profile == null || _animator == null)
            {
                return;
            }

            _locomotion = profile.CreateLocomotion(_animator);
        }

        private void ReleaseLocomotion()
        {
            if (_locomotion == null)
            {
                return;
            }

            _locomotion.Destroy();
            _locomotion = null;
        }
    }
}
