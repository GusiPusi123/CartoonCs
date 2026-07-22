using UnityEngine;

public class WeaponRaycast : MonoBehaviour
{
    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = false;

    [Header("Точки на оружии")]
    [SerializeField] private Transform firePoint; // дуло — откуда летит визуальная пуля и рейкаст
    [SerializeField] private Transform shellEjectPoint; // точка вылета гильзы (необязательно)
    [SerializeField] private Camera playerCamera; // камера игрока — рейкаст обычно стреляет из центра экрана, а не из дула

    [Header("Стрельба")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 8f; // выстрелов в секунду
    [SerializeField] private bool isAutomatic = true; // зажатие ЛКМ или клик каждый раз
    [SerializeField] private LayerMask hitMask; // что может быть поражено (не забудь включить слои Enemy и Environment)

    [Header("Разброс (Spread)")]
    [SerializeField] private float baseSpread = 0.5f; // угол разброса в градусах в состоянии покоя
    [SerializeField] private float maxSpread = 4f; // максимальный разброс при долгой стрельбе
    [SerializeField] private float spreadIncreasePerShot = 0.3f; // насколько растёт разброс с каждым выстрелом
    [SerializeField] private float spreadRecoverySpeed = 3f; // как быстро разброс восстанавливается после отпускания кнопки
    private float currentSpread;

    [Header("Боезапас")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int reserveAmmo = 90;
    [SerializeField] private float reloadTime = 1.8f;
    private int currentAmmo;
    private bool isReloading;

    [Header("Визуальная пуля (трассер)")]
    [SerializeField] private GameObject bulletTracerPrefab; // объект с TrailRenderer или простой цилиндр/спрайт
    [SerializeField] private float tracerSpeed = 150f; // скорость полёта визуальной пули к точке попадания
    [SerializeField] private bool useInstantTracer = false; // true = трассер сразу линия до цели, false = летящий объект

    [Header("Эффекты выстрела")]
    [SerializeField] private GameObject muzzleFlashEffect;
    [SerializeField] private GameObject shellCasingPrefab;
    [SerializeField] private float shellEjectForce = 2f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] fireSounds; // массив — можно чередовать звуки для вариативности
    [SerializeField] private AudioClip emptySound; // клик при пустом магазине
    [SerializeField] private AudioClip reloadSound;

    [Header("Эффекты попадания")]
    [SerializeField] private GameObject defaultImpactEffect; // искры/пыль по умолчанию (стены, пол)
    [SerializeField] private GameObject enemyImpactEffect; // кровь/особый эффект при попадании во врага
    [SerializeField] private float impactEffectLifetime = 2f;

    [Header("Отдача камеры")]
    [SerializeField] private float recoilAmount = 1.5f; // на сколько градусов "подбрасывает" камеру за выстрел
    [SerializeField] private float recoilRecoverySpeed = 8f;
    private float currentRecoil;
    private float targetRecoil;

    [Header("Анимация")]
    [SerializeField] private Animator animator; // триггеры "Fire", "Reload"

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
        HandleSpreadRecovery();
        HandleRecoilRecovery();
    }

    private void HandleInput()
    {
        if (isReloading) return;

        bool wantsToFire = isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");

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
                fireCooldown = 0.3f; // защита от спама клика на пустом магазине
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
        Vector3 direction = ApplySpread(playerCamera.transform.forward);

        if (animator != null)
            animator.SetTrigger("Fire");

        PlayFireSound();
        SpawnMuzzleFlash();
        EjectShell();

        // Увеличиваем разброс и отдачу с каждым выстрелом
        currentSpread = Mathf.Min(currentSpread + spreadIncreasePerShot, maxSpread);
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
            // Ничего не задели — трассер летит в пустоту на максимальную дальность
            Vector3 missPoint = origin + direction * range;
            SpawnTracer(missPoint);

            if (debugLogs)
                Debug.Log($"[{name}] Промах, выстрел ушёл в пустоту");
        }
    }

    private Vector3 ApplySpread(Vector3 baseDirection)
    {
        float totalSpread = baseSpread + currentSpread;

        float spreadX = Random.Range(-totalSpread, totalSpread);
        float spreadY = Random.Range(-totalSpread, totalSpread);

        Quaternion spreadRotation = Quaternion.Euler(spreadY, spreadX, 0f);
        return spreadRotation * baseDirection;
    }

    private void HandleHit(RaycastHit hit)
    {
        // Наносим урон, если попали во что-то с IDamageable
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }

        // Разные эффекты попадания — по врагу или по окружению
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

        // Физический импульс в объект, если у него есть Rigidbody (для лёгких предметов/тряпичных кукол)
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
            // Мгновенная линия — просто растягиваем LineRenderer, если он есть на префабе
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

    private void HandleSpreadRecovery()
    {
        if (currentSpread > 0f)
        {
            currentSpread = Mathf.Max(0f, currentSpread - spreadRecoverySpeed * Time.deltaTime);
        }
    }

    private void HandleRecoilRecovery()
    {
        // Плавно "подбрасываем" камеру к targetRecoil, потом плавно возвращаем обратно
        currentRecoil = Mathf.Lerp(currentRecoil, targetRecoil, Time.deltaTime * 20f);
        targetRecoil = Mathf.Lerp(targetRecoil, 0f, Time.deltaTime * recoilRecoverySpeed);

        if (playerCamera != null)
        {
            // Применяем небольшой локальный поворот вверх — если у тебя есть отдельный скрипт Look(),
            // этот recoil стоит либо суммировать с ним, либо применять на дочерний объект камеры, а не сам transform
            playerCamera.transform.localRotation = Quaternion.Euler(-currentRecoil, 0f, 0f) * playerCamera.transform.localRotation;
        }
    }

    // Публичные геттеры для UI (счётчик патронов, индикатор перезарядки)
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;
}