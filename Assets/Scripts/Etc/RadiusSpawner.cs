using UnityEngine;

namespace Etc
{
    public class RadiusSpawner : MonoBehaviour
    {
        public int count;
        public float maxDistance;
        public float forwardOffset = 40;
        public GameObject spherePrefab;
        // Start is called before the first frame update
        private void Update()
        {
            //Random.InitState(0);
            if (Input.GetKeyDown(KeyCode.N))
            {
                for (var i = 0; i < count; i++)
                {
                    var currentTransform = transform;
                    Instantiate(spherePrefab, currentTransform.position + (currentTransform.forward * forwardOffset) + Random.insideUnitSphere * maxDistance, Quaternion.identity);
                }
            }
        }
    }
}
