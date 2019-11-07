using UnityEngine;

namespace Etc
{
    /// <summary>
    /// Shoot a sphere depending on the duration of a space bar press.
    /// </summary>
    public class ShootSphere : MonoBehaviour
    {
        public GameObject sphere;

        public float maxSpeed;

        private float _startChargeTime;
        public float maxChargeTime;
    
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _startChargeTime = Time.time;
            }

            if (!Input.GetKeyUp(KeyCode.Space)) return;
            
            var charge = Mathf.Clamp01((Time.time - _startChargeTime) / maxChargeTime);
            var t = transform;
            var sphereGo = Instantiate(sphere, t.position, t.rotation);
            sphereGo.GetComponent<Rigidbody>().AddForce(charge * maxSpeed * t.forward, ForceMode.VelocityChange);

        }
    }
}
