using System.Collections;
using UnityEngine;

public class ShotgunWeapon : MonoBehaviour, IAmmoMagazine, IWeaponSwitchable
{
    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = false;

    [Header("Точки на оружии")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform shellEjectPoint;
    [SerializeField] private Camera playerCamera;

    [Header("Стрельба — дробь")]
    [Tooltip("Сколько снарядов (дробинок) вылетает за один выстрел")]
    [SerializeField] private int pelletsPerShot = 8;
    [Tooltip("Угол разброса дроби в градусах (по горизонтали и вертикали от центра прицела)")]
    [SerializeField] private float spreadAngle = 4f;
    [Tooltip("Урон от ОДНОЙ дробинки. Общий урон при полном попадании = damagePerPellet * pelletsPerShot")]
    [SerializeField] private int damagePerPellet = 20;
    [SerializeField] private float range = 25f;
    [Tooltip("Выстрелов в секунду (для помпового — обычно 1-1.5)")]
    [SerializeField] private float fireRate = 1.2f;
    [SerializeField] private LayerMask hitMask;

    [Header("Падение урона с дистанцией (опционально)")]
    [Tooltip("Если включено — урон дробинки падает с расстоянием по кривой ниже")]
    [SerializeField] private bool useDamageFalloff = true;
    [Tooltip("X = доля пройденной дистанции (0..1 от range), Y = множитель урона (1 = полный урон)")]
    [SerializeField] private AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.25f);

    [Header("Боезапас")]
    [SerializeField] private int magazineSize = 8;
    private int currentAmmo;
    private bool isReloading;
    private Coroutine reloadCoroutine;

    [Header("Перезарядка — вся обойма разом")]
    [SerializeField] private float reloadTime = 1.6f;
    [SerializeField] private string reloadAnimTrigger = "Reload";
    [SerializeField] private AudioClip reloadSound;

    [Header("Визуальная пуля (трассер)")]
    [SerializeField] private GameObject bulletTracerPrefab;
    [SerializeField] private float tracerSpeed = 200f;

    [Header("Эффекты выстрела")]
    [SerializeField] private GameObject muzzleFlashEffect;
    [SerializeField] private GameObject shellCasingPrefab;
    [SerializeField] private float shellEjectForce = 2f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] fireSounds;
    [SerializeField] private AudioClip emptySound;

    [Header("Эффекты попадания")]
    [SerializeField] private GameObject defaultImpactEffect;
    [SerializeField] private GameObject enemyImpactEffect;
    [SerializeField] private float impactEffectLifetime = 2f;

    [Header("Отдача камеры")]
    [Tooltip("У дробовика отдача обычно заметно сильнее, чем у автомата")]
    [SerializeField] private float recoilAmount = 4f;
    [SerializeField] private float recoilRecoverySpeed = 6f;
    [SerializeField] private float recoilSnapSpeed = 18f;
    private float currentRecoil;
    private float targetRecoil;

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string fireAnimTrigger = "Fire";

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

    private void LateUpdate()
    {
        HandleRecoilRecovery();
    }

    private void HandleInput()
    {
        if (isReloading) return;

        if (Input.GetMouseButtonDown(0))
        {
            TryFire();
        }

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineSize)
        {
            reloadCoroutine = StartCoroutine(Reload());
        }
    }

    private void TryFire()
    {
        if (fireCooldown > 0f) return;

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

    private void Fire()
    {
        currentAmmo--;

        if (animator != null && !string.IsNullOrEmpty(fireAnimTrigger))
            animator.SetTrigger(fireAnimTrigger);

        PlayFireSound();
        SpawnMuzzleFlash();
        EjectShell();

        targetRecoil += recoilAmount;

        Vector3 origin = playerCamera.transform.position;
        Vector3 baseDirection = playerCamera.transform.forward;

        for (int i = 0; i < pelletsPerShot; i++)
        {
            Vector3 pelletDirection = ApplySpread(baseDirection);

            // Raycast остаётся мгновенным — так дробь точно попадает туда, куда целился игрок
            // в момент выстрела (иначе за время полёта трассера цель могла бы сдвинуться,
            // и попадание перестало бы совпадать с тем, что видит игрок).
            if (Physics.Raycast(origin, pelletDirection, out RaycastHit hit, range, hitMask))
            {
                // Эффект/урон от этого попадания больше НЕ применяется сразу —
                // он передаётся в SpawnTracer и сработает только когда трассер долетит.
                SpawnTracer(hit.point, hit);
            }
            else
            {
                SpawnTracer(origin + pelletDirection * range, null);
            }
        }

        if (debugLogs)
            Debug.Log($"[{name}] Выстрел дробью: {pelletsPerShot} дробинок, патронов осталось {currentAmmo}");
    }

    private Vector3 ApplySpread(Vector3 direction)
    {
        float randomX = Random.Range(-spreadAngle, spreadAngle);
        float randomY = Random.Range(-spreadAngle, spreadAngle);
        Quaternion spreadRotation = Quaternion.Euler(randomY, randomX, 0f);
        return spreadRotation * direction;
    }

    /// <summary>
    /// Применяет урон, спавнит эффект попадания и импульс от удара.
    /// Вызывается НЕ в момент выстрела, а в момент, когда трассер визуально долетел до цели
    /// (см. DelayedImpact).
    /// </summary>
    private void HandleHit(RaycastHit hit)
    {
        int finalDamage = damagePerPellet;

        if (useDamageFalloff)
        {
            float distanceFraction = Mathf.Clamp01(hit.distance / range);
            float multiplier = damageFalloff.Evaluate(distanceFraction);
            finalDamage = Mathf.RoundToInt(damagePerPellet * multiplier);
        }

        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(finalDamage);
            DamageNumberSpawner.Instance?.Spawn(hit.point, finalDamage);
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
            hit.rigidbody.AddForceAtPosition(-hit.normal * 3f, hit.point, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Спавнит трассер и, если было попадание, запускает отложенный вызов HandleHit —
    /// ровно на то время, за которое трассер долетит до точки попадания.
    /// </summary>
    private void SpawnTracer(Vector3 targetPoint, RaycastHit? hit)
    {
        if (bulletTracerPrefab == null || firePoint == null)
        {
            // Нет визуального трассера — ждать нечего, применяем эффект попадания сразу.
            if (hit.HasValue)
                HandleHit(hit.Value);
            return;
        }

        GameObject tracer = Instantiate(bulletTracerPrefab, firePoint.position, Quaternion.LookRotation(targetPoint - firePoint.position));

        TrailRenderer trail = tracer.GetComponent<TrailRenderer>();
        if (trail != null)
            trail.Clear();

        TracerProjectile projectile = tracer.AddComponent<TracerProjectile>();
        projectile.Launch(targetPoint, tracerSpeed, null);

        if (hit.HasValue)
        {
            float distance = Vector3.Distance(firePoint.position, targetPoint);
            float travelTime = tracerSpeed > 0f ? distance / tracerSpeed : 0f;
            StartCoroutine(DelayedImpact(hit.Value, travelTime));
        }
    }

    /// <summary>
    /// Ждёт время полёта трассера, затем применяет урон/эффект попадания.
    /// </summary>
    private IEnumerator DelayedImpact(RaycastHit hit, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        HandleHit(hit);
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
        audioSource.PlayOneShot(fireSounds[Random.Range(0, fireSounds.Length)]);
    }

    private void PlayEmptySound()
    {
        if (audioSource != null && emptySound != null)
            audioSource.PlayOneShot(emptySound);
    }

    private IEnumerator Reload()
    {
        isReloading = true;

        if (animator != null && !string.IsNullOrEmpty(reloadAnimTrigger))
            animator.SetTrigger(reloadAnimTrigger);

        if (audioSource != null && reloadSound != null)
            audioSource.PlayOneShot(reloadSound);

        if (debugLogs) Debug.Log($"[{name}] Перезарядка обоймы...");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;

        if (debugLogs) Debug.Log($"[{name}] Перезарядка завершена: {currentAmmo}/{magazineSize}");
    }

    private float appliedRecoil;

    private void HandleRecoilRecovery()
    {
        currentRecoil = Mathf.Lerp(currentRecoil, targetRecoil, Time.deltaTime * recoilSnapSpeed);
        targetRecoil = Mathf.Lerp(targetRecoil, 0f, Time.deltaTime * recoilRecoverySpeed);

        if (playerCamera != null)
        {
            float delta = currentRecoil - appliedRecoil;
            playerCamera.transform.localRotation = Quaternion.Euler(-delta, 0f, 0f) * playerCamera.transform.localRotation;
            appliedRecoil = currentRecoil;
        }
    }

    // Реализация IAmmoMagazine
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public bool IsReloading => isReloading;
    public bool CanFire => !isReloading && currentAmmo > 0;

    // Реализация IWeaponSwitchable
    public void OnEquip()
    {
        fireCooldown = 0f;

        if (animator != null)
        {
            animator.ResetTrigger(fireAnimTrigger);
            animator.ResetTrigger(reloadAnimTrigger);
        }
    }

    public void OnUnequip()
    {
        if (isReloading)
        {
            if (reloadCoroutine != null)
                StopCoroutine(reloadCoroutine);

            isReloading = false;

            if (debugLogs)
                Debug.Log($"[{name}] Перезарядка прервана сменой оружия. Патроны остались: {currentAmmo}/{magazineSize}");
        }

        if (animator != null && !string.IsNullOrEmpty(idleStateName))
        {
            animator.ResetTrigger(fireAnimTrigger);
            animator.ResetTrigger(reloadAnimTrigger);
            animator.Play(idleStateName, 0, 0f);
            animator.Update(0f);
        }
    }
}
