using UnityEngine;

public class WeaponRaycast : MonoBehaviour
{
    public Camera shooterCamera; // Камера для выстрела
    public float range = 100f; // Дальность
    public float fireRate = 0.5f; // Время между выстрелами
    public int maxAmmo = 30; // Максимальный магазин
    public float reloadTime = 2f; // Время перезарядки

    public GameObject muzzleFlashEffect; // Эффект вспышки у дульного среза
    public Transform muzzleEffectPoint; // Точка появления эффекта у дульного среза
    public GameObject hitEffectPrefab; // Эффект при попадании
    public Transform hitEffectPoint; // Точка для эффекта попадания

    public AudioClip gunshotSound; // Звук выстрела
    public TrailRenderer tracerTrail; // Трайл рендерер для трассировки
    public Transform traceStartPoint; // Точка вылета трассировки

    private int currentAmmo;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private AudioSource audioSource;

    void Start()
    {
        currentAmmo = maxAmmo;
        audioSource = GetComponent<AudioSource>();
        if (tracerTrail != null)
        {
            tracerTrail.enabled = false; // Отключить по умолчанию
        }
    }

    void Update()
    {
        if (shooterCamera == null) return;

        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime && !isReloading)
        {
            if (currentAmmo > 0)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
            else
            {
                Debug.Log("Нет патронов! Нажмите R для перезарядки.");
            }
        }
    }

    void Shoot()
    {
        // Звук выстрела
        if (gunshotSound != null)
        {
            audioSource.PlayOneShot(gunshotSound);
        }

        // Эффект вспышки у дульного среза
        if (muzzleFlashEffect != null && muzzleEffectPoint != null)
        {
            GameObject muzzleEffect = Instantiate(muzzleFlashEffect, muzzleEffectPoint.position, muzzleEffectPoint.rotation);
            Destroy(muzzleEffect, 1f);
        }

        // Трасса
        if (tracerTrail != null && traceStartPoint != null)
        {
            StartCoroutine(ShootTracer());
        }

        // Луч
        RaycastHit hit;
        Ray ray = new Ray(shooterCamera.transform.position, shooterCamera.transform.forward);
        Vector3 targetPoint = ray.origin + ray.direction * range;

        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log("Попадание в: " + hit.collider.name);

            // Эффект попадания
            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(hitEffect, 2f);
            }
        }
        else
        {
            Debug.Log("Промах");
        }

        currentAmmo--;
        Debug.Log("Патроны: " + currentAmmo + "/" + maxAmmo);
    }

    System.Collections.IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Перезарядка...");
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("Перезарядка завершена");
    }

    System.Collections.IEnumerator ShootTracer()
    {
        tracerTrail.enabled = true;
        tracerTrail.Clear();

        // Устанавливаем стартовую точку трассировки
        if (traceStartPoint != null)
        {
            tracerTrail.transform.position = traceStartPoint.position;
            tracerTrail.transform.rotation = traceStartPoint.rotation;
        }
        else
        {
            // Если точки вылета нет, используем позицию камеры
            tracerTrail.transform.position = shooterCamera.transform.position;
            tracerTrail.transform.rotation = shooterCamera.transform.rotation;
        }
        RaycastHit hit;
        Ray ray = new Ray(shooterCamera.transform.position, shooterCamera.transform.forward);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, range))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * range;
        }

        float elapsedTime = 0f;
        float duration = 0.05f; // Время отображения трассы

        Vector3 startPosition = tracerTrail.transform.position;
        Vector3 endPosition = targetPoint;

        while (elapsedTime < duration)
        {
            tracerTrail.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        tracerTrail.transform.position = endPosition;
        yield return new WaitForSeconds(0.01f);

        tracerTrail.enabled = false;
    }
}