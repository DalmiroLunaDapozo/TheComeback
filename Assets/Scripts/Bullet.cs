using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float bulletLifetime = 5f;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private int damage = 10;

    private Rigidbody rb;

    [SerializeField] private ParticleSystem sparks;
    [SerializeField] private ParticleSystem fleshHit;
    private RaycastHit hit;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Improve fast object collision
            rb.isKinematic = false;
        }

        Collider playerCollider = GameObject.FindGameObjectWithTag("Player").GetComponent<Collider>();
        if (playerCollider != null)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), playerCollider);
        }

        Destroy(gameObject, bulletLifetime);
    }

    public void SetBulletDirection(Vector3 direction)
    {
        if (rb != null)
        {
            rb.velocity = direction * bulletSpeed;
        }
    }

    void FixedUpdate()
    {
        // Raycast ahead to prevent missing fast-moving objects
        if (Physics.Raycast(transform.position, rb.velocity.normalized, out hit, rb.velocity.magnitude * Time.fixedDeltaTime))
        {
            HandleHit(hit.collider, hit.point, hit.normal); // Use raycast hit information
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0]; // Get the collision contact point
        HandleHit(collision.collider, contact.point, contact.normal); // Use collision contact information
    }

    private void HandleHit(Collider collider, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (collider.CompareTag("Enemy"))
        {
            EnemyAI enemy = collider.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                Vector3 hitPosition = hitPoint;
                hitPosition.y -= 0.7f; // Adjust hit position slightly

                Instantiate(fleshHit, hitPosition, Quaternion.LookRotation(hitNormal)); // Instantiate flesh hit effect
                enemy.TakeDamage(damage); // Apply damage to the enemy
            }
        }
        else if (collider.CompareTag("Untagged"))
        {
            Instantiate(sparks, hitPoint, Quaternion.LookRotation(hitNormal)); // Instantiate sparks on collision
        }

        // Stop bullet immediately
        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero; // Stop movement only if it's still dynamic
            rb.isKinematic = true;
        }


        Destroy(gameObject, 0.05f); // Destroy the bullet after impact
    }
}
