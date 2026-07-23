using UnityEngine;

public class WeaponRaycast : MonoBehaviour
{
    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = false;

    [Header("Точки на оружии")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform shellEjectPoint;
    [SerializeField] private Camera playerCamera;

    [Header("Стрельба")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 8f;
    [SerializeField] private bool isAutomatic = true;
    [SerializeField] private LayerMask hitMask;

    [Header("Боезапас")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int reserveAmmo = 90;
    [SerializeField] private float reloadTime = 1.8f;
    private int currentAmmo;
    private bool isReloading;

    [Header("Визуальная пуля (трассер)")]
    [SerializeField] private GameObject bulletTracerPrefab;
    [SerializeField] private float tracerSpeed = 150f;
    [SerializeField] private bool useInstantTracer = false;

    [Header("Эффекты выстрела")]
    [SerializeField] private GameObject muzzleFlashEffect;
    [SerializeField] private GameObject shellCasingPrefab;
    [SerializeField] private float shellEjectForce = 2f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] fireSounds;
    [SerializeField] private AudioClip emptySound;
    [SerializeField] private AudioClip reloadSound;

    [Header("Эффекты попадания")]
    [SerializeField] private GameObject defaultImpactEffect;
    [SerializeField] private GameObject enemyImpactEffect;
    [SerializeField] private float impactEffectLifetime = 2f;

    [Header("Отдача камеры")]
    [SerializeField] private float recoilAmount = 1.5f;
    [SerializeField] private float recoilRecoverySpeed = 8f;
    [SerializeField] private float recoilSnapSpeed = 20f; // насколько резко камера "подскакивает" в момент выстрела
    private float currentRecoil;
    private float targetRecoil;

    [Header("Анимация")]
    [SerializeField] private Animator animator;

    private float fireCooldown;

    private void Awake()
    {
        currentAmmo = magazineSize;

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        fireCooldown -= Time.deltaTime;

        HandleInput();
    }

    // Recoil применяется в LateUpdate, чтобы гарантированно сработать ПОСЛЕ поворота камеры мышью (Look() в PlayerMovement)
    private void LateUpdate()
    {
        HandleRecoilRecovery();
    }

    private void HandleInput()
    {
        if (isReloading) return;

        // Проверяем именно кнопку мыши напрямую, а не ось "Fire1" — она в Unity по умолчанию завязана ещё и на Left Ctrl
        bool wantsToFire = isAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (wantsToFire && fireCooldown <= 0f)
        {
            if (currentAmmo > 0)
            {
                Fire();
                fireCooldown = 1f / fireRate;
            }
            else
            {
                PlayEmptySound();
                fireCooldown = 0.3f;
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineSize && reserveAmmo > 0)
        {
            StartCoroutine(Reload());
        }
    }

    private void Fire()
    {
        currentAmmo--;

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward; // без разброса — всегда строго по центру камеры

        if (animator != null)
            animator.SetTrigger("Fire");

        PlayFireSound();
        SpawnMuzzleFlash();
        EjectShell();

        targetRecoil += recoilAmount;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask))
        {
            HandleHit(hit);
            SpawnTracer(hit.point);

            if (debugLogs)
                Debug.Log($"[{name}] Попал в: {hit.collider.name} на дистанции {hit.distance:F1}");
        }
        else
        {
            Vector3 missPoint = origin + direction * range;
            SpawnTracer(missPoint);

            if (debugLogs)
                Debug.Log($"[{name}] Промах, выстрел ушёл в пустоту");
        }
    }

    private void HandleHit(RaycastHit hit)
    {
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }

        GameObject effectToSpawn = defaultImpactEffect;

        if (hit.collider.CompareTag("Enemy") && enemyImpactEffect != null)
        {
            effectToSpawn = enemyImpactEffect;
        }

        if (effectToSpawn != null)
        {
            GameObject impact = Instantiate(effectToSpawn, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impact, impactEffectLifetime);
        }

        if (hit.rigidbody != null)
        {
            hit.rigidbody.AddForceAtPosition(-hit.normal * 5f, hit.point, ForceMode.Impulse);
        }
    }

    private void SpawnTracer(Vector3 targetPoint)
    {
        if (bulletTracerPrefab == null || firePoint == null) return;

        GameObject tracer = Instantiate(bulletTracerPrefab, firePoint.position, Quaternion.LookRotation(targetPoint - firePoint.position));

        if (useInstantTracer)
        {
            LineRenderer line = tracer.GetComponent<LineRenderer>();
            if (line != null)
            {
                line.SetPosition(0, firePoint.position);
                line.SetPosition(1, targetPoint);
            }
            Destroy(tracer, 0.05f);
        }
        else
        {
            StartCoroutine(MoveTracer(tracer, targetPoint));
        }
    }

    private System.Collections.IEnumerator MoveTracer(GameObject tracer, Vector3 targetPoint)
    {
        float distance = Vector3.Distance(tracer.transform.position, targetPoint);
        float duration = distance / tracerSpeed;
        float elapsed = 0f;
        Vector3 startPos = tracer.transform.position;

        while (elapsed < duration)
        {
            if (tracer == null) yield break;

            elapsed += Time.deltaTime;
            tracer.transform.position = Vector3.Lerp(startPos, targetPoint, elapsed / duration);
            yield return null;
        }

        if (tracer != null)
            Destroy(tracer);
    }

    private void SpawnMuzzleFlash()
    {
        if (muzzleFlashEffect != null && firePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashEffect, firePoint.position, firePoint.rotation, firePoint);
            Destroy(flash, 0.1f);
        }
    }

    private void EjectShell()
    {
        if (shellCasingPrefab == null || shellEjectPoint == null) return;

        GameObject shell = Instantiate(shellCasingPrefab, shellEjectPoint.position, shellEjectPoint.rotation);
        Rigidbody shellRb = shell.GetComponent<Rigidbody>();

        if (shellRb != null)
        {
            Vector3 ejectDirection = shellEjectPoint.right + Vector3.up * 0.5f + Random.insideUnitSphere * 0.1f;
            shellRb.AddForce(ejectDirection * shellEjectForce, ForceMode.Impulse);
            shellRb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }

        Destroy(shell, 5f);
    }

    private void PlayFireSound()
    {
        if (audioSource == null || fireSounds == null || fireSounds.Length == 0) return;

        AudioClip clip = fireSounds[Random.Range(0, fireSounds.Length)];
        audioSource.PlayOneShot(clip);
    }

    private void PlayEmptySound()
    {
        if (audioSource != null && emptySound != null)
            audioSource.PlayOneShot(emptySound);
    }

    private System.Collections.IEnumerator Reload()
    {
        isReloading = true;

        if (animator != null)
            animator.SetTrigger("Reload");

        if (audioSource != null && reloadSound != null)
            audioSource.PlayOneShot(reloadSound);

        if (debugLogs) Debug.Log($"[{name}] Перезарядка...");

        yield return new WaitForSeconds(reloadTime);

        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;

        if (debugLogs) Debug.Log($"[{name}] Перезарядка завершена. Патроны: {currentAmmo}/{reserveAmmo}");
    }

    private void HandleRecoilRecovery()
    {
        currentRecoil = Mathf.Lerp(currentRecoil, targetRecoil, Time.deltaTime * recoilSnapSpeed);
        targetRecoil = Mathf.Lerp(targetRecoil, 0f, Time.deltaTime * recoilRecoverySpeed);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(-currentRecoil, 0f, 0f) * playerCamera.transform.localRotation;
        }
    }

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;
}
