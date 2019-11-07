using UnityEngine;

namespace Etc
{
    /// <summary>
    /// Smoothly move and rotate the camera or run along a fixed path defined in lockedStart and lockedForward
    /// </summary>
    public class CameraMovement : MonoBehaviour {

        public bool lockedPath;
        public Vector3 lockedStart, lockedForward;
        private float _lockedStartTime;
        
        public float maxSpeedH = 2.0f;
        public float maxSpeedV = 2.0f;

        public float accelerationH = 1.0f;
        public float accelerationV = 1.0f;

        public float dragH = 1.0f;
        public float dragV = 1.0f;
    
        private float _speedH, _speedV;

        private float _yaw;
        private float _pitch;
        public float speed;
        private Vector3 _currentSpeed;
        public float drag = 3;

        public bool showingMenu;

        public GameObject menu;
        
        private void Start()
        {
            _lockedStartTime = Time.time;
            Cursor.visible = false;
        }

        private void Update ()
        {
            if (!showingMenu)
            {
                _speedH += accelerationH * Input.GetAxis("Mouse X") * Time.deltaTime;
                _speedV += accelerationV * -Input.GetAxis("Mouse Y") * Time.deltaTime;
                var speedCurrent = speed;
                speedCurrent *= Input.GetKey(KeyCode.LeftShift) ? 6 : 1;
                _currentSpeed.x += speedCurrent * Input.GetAxis("Vertical") * Time.deltaTime;
                _currentSpeed.y += speedCurrent * Input.GetAxis("Horizontal") * Time.deltaTime;
            }
            
            menu.SetActive(showingMenu);

            _speedH *= (1-(dragH* Time.deltaTime));
            _speedV *= (1-(dragV* Time.deltaTime));
        
            _speedH = Mathf.Clamp(_speedH, -maxSpeedH, maxSpeedH);
            _speedV = Mathf.Clamp(_speedV, -maxSpeedV, maxSpeedV);


            _currentSpeed *= 1-drag * Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                showingMenu = !showingMenu;
                Cursor.visible = showingMenu;
            }
        }
        private void FixedUpdate()
        {
            if (lockedPath)
            {
                transform.position = lockedStart + (lockedForward * (Time.time - _lockedStartTime));
                transform.rotation = Quaternion.LookRotation(lockedForward);
            }
            else
            {
                _yaw += _speedH;
                _pitch += _speedV;

                if (_pitch > 90)
                {
                    _pitch = 90;
                    _speedV = 0;
                }
                if (_pitch < -90)
                {
                    _pitch = -90;
                    _speedV = 0;
                }

                Transform currentTransform;
                (currentTransform = transform).rotation = Quaternion.Euler(new Vector3(_pitch, _yaw, 0));
                currentTransform.position += _currentSpeed.x * currentTransform.forward + _currentSpeed.y * currentTransform.right;
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global Because it is used from the Unity editor.
        public void Quit()
        {
            Application.Quit();
        }
    }
}