using AlSo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AlSo.ShortCuts;

namespace AlSo
{
    public class FreeCamera : MonoBehaviour
    {
        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Vector3 Rotation
        {
            get => transform.rotation.eulerAngles;
            set => transform.rotation = Quaternion.Euler(value);
        }

        public IFreeCamConfig Config { get; set; } = new FreeCamConfig();

        private Camera _camera;
        protected Camera Camera => CreateIfNotExist(ref _camera, () => this.GetComponent<Camera>());

        void Update()
        {
            transform.position += MoveBody();
            transform.eulerAngles += MoveFocus();
        }

        private Vector3 MoveBody()
        {
            Vector2 planeInput = GetPlaneInput();
            Vector3 planeShift = GetPlaneShift(planeInput) * Config.KeyPanSensivity;

            float verticalInput = GetVerticalInput();
            Vector3 verticalShift = Vector3.up * verticalInput * Config.KeyTopDownSensivity;

            Vector3 sum = planeShift + verticalShift;

            bool shiftPressed = Input.GetKey(KeyCode.LeftShift);
            Vector3 res = sum * (shiftPressed ? Config.ShiftBoost : 1);
            return res;
        }

        private Vector3 GetPlaneShift(Vector2 panDelta)
        {
            float sx = -panDelta.x * Mathf.Cos(AngleRad) - panDelta.y * Mathf.Sin(AngleRad);
            float sy = panDelta.x * Mathf.Sin(AngleRad) - panDelta.y * Mathf.Cos(AngleRad);

            Vector3 shift = new Vector3(sx, 0, sy);
            return shift;
        }

        private Vector2 GetPlaneInput()
        {
            float x = 0;
            float y = 0;

            if (Input.GetKey(KeyCode.W)) y = -1;
            if (Input.GetKey(KeyCode.S)) y = 1;

            if (Input.GetKey(KeyCode.D)) x = -1;
            if (Input.GetKey(KeyCode.A)) x = 1;

            return new Vector2(x, y);
        }

        private float GetVerticalInput()
        {
            float z = 0;
            if (Input.GetKey(KeyCode.R)) z = 1;
            if (Input.GetKey(KeyCode.F)) z = -1;
            return z;
        }

        private Vector3 MoveFocus()
        {
            bool isInControlMode = Input.GetKey(KeyCode.Mouse1);

            Cursor.visible = !isInControlMode;
            Cursor.lockState = isInControlMode ? CursorLockMode.Locked : CursorLockMode.None;

            return isInControlMode ? new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0) * Config.FreeLookSensitivity : Vector3.zero;
        }

        private float AngleDeg => transform.localEulerAngles.y;
        private float AngleRad => Mathf.Deg2Rad * AngleDeg;
    }
}