using UnityEngine;
using Cinemachine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private PlayerShooting shootingController;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float shakeIntensity = 2f;
    [SerializeField] private float shakeDuration = 0.2f;

    private CinemachineBasicMultiChannelPerlin noise;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (virtualCamera != null)
        {
            noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    private void OnEnable()
    {
        if (shootingController != null)
        {
            shootingController.OnShoot += StartShake;
        }
    }

    private void OnDisable()
    {
        if (shootingController != null)
        {
            shootingController.OnShoot -= StartShake;
        }
    }

    private void StartShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        if (noise == null) yield break;

        float elapsed = 0f;
        float randomDirection = Random.Range(-1f, 1f); // Shake direction can go left or right

        while (elapsed < shakeDuration)
        {
            noise.m_AmplitudeGain = shakeIntensity * Mathf.Sign(randomDirection);
            elapsed += Time.deltaTime;
            yield return null;
        }

        noise.m_AmplitudeGain = 0f; // Reset shake
    }
}
