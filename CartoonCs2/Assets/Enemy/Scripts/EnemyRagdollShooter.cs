// using UnityEngine;
// using UnityEngine.AI;

// /// <summary>
// /// Стреляющий враг с рэгдоллом при смерти.
// /// Рука (handBone) доворачивается на цель отдельно от анимации тела,
// /// firePoint (обычно дочерний объект handBone/оружия) используется как точка вылета пули.
// /// Поддерживает хитскан и снаряд (projectile), бонусный урон в голову через EnemyHitbox,
// /// и опциональное преследование игрока.
// /// </summary>
// [RequireComponent(typeof(NavMeshAgent))]
// public class EnemyRagdollShooter : MonoBehaviour, IDamageable
// {
//     [Header("Диагностика")]
//     [SerializeField] private bool debugLogs = true;

//     [Header("Здоровье")]
//     [SerializeField] private int maxHealth = 80;
//     [Tooltip("Множитель урона при попадании в хитбоксу с пометкой 'голова' (см. EnemyHitbox)")]
//     [SerializeField] private float headshotMultiplier = 2.5f;
//     private int currentHealth;
//     private bool isDead;

//     [Header("Цель")]
//     [SerializeField] private Transform player;
//     [SerializeField] private Transform aimTarget;
//     [SerializeField] private float aimHeightOffset = 1f;

//     [Header("Рука / прицеливание (независимо от анимации тела)")]
//     [Tooltip("Кость/объект руки, которая держит оружие. К ней обычно прикреплён firePoint")]
//     [SerializeField] private Transform handBone;
//     [Tooltip("Какая локальная ось кости руки должна 'смотреть' на цель. Если рука целится не туда — поменяй эту ось")]
//     [SerializeField] private Vector3 handForwardAxis = Vector3.forward;
//     [Tooltip("Насколько сильно рука доворачивается к цели поверх анимации: 0 — рука полностью анимирована, 1 — рука жёстко смотрит на игрока")]
//     [SerializeField, Range(0f, 1f)] private float handAimWeight = 1f;
//     [Tooltip("Скорость доворота руки к цели (чем больше, тем резче)")]
//     [SerializeField] private float handAimSpeed = 15f;

//     // Собственное "накопленное" вращение прицела руки, не зависящее от того,
//     // что Animator записал в handBone.rotation в этом кадре. Раньше сглаживание
//     // бралось от handBone.rotation ПОСЛЕ применения анимации (Update -> Animator -> LateUpdate),
//     // из-за чего во время роликов вроде "Shoot" рука дёргалась к позе анимации
//     // и заново доворачивалась на цель. Теперь сглаживаем независимое состояние
//     // и только в конце примешиваем его к текущей позе анимации через handAimWeight.
//     private Quaternion currentHandAimRotation;
//     private bool handAimInitialized;

//     [Header("Поведение / преследование")]
//     [SerializeField] private float detectionRadius = 20f;
//     [Tooltip("Если выключено — враг не будет подходить к игроку, только разворачивается и стреляет с места")]
//     [SerializeField] private bool canApproachPlayer = true;
//     [SerializeField] private float preferredDistance = 8f;
//     [SerializeField] private float retreatDistance = 4f;
//     [SerializeField] private float moveSpeed = 3f;
//     [SerializeField] private float pathUpdateRate = 0.2f;
//     [SerializeField] private float navMeshSampleRadius = 5f;

//     [Header("Стрельба")]
//     [SerializeField] private Transform firePoint;
//     [SerializeField] private LayerMask lineOfSightMask;
//     [SerializeField] private LayerMask hitMask;
//     [SerializeField] private float fireRate = 1.5f;
//     [SerializeField] private float shootRange = 15f;
//     [SerializeField] private int hitscanDamage = 10;

//     [Header("Тип атаки")]
//     [SerializeField] private bool useProjectile = false;
//     [Tooltip("Пуля врага (нужен Rigidbody и, желательно, компонент Projectile, как в старом EnemyShooter)")]
//     [SerializeField] private GameObject projectilePrefab;
//     [SerializeField] private float projectileSpeed = 20f;

//     [Header("Звуки")]
//     [SerializeField] private AudioSource audioSource;
//     [SerializeField] private AudioClip[] shootSounds;
//     [SerializeField] private AudioClip hitSound;
//     [SerializeField] private AudioClip deathSound;
//     [SerializeField] private GameObject muzzleFlashEffect;

//     [Header("Анимация")]
//     [SerializeField] private Animator animator;
//     [Tooltip("Имя float-параметра в Animator Controller для скорости бега (0 = стоит, 1 = бежит с полной скоростью agent.speed)")]
//     [SerializeField] private string speedParam = "Speed";
//     [Tooltip("Как быстро параметр Speed сглаживается к реальной скорости агента (0 = мгновенно)")]
//     [SerializeField] private float speedSmoothTime = 0.1f;
//     private float currentAnimSpeed;
//     private float animSpeedVelocity;

//     [Header("Мигание при уроне")]
//     [SerializeField] private Color flashColor = Color.white;
//     [SerializeField] private float flashDuration = 0.15f;

//     [Header("Рэгдолл")]
//     [Tooltip("Все Rigidbody костей рэгдолла (кроме корня персонажа). Должны быть isKinematic = true, пока враг жив")]
//     [SerializeField] private Rigidbody[] ragdollRigidbodies;
//     [Tooltip("Соответствующие коллайдеры костей рэгдолла. Должны быть выключены, пока враг жив")]
//     [SerializeField] private Collider[] ragdollColliders;
//     [Tooltip("Rigidbody, к которому применяется сила отдачи, если попадание пришло не через EnemyHitbox (например хитскан по основному коллайдеру)")]
//     [SerializeField] private Rigidbody defaultRagdollHitBody;
//     [Tooltip("Основной коллайдер персонажа (обычно CapsuleCollider для NavMeshAgent). Выключается при смерти")]
//     [SerializeField] private Collider mainCollider;
//     [SerializeField] private float ragdollForce = 8f;
//     [SerializeField] private float ragdollUpwardForce = 2f;
//     [SerializeField] private float destroyDelay = 8f;

//     private NavMeshAgent agent;
//     private float pathUpdateTimer;
//     private float fireCooldown;
//     private bool hasLineOfSight;

//     private Renderer[] renderers;
//     private Color[] originalColors;
//     private Coroutine flashCoroutine;

//     private void Awake()
//     {
//         agent = GetComponent<NavMeshAgent>();
//         agent.speed = moveSpeed;
//         currentHealth = maxHealth;

//         renderers = GetComponentsInChildren<Renderer>();
//         originalColors = new Color[renderers.Length];
//         for (int i = 0; i < renderers.Length; i++)
//             originalColors[i] = renderers[i].material.color;

//         // Пока враг жив — рэгдолл выключен: кости управляются анимацией, а не физикой.
//         SetRagdollActive(false);
//     }

//     private void Start()
//     {
//         if (player == null)
//         {
//             GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
//             if (playerObj != null)
//                 player = playerObj.transform;
//         }

//         if (firePoint == null)
//             firePoint = handBone != null ? handBone : transform;

//         if (debugLogs)
//         {
//             if (player == null)
//                 Debug.LogError($"[{name}] Игрок не найден! Проверь тег 'Player'.");
//             if (handBone == null)
//                 Debug.LogWarning($"[{name}] handBone не задан — рука не будет доворачиваться на игрока отдельно от анимации.");
//             if (ragdollRigidbodies == null || ragdollRigidbodies.Length == 0)
//                 Debug.LogWarning($"[{name}] Не заданы ragdollRigidbodies — при смерти рэгдолл не сработает, враг просто исчезнет.");
//         }
//     }

//     private void Update()
//     {
//         if (isDead || player == null) return;

//         if (!agent.isOnNavMesh)
//         {
//             if (debugLogs) Debug.LogWarning($"[{name}] Агент не на NavMesh!");
//             return;
//         }

//         UpdateMoveAnimation();

//         float distanceToPlayer = Vector3.Distance(transform.position, player.position);

//         if (distanceToPlayer <= detectionRadius)
//         {
//             CheckLineOfSight();
//             HandleMovement(distanceToPlayer);
//             HandleShooting(distanceToPlayer);
//         }
//         else
//         {
//             agent.ResetPath();
//         }
//     }

//     // Доворот руки к цели делаем в LateUpdate, чтобы это применилось ПОСЛЕ того,
//     // как Animator в этом кадре уже выставил позу тела — иначе анимация тут же перезапишет поворот руки.
//     private void LateUpdate()
//     {
//         if (isDead || handBone == null || player == null) return;

//         Vector3 aimDir = (GetAimPoint() - handBone.position).normalized;
//         if (aimDir == Vector3.zero) return;

//         // Разворачиваем так, чтобы handForwardAxis кости совпала с направлением на цель.
//         Quaternion axisRemap = Quaternion.FromToRotation(handForwardAxis, Vector3.forward);
//         Quaternion targetRotation = Quaternion.LookRotation(aimDir, transform.up) * axisRemap;

//         if (!handAimInitialized)
//         {
//             currentHandAimRotation = handBone.rotation;
//             handAimInitialized = true;
//         }

//         // Сглаживаем СВОЁ накопленное состояние прицела, а не handBone.rotation —
//         // тот уже мог быть перезаписан анимацией (например, роликом "Shoot") в этом кадре.
//         // Так прицел не "дёргается" к позе анимации при каждом выстреле.
//         currentHandAimRotation = Quaternion.Slerp(currentHandAimRotation, targetRotation, Time.deltaTime * handAimSpeed);

//         // handAimWeight решает, насколько сильно наш прицел перебивает текущую позу анимации.
//         handBone.rotation = Quaternion.Slerp(handBone.rotation, currentHandAimRotation, handAimWeight);
//     }

//     private Vector3 GetAimPoint()
//     {
//         if (aimTarget != null)
//             return aimTarget.position;

//         return player.position + Vector3.up * aimHeightOffset;
//     }

//     private void CheckLineOfSight()
//     {
//         Vector3 origin = firePoint != null ? firePoint.position : transform.position;
//         Vector3 aimPoint = GetAimPoint();
//         Vector3 direction = (aimPoint - origin).normalized;
//         float distance = Vector3.Distance(origin, aimPoint);

//         bool blocked = Physics.Raycast(origin, direction, out _, distance, lineOfSightMask);
//         hasLineOfSight = !blocked;

//         if (debugLogs)
//             Debug.DrawLine(origin, aimPoint, hasLineOfSight ? Color.green : Color.red, pathUpdateRate);
//     }

//     private void HandleMovement(float distanceToPlayer)
//     {
//         pathUpdateTimer -= Time.deltaTime;
//         if (pathUpdateTimer > 0f) return;
//         pathUpdateTimer = pathUpdateRate;

//         // Если преследование выключено — враг вообще не двигается, только стоит и стреляет/поворачивает руку.
//         if (!canApproachPlayer)
//         {
//             agent.ResetPath();
//             return;
//         }

//         if (distanceToPlayer < retreatDistance)
//         {
//             Vector3 retreatDir = (transform.position - player.position).normalized;
//             Vector3 retreatPoint = transform.position + retreatDir * (retreatDistance - distanceToPlayer + 1f);

//             if (NavMesh.SamplePosition(retreatPoint, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
//                 agent.SetDestination(hit.position);
//         }
//         else if (distanceToPlayer > preferredDistance || !hasLineOfSight)
//         {
//             if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
//                 agent.SetDestination(hit.position);
//         }
//         else
//         {
//             agent.ResetPath();
//         }
//     }

//     private void HandleShooting(float distanceToPlayer)
//     {
//         fireCooldown -= Time.deltaTime;

//         if (distanceToPlayer > shootRange) return;
//         if (!hasLineOfSight) return;
//         if (fireCooldown > 0f) return;

//         Shoot();
//         fireCooldown = 1f / fireRate;
//     }

//     private void Shoot()
//     {
//         if (debugLogs) Debug.Log($"[{name}] ВЫСТРЕЛ!");

//         // Разворот тела к игроку (только по горизонтали) — рука при этом уже смотрит на цель через LateUpdate.
//         Vector3 lookDir = player.position - transform.position;
//         lookDir.y = 0f;
//         if (lookDir != Vector3.zero)
//             transform.rotation = Quaternion.LookRotation(lookDir);

//         PlayShootSound();

//         if (muzzleFlashEffect != null && firePoint != null)
//             Instantiate(muzzleFlashEffect, firePoint.position, firePoint.rotation);

//         if (useProjectile)
//             ShootProjectile();
//         else
//             ShootHitscan();
//     }

//     private void PlayShootSound()
//     {
//         if (audioSource == null || shootSounds == null || shootSounds.Length == 0) return;
//         AudioClip clip = shootSounds[Random.Range(0, shootSounds.Length)];
//         audioSource.PlayOneShot(clip);
//     }

//     private void ShootHitscan()
//     {
//         Vector3 targetPoint = GetAimPoint();
//         Vector3 origin = firePoint.position;
//         Vector3 direction = (targetPoint - origin).normalized;

//         if (Physics.Raycast(origin, direction, out RaycastHit hit, shootRange, hitMask))
//         {
//             if (debugLogs) Debug.Log($"[{name}] Хитскан попал в: {hit.collider.name}");

//             if (hit.collider.CompareTag("Player"))
//             {
//                 PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
//                 if (playerHealth != null)
//                     playerHealth.TakeDamage(hitscanDamage);
//                 else if (debugLogs)
//                     Debug.LogWarning($"[{name}] На игроке нет компонента PlayerHealth!");
//             }
//         }
//         else if (debugLogs)
//         {
//             Debug.Log($"[{name}] Хитскан ни во что не попал");
//         }
//     }

//     private void ShootProjectile()
//     {
//         if (projectilePrefab == null)
//         {
//             if (debugLogs) Debug.LogWarning($"[{name}] projectilePrefab не назначен!");
//             return;
//         }

//         Vector3 targetPoint = GetAimPoint();
//         Vector3 direction = (targetPoint - firePoint.position).normalized;

//         GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
//         Rigidbody rb = projectile.GetComponent<Rigidbody>();

//         if (rb == null)
//         {
//             if (debugLogs) Debug.LogWarning($"[{name}] На снаряде нет Rigidbody!");
//             return;
//         }

//         rb.isKinematic = false;
//         rb.useGravity = false;
//         rb.velocity = direction * projectileSpeed;
//     }

//     // ---------- Анимация движения ----------

//     /// <summary>
//     /// Обновляет float-параметр аниматора (по умолчанию "Speed") на основе реальной
//     /// скорости NavMeshAgent, нормализованной к [0..1] относительно agent.speed.
//     /// Настрой Blend Tree/переходы Idle-Run в Animator Controller по этому параметру.
//     /// </summary>
//     private void UpdateMoveAnimation()
//     {
//         if (animator == null || string.IsNullOrEmpty(speedParam)) return;

//         float targetSpeed = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f);
//         currentAnimSpeed = Mathf.SmoothDamp(currentAnimSpeed, targetSpeed, ref animSpeedVelocity, speedSmoothTime);
//         animator.SetFloat(speedParam, currentAnimSpeed);
//     }

//     // ---------- Получение урона ----------

//     // Реализация IDamageable — используется, когда пуля/оружие попадает напрямую
//     // в основной коллайдер (не через EnemyHitbox), т.е. как обычный, не-хедшотный урон.
//     public void TakeDamage(int damageAmount)
//     {
//         ReceiveHit(damageAmount, false, defaultRagdollHitBody, transform.position);
//     }

//     /// <summary>
//     /// Вызывается EnemyHitbox (или напрямую), когда во врага попали.
//     /// isHeadshot — попадание в хитбоксу, помеченную как голова (см. EnemyHitbox.isHeadshot).
//     /// hitRigidbody — конкретное тело рэгдолла, к которому нужно приложить силу при смерти (может быть null).
//     /// hitPoint — мировая точка попадания, используется для направления отдачи.
//     /// </summary>
//     public void ReceiveHit(int baseDamage, bool isHeadshot, Rigidbody hitRigidbody, Vector3 hitPoint)
//     {
//         if (isDead) return;

//         int finalDamage = isHeadshot ? Mathf.RoundToInt(baseDamage * headshotMultiplier) : baseDamage;
//         currentHealth -= finalDamage;

//         if (debugLogs)
//             Debug.Log($"[{name}] Получено {finalDamage} урона (headshot: {isHeadshot}). Осталось HP: {currentHealth}");

//         if (audioSource != null && hitSound != null)
//             audioSource.PlayOneShot(hitSound);

//         if (flashCoroutine != null)
//             StopCoroutine(flashCoroutine);
//         flashCoroutine = StartCoroutine(FlashWhite());

//         if (currentHealth <= 0)
//             Die(hitRigidbody, hitPoint);
//     }

//     private System.Collections.IEnumerator FlashWhite()
//     {
//         for (int i = 0; i < renderers.Length; i++)
//             if (renderers[i] != null) renderers[i].material.color = flashColor;

//         yield return new WaitForSeconds(flashDuration);

//         for (int i = 0; i < renderers.Length; i++)
//             if (renderers[i] != null) renderers[i].material.color = originalColors[i];

//         flashCoroutine = null;
//     }

//     // ---------- Смерть и рэгдолл ----------

//     private void Die(Rigidbody hitRigidbody, Vector3 hitPoint)
//     {
//         isDead = true;

//         if (agent != null)
//         {
//             agent.isStopped = true;
//             agent.enabled = false;
//         }

//         if (audioSource != null && deathSound != null)
//             audioSource.PlayOneShot(deathSound);

//         SetRagdollActive(true);

//         // Направление отдачи — от игрока (источник выстрела) к точке попадания.
//         // Это приближение: WeaponRaycast сейчас не передаёт точную точку выстрела,
//         // поэтому считаем, что стреляли примерно с позиции игрока.
//         Vector3 knockDir = (hitPoint - player.position).normalized;
//         if (knockDir == Vector3.zero) knockDir = -transform.forward;

//         Rigidbody targetBody = hitRigidbody != null ? hitRigidbody : defaultRagdollHitBody;
//         if (targetBody != null)
//         {
//             Vector3 force = knockDir * ragdollForce + Vector3.up * ragdollUpwardForce;
//             targetBody.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);
//         }
//         else if (debugLogs)
//         {
//             Debug.LogWarning($"[{name}] Нет rigidbody для применения силы рэгдолла (задай defaultRagdollHitBody).");
//         }

//         Destroy(gameObject, destroyDelay);
//     }

//     private void SetRagdollActive(bool active)
//     {
//         if (animator != null)
//             animator.enabled = !active;

//         if (mainCollider != null)
//             mainCollider.enabled = !active;

//         // Важно: коллайдеры костей (ragdollColliders) НЕ выключаются здесь.
//         // Они должны быть включены всегда — иначе EnemyHitbox на голове/теле
//         // не будет регистрировать попадания, пока враг жив (Physics.Raycast
//         // игнорирует выключенные коллайдеры). Переключаем только isKinematic:
//         // true (пока жив) — кость следует за анимацией, false (при смерти) — физика берёт управление.
//         if (ragdollRigidbodies != null)
//             foreach (Rigidbody rb in ragdollRigidbodies)
//                 if (rb != null) rb.isKinematic = !active;
//     }

//     private void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.red;
//         Gizmos.DrawWireSphere(transform.position, detectionRadius);

//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(transform.position, preferredDistance);

//         Gizmos.color = Color.magenta;
//         Gizmos.DrawWireSphere(transform.position, retreatDistance);

//         if (aimTarget != null)
//         {
//             Gizmos.color = Color.cyan;
//             Gizmos.DrawWireSphere(aimTarget.position, 0.15f);
//         }
//     }
// }

using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Стреляющий враг с рэгдоллом при смерти.
/// Рука (handBone) доворачивается на цель отдельно от анимации тела,
/// firePoint (обычно дочерний объект handBone/оружия) используется как точка вылета пули.
/// Поддерживает хитскан и снаряд (projectile), бонусный урон в голову через EnemyHitbox,
/// и опциональное преследование игрока.
/// Корпус постоянно доворачивается к игроку (не только в момент выстрела) — см. RotateTowardsPlayer().
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRagdollShooter : MonoBehaviour, IDamageable
{
    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = true;

    [Header("Здоровье")]
    [SerializeField] private int maxHealth = 80;
    [Tooltip("Множитель урона при попадании в хитбоксу с пометкой 'голова' (см. EnemyHitbox)")]
    [SerializeField] private float headshotMultiplier = 2.5f;
    private int currentHealth;
    private bool isDead;

    [Header("Цель")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform aimTarget;
    [SerializeField] private float aimHeightOffset = 1f;

    [Header("Рука / прицеливание (независимо от анимации тела)")]
    [Tooltip("Кость/объект руки, которая держит оружие. К ней обычно прикреплён firePoint")]
    [SerializeField] private Transform handBone;
    [Tooltip("Какая локальная ось кости руки должна 'смотреть' на цель. Если рука целится не туда — поменяй эту ось")]
    [SerializeField] private Vector3 handForwardAxis = Vector3.forward;
    [Tooltip("Насколько сильно рука доворачивается к цели поверх анимации: 0 — рука полностью анимирована, 1 — рука жёстко смотрит на игрока")]
    [SerializeField, Range(0f, 1f)] private float handAimWeight = 1f;
    [Tooltip("Скорость доворота руки к цели (чем больше, тем резче)")]
    [SerializeField] private float handAimSpeed = 15f;

    // Собственное "накопленное" вращение прицела руки, не зависящее от того,
    // что Animator записал в handBone.rotation в этом кадре. Раньше сглаживание
    // бралось от handBone.rotation ПОСЛЕ применения анимации (Update -> Animator -> LateUpdate),
    // из-за чего во время роликов вроде "Shoot" рука дёргалась к позе анимации
    // и заново доворачивалась на цель. Теперь сглаживаем независимое состояние
    // и только в конце примешиваем его к текущей позе анимации через handAimWeight.
    private Quaternion currentHandAimRotation;
    private bool handAimInitialized;

    [Header("Поведение / преследование")]
    [SerializeField] private float detectionRadius = 20f;
    [Tooltip("Если выключено — враг не будет подходить к игроку, только разворачивается и стреляет с места")]
    [SerializeField] private bool canApproachPlayer = true;
    [SerializeField] private float preferredDistance = 8f;
    [SerializeField] private float retreatDistance = 4f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float pathUpdateRate = 0.2f;
    [SerializeField] private float navMeshSampleRadius = 5f;

    [Header("Поворот корпуса")]
    [Tooltip("Скорость доворота корпуса лицом к игроку (множитель для Slerp, чем больше — тем резче). " +
             "Работает постоянно, а не только во время выстрела — раньше именно из-за этого враг мог " +
             "стоять боком: тело поворачивалось только внутри Shoot(), а NavMeshAgent крутил объект " +
             "по направлению движения, а не по направлению на игрока.")]
    [SerializeField] private float bodyTurnSpeed = 8f;
    [Tooltip("Необязательно. Пустой дочерний объект, поставленный там, куда ФИЗИЧЕСКИ смотрит модель " +
             "(перед лицом персонажа). Нужен, если визуальный перед модели не совпадает с transform.forward — " +
             "именно из-за этого несовпадения при прямом повороте на игрока персонаж 'крутится'/смотрит боком. " +
             "Поставь пустышку прямо перед лицом модели и перетащи её сюда. Если не задан — используется " +
             "обычный transform.forward, как будто перед модели совпадает с осью Z объекта.")]
    [SerializeField] private Transform bodyFrontMarker;

    // Локальное направление 'переда' модели, посчитанное один раз в Start() по позиции bodyFrontMarker.
    // Хранится в локальных координатах объекта, поэтому не зависит от текущего поворота transform
    // и не даёт эффекта "накручивания" на каждом кадре.
    private Vector3 bodyFrontLocalDir = Vector3.forward;

    [Header("Стрельба")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask lineOfSightMask;
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private float shootRange = 15f;
    [SerializeField] private int hitscanDamage = 10;

    [Header("Тип атаки")]
    [SerializeField] private bool useProjectile = false;
    [Tooltip("Пуля врага (нужен Rigidbody и, желательно, компонент Projectile, как в старом EnemyShooter)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;

    [Header("Звуки")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] shootSounds;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private GameObject muzzleFlashEffect;

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [Tooltip("Имя float-параметра в Animator Controller для скорости бега (0 = стоит, 1 = бежит с полной скоростью agent.speed)")]
    [SerializeField] private string speedParam = "Speed";
    [Tooltip("Как быстро параметр Speed сглаживается к реальной скорости агента (0 = мгновенно)")]
    [SerializeField] private float speedSmoothTime = 0.1f;
    private float currentAnimSpeed;
    private float animSpeedVelocity;

    [Header("Мигание при уроне")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.15f;

    [Header("Рэгдолл")]
    [Tooltip("Все Rigidbody костей рэгдолла (кроме корня персонажа). Должны быть isKinematic = true, пока враг жив")]
    [SerializeField] private Rigidbody[] ragdollRigidbodies;
    [Tooltip("Соответствующие коллайдеры костей рэгдолла. Должны быть выключены, пока враг жив")]
    [SerializeField] private Collider[] ragdollColliders;
    [Tooltip("Rigidbody, к которому применяется сила отдачи, если попадание пришло не через EnemyHitbox (например хитскан по основному коллайдеру)")]
    [SerializeField] private Rigidbody defaultRagdollHitBody;
    [Tooltip("Основной коллайдер персонажа (обычно CapsuleCollider для NavMeshAgent). Выключается при смерти")]
    [SerializeField] private Collider mainCollider;
    [SerializeField] private float ragdollForce = 8f;
    [SerializeField] private float ragdollUpwardForce = 2f;
    [SerializeField] private float destroyDelay = 8f;

    private NavMeshAgent agent;
    private float pathUpdateTimer;
    private float fireCooldown;
    private bool hasLineOfSight;

    private Renderer[] renderers;
    private Color[] originalColors;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        // Поворотом корпуса теперь управляем сами (RotateTowardsPlayer), а не NavMeshAgent —
        // иначе агент крутит объект по направлению движения, а не по направлению на игрока,
        // и когда враг стоит на месте (например, на preferredDistance), поворот вообще не происходит.
        agent.updateRotation = false;
        currentHealth = maxHealth;

        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;

        // Пока враг жив — рэгдолл выключен: кости управляются анимацией, а не физикой.
        SetRagdollActive(false);
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (firePoint == null)
            firePoint = handBone != null ? handBone : transform;

        // Считаем локальное направление "переда" модели один раз по позиции bodyFrontMarker.
        // Дальше используем именно его, а не world-позицию маркера — иначе при повороте объекта
        // маркер (как дочерний transform) тоже вращается вместе с ним, и постоянная погоня
        // за его текущей world-позицией даёт эффект "накручивания"/дрожания.
        if (bodyFrontMarker != null)
        {
            Vector3 localOffset = transform.InverseTransformPoint(bodyFrontMarker.position);
            localOffset.y = 0f;
            if (localOffset.sqrMagnitude > 0.0001f)
                bodyFrontLocalDir = localOffset.normalized;
            else if (debugLogs)
                Debug.LogWarning($"[{name}] bodyFrontMarker стоит слишком близко к центру объекта — отодвинь его вперёд по горизонтали.");
        }

        if (debugLogs)
        {
            if (player == null)
                Debug.LogError($"[{name}] Игрок не найден! Проверь тег 'Player'.");
            if (handBone == null)
                Debug.LogWarning($"[{name}] handBone не задан — рука не будет доворачиваться на игрока отдельно от анимации.");
            if (ragdollRigidbodies == null || ragdollRigidbodies.Length == 0)
                Debug.LogWarning($"[{name}] Не заданы ragdollRigidbodies — при смерти рэгдолл не сработает, враг просто исчезнет.");
        }
    }

    private void Update()
    {
        if (isDead || player == null) return;

        if (!agent.isOnNavMesh)
        {
            if (debugLogs) Debug.LogWarning($"[{name}] Агент не на NavMesh!");
            return;
        }

        UpdateMoveAnimation();

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            CheckLineOfSight();
            HandleMovement(distanceToPlayer);
            HandleShooting(distanceToPlayer);

            // Доворачиваем корпус каждый кадр, а не только в момент выстрела —
            // раньше именно из-за этого враг мог стоять к игроку боком.
            RotateTowardsPlayer();
        }
        else
        {
            agent.ResetPath();
        }
    }

    /// <summary>
    /// Плавно поворачивает корпус (только по горизонтали, вокруг Y) так, чтобы "перёд" модели
    /// (см. bodyFrontMarker / bodyFrontLocalDir) смотрел на игрока. Если bodyFrontMarker не задан,
    /// перед считается совпадающим с transform.forward — поведение как раньше.
    /// Вызывается каждый кадр, пока игрок в радиусе обнаружения — не только во время выстрела,
    /// поэтому враг больше не "застревает" боком, когда стоит на месте между выстрелами
    /// или когда canApproachPlayer = false.
    /// </summary>
    private void RotateTowardsPlayer()
    {
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.0001f) return;

        // axisRemap перед этим "перекладывает" локальный перед модели на локальную ось Z,
        // а LookRotation дальше поворачивает эту ось Z в сторону игрока. В сумме — именно
        // "перёд" модели (а не абстрактный transform.forward) оказывается направлен на игрока.
        Quaternion axisRemap = Quaternion.FromToRotation(bodyFrontLocalDir, Vector3.forward);
        Quaternion targetRotation = Quaternion.LookRotation(lookDir) * axisRemap;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * bodyTurnSpeed);
    }

    // Доворот руки к цели делаем в LateUpdate, чтобы это применилось ПОСЛЕ того,
    // как Animator в этом кадре уже выставил позу тела — иначе анимация тут же перезапишет поворот руки.
    private void LateUpdate()
    {
        if (isDead || handBone == null || player == null) return;

        Vector3 aimDir = (GetAimPoint() - handBone.position).normalized;
        if (aimDir == Vector3.zero) return;

        // Разворачиваем так, чтобы handForwardAxis кости совпала с направлением на цель.
        Quaternion axisRemap = Quaternion.FromToRotation(handForwardAxis, Vector3.forward);
        Quaternion targetRotation = Quaternion.LookRotation(aimDir, transform.up) * axisRemap;

        if (!handAimInitialized)
        {
            currentHandAimRotation = handBone.rotation;
            handAimInitialized = true;
        }

        // Сглаживаем СВОЁ накопленное состояние прицела, а не handBone.rotation —
        // тот уже мог быть перезаписан анимацией (например, роликом "Shoot") в этом кадре.
        // Так прицел не "дёргается" к позе анимации при каждом выстреле.
        currentHandAimRotation = Quaternion.Slerp(currentHandAimRotation, targetRotation, Time.deltaTime * handAimSpeed);

        // handAimWeight решает, насколько сильно наш прицел перебивает текущую позу анимации.
        handBone.rotation = Quaternion.Slerp(handBone.rotation, currentHandAimRotation, handAimWeight);
    }

    private Vector3 GetAimPoint()
    {
        if (aimTarget != null)
            return aimTarget.position;

        return player.position + Vector3.up * aimHeightOffset;
    }

    private void CheckLineOfSight()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 aimPoint = GetAimPoint();
        Vector3 direction = (aimPoint - origin).normalized;
        float distance = Vector3.Distance(origin, aimPoint);

        bool blocked = Physics.Raycast(origin, direction, out _, distance, lineOfSightMask);
        hasLineOfSight = !blocked;

        if (debugLogs)
            Debug.DrawLine(origin, aimPoint, hasLineOfSight ? Color.green : Color.red, pathUpdateRate);
    }

    private void HandleMovement(float distanceToPlayer)
    {
        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer > 0f) return;
        pathUpdateTimer = pathUpdateRate;

        // Если преследование выключено — враг вообще не двигается, только стоит и стреляет/поворачивает руку.
        if (!canApproachPlayer)
        {
            agent.ResetPath();
            return;
        }

        if (distanceToPlayer < retreatDistance)
        {
            Vector3 retreatDir = (transform.position - player.position).normalized;
            Vector3 retreatPoint = transform.position + retreatDir * (retreatDistance - distanceToPlayer + 1f);

            if (NavMesh.SamplePosition(retreatPoint, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
        else if (distanceToPlayer > preferredDistance || !hasLineOfSight)
        {
            if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
        else
        {
            agent.ResetPath();
        }
    }

    private void HandleShooting(float distanceToPlayer)
    {
        fireCooldown -= Time.deltaTime;

        if (distanceToPlayer > shootRange) return;
        if (!hasLineOfSight) return;
        if (fireCooldown > 0f) return;

        Shoot();
        fireCooldown = 1f / fireRate;
    }

    private void Shoot()
    {
        if (debugLogs) Debug.Log($"[{name}] ВЫСТРЕЛ!");

        // Поворот корпуса теперь делается каждый кадр в RotateTowardsPlayer() (см. Update),
        // поэтому здесь его дублировать не нужно — тело и так уже смотрит на игрока.

        PlayShootSound();

        if (muzzleFlashEffect != null && firePoint != null)
            Instantiate(muzzleFlashEffect, firePoint.position, firePoint.rotation);

        if (useProjectile)
            ShootProjectile();
        else
            ShootHitscan();
    }

    private void PlayShootSound()
    {
        if (audioSource == null || shootSounds == null || shootSounds.Length == 0) return;
        AudioClip clip = shootSounds[Random.Range(0, shootSounds.Length)];
        audioSource.PlayOneShot(clip);
    }

    private void ShootHitscan()
    {
        Vector3 targetPoint = GetAimPoint();
        Vector3 origin = firePoint.position;
        Vector3 direction = (targetPoint - origin).normalized;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, shootRange, hitMask))
        {
            if (debugLogs) Debug.Log($"[{name}] Хитскан попал в: {hit.collider.name}");

            if (hit.collider.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(hitscanDamage);
                else if (debugLogs)
                    Debug.LogWarning($"[{name}] На игроке нет компонента PlayerHealth!");
            }
        }
        else if (debugLogs)
        {
            Debug.Log($"[{name}] Хитскан ни во что не попал");
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null)
        {
            if (debugLogs) Debug.LogWarning($"[{name}] projectilePrefab не назначен!");
            return;
        }

        Vector3 targetPoint = GetAimPoint();
        Vector3 direction = (targetPoint - firePoint.position).normalized;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb == null)
        {
            if (debugLogs) Debug.LogWarning($"[{name}] На снаряде нет Rigidbody!");
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.velocity = direction * projectileSpeed;
    }

    // ---------- Анимация движения ----------

    /// <summary>
    /// Обновляет float-параметр аниматора (по умолчанию "Speed") на основе реальной
    /// скорости NavMeshAgent, нормализованной к [0..1] относительно agent.speed.
    /// Настрой Blend Tree/переходы Idle-Run в Animator Controller по этому параметру.
    /// </summary>
    private void UpdateMoveAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(speedParam)) return;

        float targetSpeed = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f);
        currentAnimSpeed = Mathf.SmoothDamp(currentAnimSpeed, targetSpeed, ref animSpeedVelocity, speedSmoothTime);
        animator.SetFloat(speedParam, currentAnimSpeed);
    }

    // ---------- Получение урона ----------

    // Реализация IDamageable — используется, когда пуля/оружие попадает напрямую
    // в основной коллайдер (не через EnemyHitbox), т.е. как обычный, не-хедшотный урон.
    public void TakeDamage(int damageAmount)
    {
        ReceiveHit(damageAmount, false, defaultRagdollHitBody, transform.position);
    }

    /// <summary>
    /// Вызывается EnemyHitbox (или напрямую), когда во врага попали.
    /// isHeadshot — попадание в хитбоксу, помеченную как голова (см. EnemyHitbox.isHeadshot).
    /// hitRigidbody — конкретное тело рэгдолла, к которому нужно приложить силу при смерти (может быть null).
    /// hitPoint — мировая точка попадания, используется для направления отдачи.
    /// </summary>
    public void ReceiveHit(int baseDamage, bool isHeadshot, Rigidbody hitRigidbody, Vector3 hitPoint)
    {
        if (isDead) return;

        int finalDamage = isHeadshot ? Mathf.RoundToInt(baseDamage * headshotMultiplier) : baseDamage;
        currentHealth -= finalDamage;

        if (debugLogs)
            Debug.Log($"[{name}] Получено {finalDamage} урона (headshot: {isHeadshot}). Осталось HP: {currentHealth}");

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashWhite());

        if (currentHealth <= 0)
            Die(hitRigidbody, hitPoint);
    }

    private System.Collections.IEnumerator FlashWhite()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].material.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].material.color = originalColors[i];

        flashCoroutine = null;
    }

    // ---------- Смерть и рэгдолл ----------

    private void Die(Rigidbody hitRigidbody, Vector3 hitPoint)
    {
        isDead = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        SetRagdollActive(true);

        // Направление отдачи — от игрока (источник выстрела) к точке попадания.
        // Это приближение: WeaponRaycast сейчас не передаёт точную точку выстрела,
        // поэтому считаем, что стреляли примерно с позиции игрока.
        Vector3 knockDir = (hitPoint - player.position).normalized;
        if (knockDir == Vector3.zero) knockDir = -transform.forward;

        Rigidbody targetBody = hitRigidbody != null ? hitRigidbody : defaultRagdollHitBody;
        if (targetBody != null)
        {
            Vector3 force = knockDir * ragdollForce + Vector3.up * ragdollUpwardForce;
            targetBody.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);
        }
        else if (debugLogs)
        {
            Debug.LogWarning($"[{name}] Нет rigidbody для применения силы рэгдолла (задай defaultRagdollHitBody).");
        }

        Destroy(gameObject, destroyDelay);
    }

    private void SetRagdollActive(bool active)
    {
        if (animator != null)
            animator.enabled = !active;

        if (mainCollider != null)
            mainCollider.enabled = !active;

        // Важно: коллайдеры костей (ragdollColliders) НЕ выключаются здесь.
        // Они должны быть включены всегда — иначе EnemyHitbox на голове/теле
        // не будет регистрировать попадания, пока враг жив (Physics.Raycast
        // игнорирует выключенные коллайдеры). Переключаем только isKinematic:
        // true (пока жив) — кость следует за анимацией, false (при смерти) — физика берёт управление.
        if (ragdollRigidbodies != null)
            foreach (Rigidbody rb in ragdollRigidbodies)
                if (rb != null) rb.isKinematic = !active;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);

        if (aimTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(aimTarget.position, 0.15f);
        }
    }
}
