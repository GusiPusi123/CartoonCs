// using UnityEngine;

// public class WeaponShot : MonoBehaviour
// {
//     public Camera shooterCamera;
//     public float range = 100f;
//     public float fireRate = 0.5f;
//     public int maxAmmo = 30;
//     public float reloadTime = 2f;

//     public GameObject muzzleFlashEffect;
//     public Transform muzzleEffectPoint;
//     public GameObject hitEffectPrefab;
//     public Transform hitEffectPoint;
//     public AudioClip gunshotSound;
//     public TrailRenderer tracerTrail;
//     public Transform traceStartPoint;
//     public Animator weaponAnimator;

//     private int currentAmmo;
//     private float nextFireTime = 0f;
//     private bool isReloading = false;
//     private AudioSource audioSource;

//     void Start()
//     {
//         currentAmmo = maxAmmo;
//         audioSource = GetComponent<AudioSource>();
//         if (tracerTrail != null)
//             tracerTrail.enabled = false;
//     }

//     void Update()
//     {
//         if (Input.GetMouseButton(0) && Time.time >= nextFireTime && !isReloading)
//         {
//             if (currentAmmo > 0)
//             {
//                 Shoot();
//                 nextFireTime = Time.time + fireRate;
//             }
//             else
//             {
//                 Debug.Log("Нет патронов! Нажмите R для перезарядки.");
//             }
//         }

//         if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < maxAmmo)
//         {
//             StartCoroutine(Reload());
//         }
//     }

//     void Shoot()
//     {
//         if (weaponAnimator != null)
//             weaponAnimator.SetTrigger("Shoot");
//         if (gunshotSound != null)
//             audioSource.PlayOneShot(gunshotSound);
//         if (muzzleFlashEffect != null && muzzleEffectPoint != null)
//         {
//             GameObject muzzleEffect = Instantiate(muzzleFlashEffect, muzzleEffectPoint.position, muzzleEffectPoint.rotation);
//             Destroy(muzzleEffect, 1f);
//         }
//         if (tracerTrail != null && traceStartPoint != null)
//             StartCoroutine(ShootTracer());

//         // Направление без разброс
//         Vector3 shootDirection = shooterCamera.transform.forward;

//         // В этом варианте разброс полностью удален
//         Vector3 direction = shootDirection;

//         RaycastHit hit;
//         Ray ray = new Ray(shooterCamera.transform.position, direction);
//         Vector3 targetPoint = ray.origin + ray.direction * range;

//         if (Physics.Raycast(ray, out hit, range))
//         {
//             Debug.Log("Попадание в: " + hit.collider.name);
//             if (hitEffectPrefab != null)
//             {
//                 GameObject hitEffect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
//                 Destroy(hitEffect, 2f);
//             }
//         }
//         else
//         {
//             Debug.Log("Промах");
//         }

//         currentAmmo--;
//         Debug.Log("Патроны: " + currentAmmo + "/" + maxAmmo);
//     }

//     System.Collections.IEnumerator Reload()
//     {
//         isReloading = true;
//         Debug.Log("Перезарядка...");
//         if (weaponAnimator != null)
//         {
//             weaponAnimator.SetTrigger("Reload");
//         }
//         yield return new WaitForSeconds(reloadTime);
//         currentAmmo = maxAmmo;
//         isReloading = false;
//         Debug.Log("Перезарядка завершена");
//     }

//     System.Collections.IEnumerator ShootTracer()
//     {
//         tracerTrail.enabled = true;
//         tracerTrail.Clear();

//         if (traceStartPoint != null)
//         {
//             tracerTrail.transform.position = traceStartPoint.position;
//             tracerTrail.transform.rotation = traceStartPoint.rotation;
//         }
//         else
//         {
//             tracerTrail.transform.position = shooterCamera.transform.position;
//             tracerTrail.transform.rotation = shooterCamera.transform.rotation;
//         }

//         // Направление без разброс
//         Vector3 direction = shooterCamera.transform.forward;

//         Ray ray = new Ray(shooterCamera.transform.position, direction);
//         Vector3 targetPoint;

//         if (Physics.Raycast(ray, out var hit, range))
//         {
//             targetPoint = hit.point;
//         }
//         else
//         {
//             targetPoint = ray.origin + ray.direction * range;
//         }

//         float elapsedTime = 0f;
//         float duration = 0.05f;

//         Vector3 startPosition = tracerTrail.transform.position;
//         Vector3 endPosition = targetPoint;

//         while (elapsedTime < duration)
//         {
//             tracerTrail.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
//             elapsedTime += Time.deltaTime;
//             yield return null;
//         }

//         tracerTrail.transform.position = endPosition;
//         yield return new WaitForSeconds(0.01f);
//         tracerTrail.enabled = false;
//     }
// }
