using Sirenix.OdinInspector;
using UnityEngine;

namespace AlSo
{
//#if UNITY_EDITOR
//    [ExecuteAlways]
//#endif
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
        [LabelText("Debug Speed (X,Z)")]
        public Vector2 debugSpeed;

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

        [Header("Root Motion")]
        [Tooltip("Выключать root motion в Edit Mode (чтобы персонаж не 'полз' при скрабе/статичном времени).")]
        public bool disableRootMotionInEditMode = true;

        [Tooltip("Выключать root motion, когда TimelineDriven=true (потому что Timeline сам детерминированно двигает Transform).")]
        public bool disableRootMotionWhenTimelineDriven = true;

        private Animator _animator;
        private LocomotionSystem _locomotion;

        private bool _defaultRootMotionCaptured;
        private bool _defaultApplyRootMotion;

        public LocomotionSystem Locomotion => _locomotion;

        // ВАЖНО: когда true — управление скоростью/экшенами идёт от Timeline, Update() не вмешивается.
        [ShowInInspector, ReadOnly]
        public bool TimelineDriven { get; private set; }

        // Odin helpers для показа полей
        private bool ShowCartesianSpeedFields => !useInput && !usePolarInput;
        private bool ShowPolarSpeedFields => !useInput && usePolarInput;

        private void Awake()
        {
            _locomotion = null;
            _animator = GetComponent<Animator>();

            CaptureDefaultRootMotionIfNeeded();
            ApplyRootMotionPolicy();

            if (profile != null)
            {
                _locomotion = profile.CreateLocomotion(_animator);
            }
            else
            {
                UnityEngine.Debug.LogError("[LocomotionProfileTest] Profile is not assigned.");
            }
        }

        private void CaptureDefaultRootMotionIfNeeded()
        {
            if (_animator == null)
            {
                return;
            }

            if (_defaultRootMotionCaptured)
            {
                return;
            }

            _defaultApplyRootMotion = _animator.applyRootMotion;
            _defaultRootMotionCaptured = true;
        }

        private void ApplyRootMotionPolicy()
        {
            if (_animator == null)
            {
                return;
            }

            CaptureDefaultRootMotionIfNeeded();

            bool desired = _defaultRootMotionCaptured ? _defaultApplyRootMotion : _animator.applyRootMotion;

#if UNITY_EDITOR
            if (!Application.isPlaying && disableRootMotionInEditMode)
            {
                desired = false;
            }
#endif

            if (TimelineDriven && disableRootMotionWhenTimelineDriven)
            {
                desired = false;
            }

            if (_animator.applyRootMotion != desired)
            {
                _animator.applyRootMotion = desired;
            }
        }

        private void Update()
        {
            if (_locomotion == null)
                return;

            // Ключевой фикс: если Timeline сейчас ведёт — не затираем его работу.
            if (TimelineDriven)
                return;

#if UNITY_EDITOR
            // В Edit Mode без Timeline управления — тоже не гоняем Input.
            if (!Application.isPlaying)
                return;
#endif

            // --------- выбираем источник скорости ---------
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
                    // Вручную — как раньше
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
            RestoreDefaultRootMotion();

            if (_locomotion != null)
            {
                _locomotion.Destroy();
                _locomotion = null;
            }
        }

        private void RestoreDefaultRootMotion()
        {
            if (_animator == null)
            {
                return;
            }

            if (!_defaultRootMotionCaptured)
            {
                return;
            }

            if (_animator.applyRootMotion != _defaultApplyRootMotion)
            {
                _animator.applyRootMotion = _defaultApplyRootMotion;
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
            ApplyRootMotionPolicy();
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

            // стопаем скорость (чтобы не “дотаптывал”)
            debugSpeed = Vector2.zero;
            _locomotion.UpdateLocomotion(Vector2.zero);

            // fade по процентам
            float len = actionData.Clip.length;
            float fadeIn = Mathf.Clamp01(actionData.FadeInPercent) * len;
            float fadeOut = Mathf.Clamp01(actionData.FadeOutPercent) * len;

            _locomotion.PerformClip(actionData.Clip, fadeIn, fadeOut);
        }

        // ==== Ensure for Edit Mode / Timeline ====

        private void OnEnable()
        {
            EnsureLocomotionCreated();
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            CaptureDefaultRootMotionIfNeeded();
            ApplyRootMotionPolicy();
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
