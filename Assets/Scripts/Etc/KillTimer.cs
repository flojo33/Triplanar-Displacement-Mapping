using UnityEngine;

namespace Etc
{
    /// <summary>
    /// Destroys and object after a certain duration.
    /// </summary>
    public class KillTimer : MonoBehaviour
    {
        private float _startTime;

        public float killDuration;
        
        private void Start()
        {
            _startTime = Time.time;
        }
        
        private void Update()
        {
            if (Time.time - _startTime > killDuration)
            {
                Destroy(gameObject);
            }
        }
    }
}
