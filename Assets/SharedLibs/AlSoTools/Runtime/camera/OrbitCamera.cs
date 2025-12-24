using System.Configuration;
using UnityEngine;

namespace AlSo.Puma
{
    public class OrbitCamera : MonoBehaviour
    {
        public Transform MyTarget;

        public float distanceMin = .5f;
        public float distanceMax = 15f;
        public float Offset;

        private float xSpeed = 120.0f;
        private float ySpeed = 120.0f;
        private float yMinLimit = -20f;
        private float yMaxLimit = 80f;

        public float distance = 5.0f;
        private float ZoomSensitivity = 0.1f;

        private float _x;
        private float _y;

        private bool teleported;
        private Vector3 teleportedPosition;

        private float Horizontal { get; set; } = 0;
        private float Vertical { get; set; } = 0;

        public Vector2 ScreenPosition => new Vector2(_x, _y);

        public Vector2 ScreenShift => new Vector2(Horizontal, Vertical);

        public void Teleport(Vector3 position, Vector2 screenPosition, Vector2 screenShift)
        { 
            teleportedPosition = position;
            _x = screenPosition.x;
            _y = screenPosition.y;
            Horizontal = screenShift.x;
            Vertical = screenShift.y;
            teleported = true;

        }

        public void Update()
        {
            if (teleported) 
            { 
                teleported = false;

                transform.position = teleportedPosition;
                Vector3 aim = MyTarget.position + Vector3.up * Offset;
                transform.LookAt(aim);
                transform.Translate(new Vector3(Horizontal, Vertical, 0) * 0.1f);
                distance = (teleportedPosition - aim).magnitude;
                //return;
            }

            if (Input.GetMouseButton(1))
            {
                _x += Input.GetAxis("Mouse X") * xSpeed * 0.04f;
                _y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            }

            if (Input.GetKey(KeyCode.D)) Horizontal += 0.1f;
            if (Input.GetKey(KeyCode.A)) Horizontal -= 0.1f;
            if (Input.GetKey(KeyCode.W)) Vertical += 0.1f;
            if (Input.GetKey(KeyCode.S)) Vertical -= 0.1f;
            if (Input.GetKey(KeyCode.Keypad0)) Vertical = Horizontal = 0.1f;


            distance = Mathf.Clamp(distance - ZoomSensitivity * Input.mouseScrollDelta.y, distanceMin, distanceMax);

            //if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.R)) Offset += OffsetSencivity;
            //if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.F)) Offset -= OffsetSencivity;

            _y = ClampAngle(_y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(_y, _x, 0);

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = MyTarget.position + Vector3.up * Offset + rotation * negDistance ;

            this.transform.position = position;
            this.transform.LookAt(MyTarget.position + Vector3.up * Offset);
            this.transform.Translate(new Vector3(Horizontal, Vertical, 0) * 0.1f);
        }

        public static float ClampAngle(float angle, float min, float max) 
        {
            while (angle < -360F) angle += 360F;
            while (angle > 360F) angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}