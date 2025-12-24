//using Sirenix.OdinInspector;
//using UnityEngine;

//namespace AlSo
//{

//    public class RunToBehaviour : MonoBehaviour, IRunToWaypoint
//    {
//        [Header("Defaults")]
        
//        [SerializeField] private float defaultDuration = 1f;

//        [SerializeField] private bool useUnscaledTime = false;
//        [SerializeField] private bool useLocalPosition = false;

//        [SerializeField] private Transform destination;

//        private bool _isMoving;
//        private Vector3 _start;
//        private Vector3 _target;
//        private float _duration;
//        private float _elapsed;

//        public bool IsMoving => _isMoving;
//        public float NormalizedT => _isMoving ? Mathf.Clamp01(_elapsed / _duration) : 1f;

//        private void Update()
//        {
//            Tick(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
//        }

//        [Button]
//        public void Test() => RunTo(destination);

//        public void StartMoveTo(Vector3 targetPosition, float duration)
//        {
//            if (duration <= 0f) duration = 0.0001f;

//            _start = useLocalPosition ? transform.localPosition : transform.position;
//            _target = targetPosition;

//            _duration = duration;
//            _elapsed = 0f;
//            _isMoving = true;
//        }

//        public void RunTo(Transform target)
//        {
//            if (target == null)
//            {
//                UnityEngine.Debug.LogWarning($"run to: target is null.", this);
//                return;
//            }

//            StartMoveTo(target.position, defaultDuration);
//        }

//        public void StartMoveTo(Transform target, float duration)
//        {
//            if (target == null)
//            {
//                UnityEngine.Debug.LogWarning($"run to: target is null.", this);
//                return;
//            }

//            StartMoveTo(target.position, duration);
//        }

//        public void StopMove()
//        {
//            _isMoving = false;
//        }

//        public void Tick(float deltaTime)
//        {
//            if (!_isMoving) return;

//            _elapsed += Mathf.Max(0f, deltaTime);

//            float t = Mathf.Clamp01(_elapsed / _duration);
//            Vector3 p = Vector3.Lerp(_start, _target, t);

//            if (useLocalPosition) transform.localPosition = p;
//            else transform.position = p;

//            if (t >= 1f)
//            {
//                // На всякий: точное попадание
//                if (useLocalPosition) transform.localPosition = _target;
//                else transform.position = _target;

//                _isMoving = false;
//            }
//        }
//    }
//}
