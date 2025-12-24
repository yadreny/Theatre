using UnityEngine;

namespace AlSo
{
    [RequireComponent(typeof(Animator))]
    public class FootMagnetIK : MonoBehaviour
    {
        private enum FootPhase
        {
            Free,       // M ~ 0, IK не вмешивается
            PreContact, // 0 < M < contactThreshold и магнит растёт (нога тянется к земле)
            Contact,    // M >= contactThreshold, нога в опоре
            Release     // 0 < M < contactThreshold и магнит падает (нога уходит от земли)
        }

        public bool enableFootIK = true;

        [Range(0f, 1f)]
        public float globalFootIKWeight = 1f;

        public bool debugForceFullMagnet = false;

        public LayerMask groundLayerMask = ~0;

        /// <summary>
        /// Высота начала луча над стопой (вдоль transform.up).
        /// </summary>
        public float raycastHeight = 0.5f;

        /// <summary>
        /// Дистанция, на которую луч бьёт вниз от точки старта (вдоль -transform.up).
        /// </summary>
        public float raycastDownDistance = 1.0f;

        /// <summary>
        /// Расстояние от кости Foot до пола при нейтральной стойке.
        /// Одинаковое для обеих ног.
        /// </summary>
        public float footHeight = 0.1f;

        /// <summary>
        /// Небольшой дополнительный отступ над землёй.
        /// </summary>
        public float footHeightOffset = 0.02f;

        public float minStepHeight = 0.02f;
        public float maxFootAboveGround = 0.4f;

        [SerializeField] private float _debugLeftMagnet;
        [SerializeField] private float _debugRightMagnet;

        private Animator _animator;

        private float _prevLeftMagnet;
        private float _prevRightMagnet;

        private Vector3 _leftFinalPos;
        private Vector3 _rightFinalPos;

        private FootPhase _leftPhase = FootPhase.Free;
        private FootPhase _rightPhase = FootPhase.Free;

        private Vector3 _leftDebugPos;
        private Vector3 _rightDebugPos;

        private bool _validated;
        private bool _validSetup;

        private LocomotionProfileTest _locomotionTest;
        private LocomotionSystem _locomotionSystem;

        private bool _ikCalledThisFrame;
        private bool _loggedNoIKPass;

        private const float FreeThreshold = 0.001f;     // M <= это → Free
        private const float ContactThreshold = 0.95f;   // M >= это → Contact

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _locomotionTest = GetComponent<LocomotionProfileTest>();
            ValidateOnce();
        }

        private void LateUpdate()
        {
            if (!enableFootIK)
            {
                _ikCalledThisFrame = false;
                return;
            }

            if (!_ikCalledThisFrame && !_loggedNoIKPass)
            {
                UnityEngine.Debug.LogWarning(
                    $"[FootMagnetIK] OnAnimatorIK is not called for Animator '{_animator?.name}'. " +
                    "Check IK Pass on the layer and that Animator is actually updating (not Cull Completely).");
                _loggedNoIKPass = true;
            }

            _ikCalledThisFrame = false;
        }

        private void ValidateOnce()
        {
            if (_validated) return;
            _validated = true;

            if (_animator == null)
            {
                UnityEngine.Debug.LogError("[FootMagnetIK] Animator component not found.");
                return;
            }

            if (!_animator.isHuman || _animator.avatar == null || !_animator.avatar.isValid)
            {
                UnityEngine.Debug.LogError($"[FootMagnetIK] Animator '{_animator.name}' must have a valid humanoid Avatar.");
                return;
            }

            _validSetup = true;
        }

        private void TryResolveLocomotion()
        {
            if (_locomotionSystem != null) return;

            if (_locomotionTest == null)
                _locomotionTest = GetComponent<LocomotionProfileTest>();

            if (_locomotionTest != null)
            {
                _locomotionSystem = _locomotionTest.Locomotion;
                if (_locomotionSystem == null)
                {
                    UnityEngine.Debug.LogWarning(
                        "[FootMagnetIK] LocomotionProfileTest found, but LocomotionSystem is null. " +
                        "Make sure profile is assigned and Locomotion is created in Awake().");
                }
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            _ikCalledThisFrame = true;

            if (!enableFootIK)
                return;

            if (!_validated)
                ValidateOnce();

            if (!_validSetup || _animator == null)
                return;

            if (!_animator.isHuman || _animator.avatar == null || !_animator.avatar.isValid)
                return;

            TryResolveLocomotion();

            float globalW = Mathf.Clamp01(globalFootIKWeight);

            if (globalW <= FreeThreshold)
            {
                ApplyIKWeights(AvatarIKGoal.LeftFoot, 0f);
                ApplyIKWeights(AvatarIKGoal.RightFoot, 0f);
                _leftPhase = FootPhase.Free;
                _rightPhase = FootPhase.Free;
                return;
            }

            float leftMagnet = 0f;
            float rightMagnet = 0f;
            float hipDummy = 0f;

            if (debugForceFullMagnet)
            {
                leftMagnet = 1f;
                rightMagnet = 1f;
            }
            else if (_locomotionSystem != null)
            {
                _locomotionSystem.EvaluateFootAndHip(out leftMagnet, out rightMagnet, out hipDummy);
                leftMagnet = Mathf.Clamp01(leftMagnet);
                rightMagnet = Mathf.Clamp01(rightMagnet);
            }

            _debugLeftMagnet = leftMagnet;
            _debugRightMagnet = rightMagnet;

            ProcessFoot(
                AvatarIKGoal.LeftFoot,
                HumanBodyBones.LeftFoot,
                leftMagnet,
                ref _prevLeftMagnet,
                out _leftFinalPos,
                out _leftPhase,
                out _leftDebugPos);

            ProcessFoot(
                AvatarIKGoal.RightFoot,
                HumanBodyBones.RightFoot,
                rightMagnet,
                ref _prevRightMagnet,
                out _rightFinalPos,
                out _rightPhase,
                out _rightDebugPos);

            ApplyIKWeights(AvatarIKGoal.LeftFoot, leftMagnet * globalW);
            ApplyIKWeights(AvatarIKGoal.RightFoot, rightMagnet * globalW);

            // === Коррекция вертикальной позиции персонажа только при КОНТАКТЕ ===
            bool leftContact = _leftPhase == FootPhase.Contact;
            bool rightContact = _rightPhase == FootPhase.Contact;

            if (leftContact || rightContact)
            {
                float supportFootY;

                if (leftContact && rightContact)
                {
                    // Обе ноги в контакте — опираемся на более низкую
                    supportFootY = Mathf.Min(_leftFinalPos.y, _rightFinalPos.y);
                }
                else if (leftContact)
                {
                    supportFootY = _leftFinalPos.y;
                }
                else
                {
                    supportFootY = _rightFinalPos.y;
                }

                // Нога стоит примерно в:
                // finalFootPos ≈ hit.point + up * (footHeight + footHeightOffset)
                // Чтобы root оказался на уровне пола, вычитаем обе величины.
                float newRootY = supportFootY - (footHeight + footHeightOffset);

                Vector3 rootPos = transform.position;
                rootPos.y = newRootY;
                transform.position = rootPos;
            }
        }

        private void ProcessFoot(
            AvatarIKGoal goal,
            HumanBodyBones bone,
            float magnet,
            ref float prevMagnet,
            out Vector3 finalFootWorldPos,
            out FootPhase phase,
            out Vector3 debugPos)
        {
            magnet = Mathf.Clamp01(magnet);

            Transform foot = _animator.GetBoneTransform(bone);
            if (foot == null)
            {
                finalFootWorldPos = Vector3.zero;
                debugPos = finalFootWorldPos;
                ApplyIKWeights(goal, 0f);
                phase = FootPhase.Free;

                UnityEngine.Debug.LogWarning(
                    $"[FootMagnetIK] Cannot find humanoid bone '{bone}' on Animator '{_animator.name}'.");
                return;
            }

            Vector3 animPos = foot.position;
            finalFootWorldPos = animPos;
            debugPos = animPos;

            float delta = magnet - prevMagnet;

            // Фазовая логика
            if (magnet <= FreeThreshold)
            {
                phase = FootPhase.Free;
                prevMagnet = magnet;
                return;
            }

            if (magnet >= ContactThreshold)
            {
                phase = FootPhase.Contact;
            }
            else
            {
                phase = (delta >= 0f) ? FootPhase.PreContact : FootPhase.Release;
            }

            // Проекция на землю и blend по магниту
            if (TryProjectToGround(animPos, out Vector3 projPos))
            {
                Vector3 blendedPos = Vector3.Lerp(animPos, projPos, magnet);
                _animator.SetIKPosition(goal, blendedPos);
                finalFootWorldPos = blendedPos;
                debugPos = blendedPos;
            }
            else
            {
                finalFootWorldPos = animPos;
                debugPos = animPos;
            }

            prevMagnet = magnet;
        }

        private bool TryProjectToGround(Vector3 fromPos, out Vector3 projPos)
        {
            Vector3 up = transform.up;

            float extraUp = 1.0f;

            float startOffset = raycastHeight + extraUp;
            Vector3 rayOrigin = fromPos + up * startOffset;

            float rayLength = startOffset + raycastDownDistance;

            if (Physics.Raycast(
                    rayOrigin,
                    -up,
                    out RaycastHit hit,
                    rayLength,
                    groundLayerMask,
                    QueryTriggerInteraction.Ignore))
            {
                float ankleToGround = fromPos.y - hit.point.y;

                if (ankleToGround < minStepHeight || ankleToGround > maxFootAboveGround)
                {
                    projPos = Vector3.zero;
                    return false;
                }

                float totalOffset = Mathf.Max(0f, footHeight) + footHeightOffset;
                projPos = hit.point + up * totalOffset;
                return true;
            }

            projPos = Vector3.zero;
            return false;
        }

        private void ApplyIKWeights(AvatarIKGoal goal, float weight)
        {
            weight = Mathf.Clamp01(weight);

            if (weight <= 0f)
            {
                _animator.SetIKPositionWeight(goal, 0f);
                _animator.SetIKRotationWeight(goal, 0f);
                return;
            }

            _animator.SetIKPositionWeight(goal, weight);
            _animator.SetIKRotationWeight(goal, 0f);
        }

#if UNITY_EDITOR
        public float footGizmoSize = 0.3f;

        private void OnDrawGizmos()
        {
            if (!enableFootIK)
                return;

            DrawFootGizmo(_leftDebugPos, _leftPhase);
            DrawFootGizmo(_rightDebugPos, _rightPhase);
        }

        private void DrawFootGizmo(Vector3 pos, FootPhase phase)
        {
            if (pos == Vector3.zero)
                return;

            Color c = phase switch
            {
                FootPhase.Free => Color.green,
                FootPhase.PreContact => Color.red,
                FootPhase.Release => Color.red,
                FootPhase.Contact => Color.blue,
                _ => Color.white,
            };

            Gizmos.color = c;
            Gizmos.DrawSphere(pos, footGizmoSize);
        }
#endif
    }
}
