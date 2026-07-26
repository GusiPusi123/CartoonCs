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
    [SerializeField] private int damagePerPellet = 12;
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

    [Header("Перезарядка — по одному снаряду")]
    [Tooltip("Сколько времени занимает вставка ОДНОГО патрона")]
    [SerializeField] private float singleShellReloadTime = 0.55f;
    [SerializeField] private string singleShellAnimTrigger = "ReloadShell";
    [SerializeField] private AudioClip singleShellSound;

    [Header("Перезарядка — вся обойма разом (когда магазин полностью пуст)")]
    [Tooltip("Если магазин ПОЛНОСТЬЮ пуст — вместо цикла по одному патрону играется отдельная анимация полной перезарядки")]
    [SerializeField] private bool useFullReloadWhenEmpty = true;
    [SerializeField] private float fullMagazineReloadTime = 2.2f;
    [SerializeField] private string fullMagazineAnimTrigger = "ReloadFull";
    [SerializeField] private AudioClip fullMagazineSound;

    [Header("Передёргивание после перезарядки по одному (помпа)")]
    [Tooltip("Проигрывается один раз ПОСЛЕ того как вставлены все недостающие патроны в цикле по одному")]
    [SerializeField] private string pumpAnimTrigger = "Pump";
    [SerializeField] private float pumpDuration = 0.35f;
    [SerializeField] private AudioClip pumpSound;

    [Header("Прерывание перезарядки выстрелом")]
    [Tooltip("Если игрок стреляет во время перезарядки по одному патрону — перезарядка прерывается и оружие сразу стреляет тем, что успело зарядиться (классика для помповых дробовиков)")]
    [SerializeField] private bool allowFireToInterruptReload = true;

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
        bool wantsToFire = Input.GetMouseButtonDown(0);

        if (isReloading)
        {
            // Прерывание перезарядки выстрелом (только пока идёт цикл "по одному патрону",
            // при полной перезарядке прерывать нельзя — обойма/магазин ещё физически не на месте)
            if (wantsToFire && allowFireToInterruptReload && currentAmmo > 0 && !currentlyDoingFullReload)
            {
                InterruptReload();
                TryFire();
            }
            return;
        }

        if (wantsToFire)
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

            if (Physics.Raycast(origin, pelletDirection, out RaycastHit hit, range, hitMask))
            {
                HandleHit(hit);
                SpawnTracer(hit.point);
            }
            else
            {
                SpawnTracer(origin + pelletDirection * range);
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

    private void SpawnTracer(Vector3 targetPoint)
    {
        if (bulletTracerPrefab == null || firePoint == null) return;

        GameObject tracer = Instantiate(bulletTracerPrefab, firePoint.position, Quaternion.LookRotation(targetPoint - firePoint.position));
        StartCoroutine(MoveTracer(tracer, targetPoint));
    }

    private IEnumerator MoveTracer(GameObject tracer, Vector3 targetPoint)
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
        audioSource.PlayOneShot(fireSounds[Random.Range(0, fireSounds.Length)]);
    }

    private void PlayEmptySound()
    {
        if (audioSource != null && emptySound != null)
            audioSource.PlayOneShot(emptySound);
    }

    private bool currentlyDoingFullReload;

    private IEnumerator Reload()
    {
        isReloading = true;
        currentlyDoingFullReload = useFullReloadWhenEmpty && currentAmmo == 0;

        if (currentlyDoingFullReload)
        {
            if (animator != null && !string.IsNullOrEmpty(fullMagazineAnimTrigger))
                animator.SetTrigger(fullMagazineAnimTrigger);

            if (audioSource != null && fullMagazineSound != null)
                audioSource.PlayOneShot(fullMagazineSound);

            if (debugLogs) Debug.Log($"[{name}] Полная перезарядка обоймы...");

            yield return new WaitForSeconds(fullMagazineReloadTime);

            currentAmmo = magazineSize;
        }
        else
        {
            while (currentAmmo < magazineSize)
            {
                if (animator != null && !string.IsNullOrEmpty(singleShellAnimTrigger))
                    animator.SetTrigger(singleShellAnimTrigger);

                if (audioSource != null && singleShellSound != null)
                    audioSource.PlayOneShot(singleShellSound);

                if (debugLogs) Debug.Log($"[{name}] Вставка патрона {currentAmmo + 1}/{magazineSize}...");

                yield return new WaitForSeconds(singleShellReloadTime);

                currentAmmo++;
            }

            if (!string.IsNullOrEmpty(pumpAnimTrigger))
            {
                if (animator != null)
                    animator.SetTrigger(pumpAnimTrigger);

                if (audioSource != null && pumpSound != null)
                    audioSource.PlayOneShot(pumpSound);

                yield return new WaitForSeconds(pumpDuration);
            }
        }

        isReloading = false;
        currentlyDoingFullReload = false;

        if (debugLogs) Debug.Log($"[{name}] Перезарядка завершена: {currentAmmo}/{magazineSize}");
    }

    private void InterruptReload()
    {
        if (reloadCoroutine != null)
            StopCoroutine(reloadCoroutine);

        isReloading = false;
        currentlyDoingFullReload = false;

        if (animator != null)
        {
            animator.ResetTrigger(singleShellAnimTrigger);
            animator.ResetTrigger(pumpAnimTrigger);
        }

        if (debugLogs)
            Debug.Log($"[{name}] Перезарядка прервана выстрелом. Патронов сейчас: {currentAmmo}/{magazineSize}");
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
            animator.ResetTrigger(singleShellAnimTrigger);
            animator.ResetTrigger(fullMagazineAnimTrigger);
            animator.ResetTrigger(pumpAnimTrigger);
        }
    }

    public void OnUnequip()
    {
        if (isReloading)
        {
            if (reloadCoroutine != null)
                StopCoroutine(reloadCoroutine);

            isReloading = false;
            currentlyDoingFullReload = false;

            if (debugLogs)
                Debug.Log($"[{name}] Перезарядка прервана сменой оружия. Патроны остались: {currentAmmo}/{magazineSize}");
        }

        if (animator != null && !string.IsNullOrEmpty(idleStateName))
        {
            animator.ResetTrigger(fireAnimTrigger);
            animator.ResetTrigger(singleShellAnimTrigger);
            animator.ResetTrigger(fullMagazineAnimTrigger);
            animator.ResetTrigger(pumpAnimTrigger);
            animator.Play(idleStateName, 0, 0f);
            animator.Update(0f);
        }
    }
}
