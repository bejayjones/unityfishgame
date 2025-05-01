using UnityEngine;
using System.Collections.Generic;

public class BoidFish : MonoBehaviour
{
    public float speed = 2f;
    public float neighborDistance = 3f;
    public float separationDistance = 5f;
    public float maxForce = 0.5f;
    public float maxSpeed = 7f;

    private Vector3 velocity;
    private static List<BoidFish> allBoids = new List<BoidFish>();
    private static Vector3 boundsMin = new Vector3(-10, -3, -2);
    private static Vector3 boundsMax = new Vector3(10, 3, 2);

    void Start()
    {
        velocity = Random.insideUnitSphere.normalized * speed;
        allBoids.Add(this);
    }

    void OnDestroy()
    {
        allBoids.Remove(this);
    }

    void Update()
    {
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;

        int count = 0;
        foreach (BoidFish other in allBoids)
        {
            if (other == this) continue;
            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < neighborDistance)
            {
                // Separation
                if (dist < separationDistance)
                    separation += (transform.position - other.transform.position) / dist;

                // Alignment
                alignment += other.velocity;

                // Cohesion
                cohesion += other.transform.position;

                count++;
            }
        }

        if (count > 0)
        {
            separation /= count;
            alignment /= count;
            cohesion = (cohesion / count - transform.position);

            separation = separation.normalized * maxSpeed - velocity;
            alignment = alignment.normalized * maxSpeed - velocity;
            cohesion = cohesion.normalized * maxSpeed - velocity;
        }

        Vector3 steer = separation + alignment/2 + cohesion/2;
        steer = Vector3.ClampMagnitude(steer, maxForce);
        velocity = Vector3.ClampMagnitude(velocity + steer, maxSpeed);
        transform.position += velocity * Time.deltaTime;

        // Keep fish inside camera bounds (bounce-style)
        Vector3 pos = transform.position;
        if (pos.x < boundsMin.x || pos.x > boundsMax.x) velocity.x *= -1;
        if (pos.y < boundsMin.y || pos.y > boundsMax.y) velocity.y *= -1;
        if (pos.z < boundsMin.z || pos.z > boundsMax.z) velocity.z *= -1;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.Cross(velocity, Vector3.down).normalized), Time.deltaTime * 5f);
    }
}