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

        private Animator _animator;
        private LocomotionSystem _locomotion;

        public LocomotionSystem Locomotion => _locomotion;

        // Odin helpers для показа полей
        private bool ShowCartesianSpeedFields => !useInput && !usePolarInput;
        private bool ShowPolarSpeedFields => !useInput && usePolarInput;

        private void Awake()
        {
            // Важно для Timeline/скраба: Awake в Edit Mode может дергаться неоднозначно,
            // поэтому НЕ создаём/не пересоздаём локомоушен здесь.
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            EnsureLocomotionCreated();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            // В Edit Mode освобождаем граф, чтобы не оставлять висячие PlayableGraph'ы.
            if (!Application.isPlaying)
            {
                ReleaseLocomotion();
            }
#endif
        }

        private void Update()
        {
            // В Edit Mode Update тоже вызывается из-за ExecuteAlways,
            // но для Timeline мы НЕ хотим тут трогать скорость/инпут и перетирать управление.
            if (!Application.isPlaying)
            {
                return;
            }

            if (_locomotion == null)
            {
                return;
            }

            // --------- выбираем источник скорости ---------
            Vector2 speed;

            if (useInput)
            {
                // Классический режим: WASD / Arrow keys
                float x = Input.GetAxisRaw("Horizontal");
                float z = Input.GetAxisRaw("Vertical");
                speed = new Vector2(x, z);
            }
            else if (usePolarInput)
            {
                // Полярный режим: модуль + угол
                float mag = speedMagnitude;
                float angleRad = speedAngleDeg * Mathf.Deg2Rad;

                // 0° = вперёд (по Z+)
                // X = вправо, Z = вперёд
                float x = Mathf.Sin(angleRad) * mag;
                float z = Mathf.Cos(angleRad) * mag;

                speed = new Vector2(x, z);
            }
            else
            {
                // Старый ручной режим X/Z
                speed = new Vector2(speedX, speedZ);
            }

            debugSpeed = speed;

            // кастомный локомоушен (PlayableGraph + Mixer)
            _locomotion.UpdateLocomotion(speed);

            // параллельно — Unity BlendTree
            if (unityBlendTreeAnimator != null)
            {
                if (!string.IsNullOrEmpty(unitySpeedXParam))
                {
                    unityBlendTreeAnimator.SetFloat(unitySpeedXParam, speed.x);
                }

                if (!string.IsNullOrEmpty(unitySpeedZParam))
                {
                    unityBlendTreeAnimator.SetFloat(unitySpeedZParam, speed.y);
                }
            }

            // запуск атаки по Q
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (attackClip != null)
                {
                    _locomotion.PerformClip(attackClip, 0.1f, 0.1f);
                    UnityEngine.Debug.Log("[LocomotionProfileTest] Q pressed: PerformClip(attackClip).");

                    if (unityBlendTreeAnimator != null)
                    {
                        unityBlendTreeAnimator.SetTrigger("Fire");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[LocomotionProfileTest] Q pressed, but attackClip is not assigned.");
                }
            }
        }

        private void OnDestroy()
        {
            // Важно: Destroy вызывается и в Play, и в Edit (при удалении/пересборке сцены).
            ReleaseLocomotion();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmo)
            {
                return;
            }

            // Оставил как было: гизмо только в Play Mode.
            // Если захочешь — можно убрать этот ранний return и рисовать в Edit Mode тоже.
            if (!Application.isPlaying)
            {
                return;
            }

            Vector3 v = new Vector3(debugSpeed.x, 0f, debugSpeed.y);
            if (v.sqrMagnitude < 0.0001f)
            {
                return;
            }

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

        /// <summary>
        /// Важно для Timeline/скраба: миксер может вызывать это перед UpdateLocomotion.
        /// </summary>
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
                // В редакторе профиль может быть ещё не назначен/перекомпилируется — не спамим.
                if (Application.isPlaying)
                {
                    UnityEngine.Debug.LogError("[LocomotionProfileTest] Profile is not assigned.");
                }
                return;
            }

            _locomotion = profile.CreateLocomotion(_animator);
        }

        public void ReleaseLocomotion()
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
