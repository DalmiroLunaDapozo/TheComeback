using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private float attackDistance;
    [SerializeField] private int health;

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private Collider mainCollider;
    private Renderer enemyRenderer;
    private Color originalColor;

    public bool isDead;
    private bool isAttacking; // NEW: Track attack state

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
        else
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");

        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        mainCollider = GetComponent<Collider>();
        DisableRagdoll();
        enemyRenderer = GetComponentInChildren<Renderer>();
        originalColor = enemyRenderer.material.color;
    }

    private void Update()
    {
        if (isDead || isAttacking) return; // Don't update movement if attacking

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < attackDistance)
        {
            agent.isStopped = true;
            StartAttack(); // NEW: Start attack if close enough
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    private void StartAttack()
    {
        if (!isAttacking) // Ensure we don't restart an attack mid-animation
        {
            isAttacking = true;
            animator.SetBool("Attack", true);
            StartCoroutine(AttackCooldown()); // NEW: Handle attack completion
        }
    }

    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(1.5f); // Adjust to match attack animation duration
        isAttacking = false;
        animator.SetBool("Attack", false);
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        StartCoroutine(HitFlashEffect());

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        agent.enabled = false;
        animator.enabled = false;
        EnableRagdoll();
        isDead = true;
        Destroy(gameObject, 5f);
    }

    private void DisableRagdoll()
    {
        foreach (Rigidbody rb in ragdollBodies)
            rb.isKinematic = true;

        foreach (Collider col in ragdollColliders)
        {
            if (col != mainCollider)
                col.enabled = false;
        }
    }

    private void EnableRagdoll()
    {
        mainCollider.enabled = false;

        foreach (Rigidbody rb in ragdollBodies)
            rb.isKinematic = false;

        foreach (Collider col in ragdollColliders)
        {
            if (col != mainCollider)
                col.enabled = true;
        }

        ragdollBodies[0].AddForce(Vector3.back * 50f, ForceMode.Impulse);
    }

    private IEnumerator HitFlashEffect()
    {
        enemyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        enemyRenderer.material.color = originalColor;
    }
}
