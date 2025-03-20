using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private int maxAmmo = 10; // Maximum ammo capacity
    [SerializeField] private int currentAmmo; // Current ammo count

    [SerializeField] private ParticleSystem muzzleFire;

    private float lastShootTime = 0f;
    private bool isAiming;
    private PlayerControls playerControls;
    private InputAction shootAction;

    public event Action OnShoot;
    public event Action<int, int> OnAmmoChanged; // Event to notify UI updates (current ammo, max ammo)

    private void Awake()
    {
        playerControls = new PlayerControls();
        shootAction = playerControls.Controls.Shoot;
        currentAmmo = maxAmmo; // Start with full ammo
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Update()
    {
        isAiming = GetComponent<PlayerMovement>().isAiming;

        if (isAiming && shootAction.IsPressed() && currentAmmo > 0)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (Time.time - lastShootTime >= fireRate)
        {
            FireBullet();
            lastShootTime = Time.time;
        }
    }

    private void FireBullet()
    {
        if (currentAmmo > 0)
        {
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            bullet.GetComponent<Bullet>().SetBulletDirection(GetComponent<PlayerMovement>().GetAimDirection() * bulletSpeed);
            muzzleFire.Play();
            currentAmmo--; // Decrease ammo
            OnShoot?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo); // Notify UI
        }
    }

    public void RefillAmmo(int amount)
    {
        currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, maxAmmo);
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo); // Notify UI
    }
}
