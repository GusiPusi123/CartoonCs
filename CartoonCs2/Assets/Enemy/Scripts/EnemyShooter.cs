using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyShooter : MonoBehaviour, IDamageable
{
    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = true;

    [Header("Здоровье")]
    [SerializeField] private int maxHealth = 80;
    private int currentHealth;

    [Header("Цель")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform aimTarget; // точка прицеливания на игроке (например, в районе груди). Если не задана — используется player.position + aimHeightOffset

    [Header("Высота прицеливания (если aimTarget не задан)")]
    [SerializeField] private float aimHeightOffset = 1f;

    [Header("Дистанция боя")]
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private float preferredDistance = 8f;
    [SerializeField] private float retreatDistance = 4f;
    [SerializeField] private float moveSpeed = 3f;

    [Header("Стрельба")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask lineOfSightMask;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private float shootRange = 15f;

    [Header("Хитскан (только если useProjectile = false)")]
    [SerializeField] private int hitscanDamage = 10;

    [Header("Тип атаки")]
    [SerializeField] private bool useProjectile = false;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;

    [Header("Эффекты")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private GameObject muzzleFlashEffect;
    [SerializeField] private GameObject deathEffect;

    [Header("Мигание при уроне")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 1f;

    [Header("Обновление пути / NavMesh")]
    [SerializeField] private float pathUpdateRate = 0.2f;
    [SerializeField] private float navMeshSampleRadius = 5f;

    private NavMeshAgent agent;
    private float pathUpdateTimer;
    private float fireCooldown;
    private bool isDead;
    private bool hasLineOfSight;

    private Renderer[] renderers;
    private Color[] originalColors;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        currentHealth = maxHealth;

        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
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
            firePoint = transform;

        if (debugLogs)
        {
            if (player == null)
                Debug.LogError($"[{name}] Игрок не найден! Проверь, что на игроке стоит тег 'Player'.");
            else
                Debug.Log($"[{name}] Игрок найден: {player.name}");

            if (aimTarget == null)
                Debug.Log($"[{name}] Aim Target не задан, используется player.position + aimHeightOffset ({aimHeightOffset})");

            if (shootRange < preferredDistance)
                Debug.LogWarning($"[{name}] shootRange ({shootRange}) меньше preferredDistance ({preferredDistance})!");

            if (useProjectile && projectilePrefab != null)
            {
                Rigidbody prefabRb = projectilePrefab.GetComponent<Rigidbody>();
                if (prefabRb == null)
                    Debug.LogError($"[{name}] На префабе снаряда {projectilePrefab.name} нет Rigidbody!");
                else if (prefabRb.isKinematic)
                    Debug.LogWarning($"[{name}] Rigidbody на префабе снаряда помечен Is Kinematic — выключи это на префабе {projectilePrefab.name}.");

                if (projectilePrefab.GetComponent<Projectile>() == null)
                    Debug.LogError($"[{name}] На префабе снаряда {projectilePrefab.name} нет компонента Projectile — урон наноситься не будет!");
            }
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

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            CheckLineOfSight(distanceToPlayer);
            HandleMovement(distanceToPlayer);
            HandleShooting(distanceToPlayer);
        }
        else
        {
            agent.ResetPath();
        }
    }

    // Возвращает точку, в которую враг целится: aimTarget, если задан, иначе позиция игрока + смещение по высоте
    private Vector3 GetAimPoint()
    {
        if (aimTarget != null)
            return aimTarget.position;

        return player.position + Vector3.up * aimHeightOffset;
    }

    private void CheckLineOfSight(float distanceToPlayer)
    {
        Vector3 origin = firePoint.position;
        Vector3 aimPoint = GetAimPoint();
        Vector3 direction = (aimPoint - origin).normalized;
        float distanceToAimPoint = Vector3.Distance(origin, aimPoint);

        bool blocked = Physics.Raycast(origin, direction, out RaycastHit hit, distanceToAimPoint, lineOfSightMask);
        hasLineOfSight = !blocked;

        if (debugLogs)
        {
            Debug.DrawLine(origin, aimPoint, hasLineOfSight ? Color.green : Color.red, pathUpdateRate);
        }
    }

    private void HandleMovement(float distanceToPlayer)
    {
        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer > 0f) return;
        pathUpdateTimer = pathUpdateRate;

        if (distanceToPlayer < retreatDistance)
        {
            Vector3 retreatDirection = (transform.position - player.position).normalized;
            Vector3 retreatPoint = transform.position + retreatDirection * (retreatDistance - distanceToPlayer + 1f);

            if (NavMesh.SamplePosition(retreatPoint, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
        else if (distanceToPlayer > preferredDistance || !hasLineOfSight)
        {
            if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
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

        Vector3 lookDirection = (player.position - transform.position);
        lookDirection.y = 0f;
        if (lookDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDirection);

        if (animator != null)
            animator.SetTrigger("Shoot");

        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        if (muzzleFlashEffect != null)
            Instantiate(muzzleFlashEffect, firePoint.position, firePoint.rotation);

        if (useProjectile)
            ShootProjectile();
        else
            ShootHitscan();
    }

    private void ShootHitscan()
    {
        Vector3 targetPoint = GetAimPoint();
        Vector3 direction = (targetPoint - firePoint.position).normalized;

        if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, shootRange))
        {
            if (debugLogs)
                Debug.Log($"[{name}] Хитскан попал в: {hit.collider.name}");

            if (hit.collider.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(hitscanDamage);
                }
                else if (debugLogs)
                {
                    Debug.LogWarning($"[{name}] На игроке нет компонента PlayerHealth!");
                }
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
            if (debugLogs) Debug.LogWarning($"[{name}] На заспавненном снаряде нет Rigidbody!");
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        rb.velocity = direction * projectileSpeed;

        if (debugLogs)
            Debug.Log($"[{name}] Снаряд создан. Направление: {direction}, velocity: {rb.velocity}");
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (animator != null)
            animator.SetTrigger("Hit");

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashWhite());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator FlashWhite()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }

        flashCoroutine = null;
    }

    private void Die()
    {
        isDead = true;

        agent.isStopped = true;
        agent.enabled = false;

        if (animator != null)
            animator.SetTrigger("Die");

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 2f);
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
