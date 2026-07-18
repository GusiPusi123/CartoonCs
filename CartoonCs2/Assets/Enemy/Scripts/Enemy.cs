// using UnityEngine;
// using UnityEngine.AI; // Не забудьте импортировать NavMesh

// public class Enemy : MonoBehaviour
// {
//     public float health = 100f;
//     public float glowDuration = 1f; // Время свечения при получении урона

//     private Transform playerTransform;
//     private Renderer enemyRenderer;
//     private Color originalColor;
//     private NavMeshAgent navMeshAgent;

//     private bool isGlowing = false;
//     private float glowEndTime;

//     void Start()
//     {
//         // Найти игрока по тегу "Player"
//         GameObject player = GameObject.FindGameObjectWithTag("Player");
//         if (player != null)
//         {
//             playerTransform = player.transform;
//         }
//         else
//         {
//             Debug.LogWarning("Игрок не найден. Убедитесь, что у объекта есть тег 'Player'.");
//         }

//         // Получить Renderer для изменения цвета
//         enemyRenderer = GetComponent<Renderer>();
//         if (enemyRenderer != null)
//         {
//             originalColor = enemyRenderer.material.color;
//         }
//         else
//         {
//             Debug.LogWarning("Renderer не найден у врага.");
//         }

//         // Получить NavMeshAgent
//         navMeshAgent = GetComponent<NavMeshAgent>();
//         if (navMeshAgent == null)
//         {
//             Debug.LogError("Отсутствует компонент NavMeshAgent. Добавьте его к врагу.");
//         }
//     }

//     void Update()
//     {
//         // Следовать за игроком с помощью NavMeshAgent
//         if (playerTransform != null && navMeshAgent != null)
//         {
//             navMeshAgent.SetDestination(playerTransform.position);
//         }

//         // Управление свечением
//         if (isGlowing && Time.time >= glowEndTime)
//         {
//             StopGlow();
//         }
//     }

//     public void TakeDamage(float damage)
//     {
//         health -= damage;
//         Debug.Log("Enemy took damage, current health: " + health);
//         if (health <= 0)
//         {
//             Die();
//         }
//         else
//         {
//             StartGlow();
//         }
//     }

//     void Die()
//     {
//         Destroy(gameObject);
//     }

//     private void StartGlow()
//     {
//         if (enemyRenderer != null)
//         {
//             enemyRenderer.material.color = Color.white;
//             isGlowing = true;
//             glowEndTime = Time.time + glowDuration;
//         }
//     }

//     private void StopGlow()
//     {
//         if (enemyRenderer != null)
//         {
//             enemyRenderer.material.color = originalColor;
//             isGlowing = false;
//         }
//     }
// }








// using System.Collections;
// using UnityEngine;
// using UnityEngine.AI;
 
// [RequireComponent(typeof(Renderer))]
// [RequireComponent(typeof(NavMeshAgent))]
// public class Enemy : MonoBehaviour
// {
//     [Header("Следование за игроком (через NavMesh)")]
//     [Tooltip("Если не указан, скрипт найдёт объект с тегом Player")]
//     public Transform player;
//     public float moveSpeed = 3f;
//     public float stoppingDistance = 1.5f;
 
//     [Tooltip("Как часто (в секундах) пересчитывать путь до игрока. 0 = каждый кадр")]
//     public float pathUpdateInterval = 0.2f;
 
//     [Tooltip("Радиус поиска ближайшей точки NavMesh рядом с игроком (если игрок стоит вне навигационной сетки)")]
//     public float navMeshSampleRadius = 2f;
 
//     private NavMeshAgent agent;
//     private float pathUpdateTimer;
 
//     [Header("Здоровье и мигание при уроне")]
//     public float maxHealth = 100f;
//     private float currentHealth;
 
//     public Color flashColor = Color.white;
//     public float flashDuration = 1f;
 
//     private Renderer[] renderers;
//     private Color[] originalColors;
//     private Coroutine flashCoroutine;
 
//     // Каждый Renderer может иметь несколько материалов, поэтому храним пары
//     private Material[][] materialsPerRenderer;
 
//     void Awake()
//     {
//         if (player == null)
//         {
//             GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
//             if (playerObj != null)
//                 player = playerObj.transform;
//         }
 
//         currentHealth = maxHealth;
 
//         agent = GetComponent<NavMeshAgent>();
//         agent.speed = moveSpeed;
//         agent.stoppingDistance = stoppingDistance;
 
//         // Собираем все рендереры (включая дочерние объекты, если модель состоит из частей)
//         renderers = GetComponentsInChildren<Renderer>();
//         materialsPerRenderer = new Material[renderers.Length][];
//         originalColors = new Color[renderers.Length];
 
//         for (int i = 0; i < renderers.Length; i++)
//         {
//             // Используем .material (создаёт инстанс), чтобы не менять цвет у всех объектов с этим материалом
//             materialsPerRenderer[i] = renderers[i].materials;
//             if (materialsPerRenderer[i].Length > 0 && materialsPerRenderer[i][0].HasProperty("_Color"))
//             {
//                 originalColors[i] = materialsPerRenderer[i][0].color;
//             }
//         }
//     }
 
//     void Update()
//     {
//         if (player == null) return;
 
//         FollowPlayer();
//     }
 
//     void FollowPlayer()
//     {
//         // Пересчитываем путь не каждый кадр, а с интервалом — это дешевле для производительности
//         pathUpdateTimer -= Time.deltaTime;
//         if (pathUpdateTimer <= 0f)
//         {
//             UpdateDestination();
//             pathUpdateTimer = pathUpdateInterval;
//         }
//     }
 
//     void UpdateDestination()
//     {
//         NavMeshPath path = new NavMeshPath();
//         bool pathFound = agent.CalculatePath(player.position, path);
 
//         if (pathFound && path.status == NavMeshPathStatus.PathComplete)
//         {
//             // Игрок полностью достижим — идём прямо к нему
//             agent.SetPath(path);
//         }
//         else if (path.corners.Length > 0)
//         {
//             // Игрок недостижим (например, стоит на возвышенности без NavMesh) —
//             // подходим максимально близко, к последней точке доступного пути
//             agent.SetPath(path);
//         }
//         else
//         {
//             // Пути вообще нет — пробуем найти ближайшую валидную точку рядом с игроком
//             if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
//             {
//                 agent.SetDestination(hit.position);
//             }
//         }
//     }
 
//     // Вызывайте этот метод из скрипта атаки игрока, например: enemy.TakeDamage(10f);
//     public void TakeDamage(float damageAmount)
//     {
//         currentHealth -= damageAmount;
 
//         if (flashCoroutine != null)
//             StopCoroutine(flashCoroutine);
 
//         flashCoroutine = StartCoroutine(FlashWhite());
 
//         if (currentHealth <= 0)
//         {
//             Die();
//         }
//     }
 
//     IEnumerator FlashWhite()
//     {
//         // Устанавливаем цвет вспышки на всех материалах
//         for (int i = 0; i < renderers.Length; i++)
//         {
//             foreach (Material mat in materialsPerRenderer[i])
//             {
//                 if (mat.HasProperty("_Color"))
//                     mat.color = flashColor;
//             }
//         }
 
//         yield return new WaitForSeconds(flashDuration);
 
//         // Возвращаем оригинальный цвет
//         for (int i = 0; i < renderers.Length; i++)
//         {
//             foreach (Material mat in materialsPerRenderer[i])
//             {
//                 if (mat.HasProperty("_Color"))
//                     mat.color = originalColors[i];
//             }
//         }
 
//         flashCoroutine = null;
//     }
 
//     void Die()
//     {
//         // Здесь можно добавить анимацию смерти, звук, эффекты и т.д.
//         Destroy(gameObject);
//     }
// }


using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Цель")]
    [SerializeField] private Transform player;

    [Header("Параметры преследования")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float detectionRadius = 15f; // на каком расстоянии враг замечает игрока
    [SerializeField] private float stoppingDistance = 1.5f; // на каком расстоянии останавливается

    [Header("Обновление пути")]
    [SerializeField] private float pathUpdateRate = 0.2f; // как часто пересчитывать путь (сек)

    [Header("Обработка позиции вне NavMesh")]
    [SerializeField] private float navMeshSampleRadius = 5f; // радиус поиска ближайшей точки NavMesh
    [SerializeField] private float loseTargetTime = 3f; // через сколько секунд враг "теряет" игрока, если тот вне досягаемости
    private float timeSinceLastValidPath;

    [Header("Реакция на урон")]
    [SerializeField] private Animator animator; // если есть триггеры "Hit" / "Die"
    [SerializeField] private GameObject deathEffect; // партиклы/эффект смерти (необязательно)
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;

    private NavMeshAgent agent;
    private float pathUpdateTimer;
    private bool isDead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.stoppingDistance = stoppingDistance;

        currentHealth = maxHealth;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (isDead || player == null) return;

        // Если агент сейчас не на NavMesh (провалился, застрял) — ничего не делаем
        if (!agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            pathUpdateTimer -= Time.deltaTime;
            if (pathUpdateTimer <= 0f)
            {
                UpdateChaseTarget();
                pathUpdateTimer = pathUpdateRate;
            }
        }
        else
        {
            agent.ResetPath();
        }
    }

    private void UpdateChaseTarget()
    {
        // Ищем ближайшую валидную точку NavMesh рядом с игроком
        // (работает, даже если игрок прыгнул на ящик, крышу, вылез за пределы сетки)
        if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            timeSinceLastValidPath = 0f;
        }
        else
        {
            // Игрок слишком далеко от NavMesh — увеличиваем таймер
            timeSinceLastValidPath += pathUpdateRate;

            if (timeSinceLastValidPath >= loseTargetTime)
            {
                // Враг остаётся на последней известной точке и просто ждёт
                // Можно добавить сюда состояние "потерял игрока" (патрулирование и т.п.)
            }
        }
    }

    // Вызывается из LaserSword.cs: enemyComponent.TakeDamage(damage);
    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (animator != null)
            animator.SetTrigger("Hit");

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        // Останавливаем движение
        agent.isStopped = true;
        agent.enabled = false;

        if (animator != null)
            animator.SetTrigger("Die");

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // Отключаем коллайдер, чтобы труп не мешал (по желанию)
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Уничтожаем объект через время (даём анимации смерти доиграть)
        Destroy(gameObject, 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
