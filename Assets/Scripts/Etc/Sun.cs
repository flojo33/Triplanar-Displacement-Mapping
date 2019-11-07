using UnityEngine;

namespace Etc
{
    /// <summary>
    /// Create nice looking sun movement and color effects.
    /// </summary>
    [ExecuteInEditMode]
    public class Sun : MonoBehaviour
    {
        [SerializeField]
        private Gradient sunColor = new Gradient();

        [SerializeField]
        private float speed = 5;
        [SerializeField]
        private float angleY = 45;
    
        [SerializeField]
        private float currentAngle;
        private Light _light;
        private bool _playing;
    
        private void Start()
        {
            _light = GetComponent<Light>();
        }
        // Update is called once per frame
        private void Update()
        {
            if (Application.isPlaying && _playing)
            {
                currentAngle += speed * Time.deltaTime;
            }
            if (currentAngle > 360)
            {
                currentAngle -= 360;
            }
            transform.rotation = Quaternion.Euler(new Vector3(currentAngle - 90, angleY, 0));
            _light.color = sunColor.Evaluate((currentAngle) / 360.0f);
        }

        public void SetDusk()
        {
            currentAngle = 90;
        }

        public void SetDay()
        {
            currentAngle = 180;
        }

        public float GetPosition()
        {
            return (currentAngle % 360.0f) / 360.0f;
        }
        public void SetPosition(float value)
        {
            currentAngle = value * 360.0f;
        }

        public void SetPlaying(bool playing)
        {
            _playing = playing;
        }
    }
}
