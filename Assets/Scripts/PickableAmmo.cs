using UnityEngine;

public class PickableAmmo : MonoBehaviour
{
    [SerializeField] private int ammoAmount = 50; // Amount of ammo refilled
    [SerializeField] private float rotation_speed = 0.5f; // Amount of ammo refilled

    private void Update()
    {
        transform.Rotate(0f, rotation_speed, 0f);
    }


    private void OnTriggerEnter(Collider other)
    {
        PlayerShooting playerShooting = other.GetComponent<PlayerShooting>();

        if (playerShooting != null)
        {
            playerShooting.RefillAmmo(ammoAmount);
            Destroy(gameObject); // Destroy the pickup after use
        }
    }
}