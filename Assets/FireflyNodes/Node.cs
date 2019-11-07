using UnityEngine;
using Random = UnityEngine.Random;

public class Node : MonoBehaviour
{
    public Transform spinner, main;
    public AnimationCurve hoverCurve;
    private float randomTime;
    private float lastDirectionChange;
    private Rigidbody rb;
    public float maxSpeed = 10.0f;

    private Vector3 _currentForceDirection;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        randomTime = Random.Range(0, hoverCurve.length);
    }
    
    // Update is called once per frame
    void Update()
    {
        spinner.Rotate(new Vector3(0,0,100 * Time.deltaTime));
        main.localPosition = new Vector3(0,hoverCurve.Evaluate(randomTime + Time.time) * 3,0);

        var currentSpeed = rb.velocity.magnitude;
        if (currentSpeed > 5f)
        {
            lastDirectionChange = Time.time;
        }
        if (Time.time - lastDirectionChange > 2.0f)
        {
            var dir = Random.insideUnitCircle * Random.Range(0, maxSpeed);
            _currentForceDirection = new Vector3(dir.x,0,dir.y);
            lastDirectionChange = Time.time;
        }

        rb.AddForce(_currentForceDirection * (Mathf.Clamp01(1- currentSpeed / maxSpeed)), ForceMode.VelocityChange);
        if (transform.position.y < -300)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position ,transform.position + _currentForceDirection);
    }
}