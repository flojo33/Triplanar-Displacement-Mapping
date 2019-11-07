using UnityEngine;

namespace Etc
{
    /// <summary>
    /// Create a random light color on startup and change the materials base color to match.
    /// </summary>
    public class RandomLightColor : MonoBehaviour
    {
        private void Start()
        {
            var color = Random.ColorHSV(0, 1, 0.5f, 1, 1, 1);
            GetComponent<Light>().color = color;
            transform.parent.GetComponent<MeshRenderer>().material.color = color;
        }
    }
}