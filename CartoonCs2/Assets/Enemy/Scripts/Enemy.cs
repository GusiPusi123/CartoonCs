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
using System.Collections;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public NavMeshAgent navAgent;
    public Transform player;
    public LayerMask groundLayer, playerLayer;
    public float health;
    public float walkPointRange;
    public float timeBetweenAttacks;
    public float sightRange;
    public float attackRange;
    public int damage;
    public ParticleSystem hitEffect;

    private Vector3 walkPoint;
    private bool walkPointSet;
    private bool alreadyAttacked;
    private bool takeDamage;

    private void Awake()
    {
        // animator = GetComponent<Animator>(); // Удалено
        player = GameObject.Find("Player").transform;
        navAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        bool playerInSightRange = Physics.CheckSphere(transform.position, sightRange, playerLayer);
        bool playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerLayer);

        if (!playerInSightRange && !playerInAttackRange)
        {
            Patroling();
        }
        else if (playerInSightRange && !playerInAttackRange)
        {
            ChasePlayer();
        }
        else if (playerInAttackRange && playerInSightRange)
        {
            AttackPlayer();
        }
        else if (!playerInSightRange && takeDamage)
        {
            ChasePlayer();
        }
    }

    private void Patroling()
    {
        if (!walkPointSet)
        {
            SearchWalkPoint();
        }

        if (walkPointSet)
        {
            navAgent.SetDestination(walkPoint);
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;
        // animator.SetFloat("Velocity", 0.2f); // Удалено

        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;
        }
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, groundLayer))
        {
            walkPointSet = true;
        }
    }

    private void ChasePlayer()
    {
        navAgent.SetDestination(player.position);
        // animator.SetFloat("Velocity", 0.6f); // Удалено
        navAgent.isStopped = false;
    }

    private void AttackPlayer()
    {
        navAgent.SetDestination(transform.position);

        if (!alreadyAttacked)
        {
            transform.LookAt(player.position);
            alreadyAttacked = true;
            // animator.SetBool("Attack", true); // Удалено
            Invoke(nameof(ResetAttack), timeBetweenAttacks);

            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange))
            {
                /*
                // Этот блок можно оставить, если хотите использовать урон через кастомные скрипты
                PlayerHUD playerHUD = hit.transform.GetComponent<PlayerHUD>();
                if (playerHUD != null)
                {
                   playerHUD.takeDamage(damage);
                }
                */
            }
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
        // animator.SetBool("Attack", false); // Удалено
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        hitEffect.Play();
        StartCoroutine(TakeDamageCoroutine());

        if (health <= 0)
        {
            Invoke(nameof(DestroyEnemy), 0.5f);
        }
    }

    private IEnumerator TakeDamageCoroutine()
    {
        takeDamage = true;
        yield return new WaitForSeconds(2f);
        takeDamage = false;
    }

    private void DestroyEnemy()
    {
        StartCoroutine(DestroyEnemyCoroutine());
    }

    private IEnumerator DestroyEnemyCoroutine()
    {
        // animator.SetBool("Dead", true); // Удалено
        yield return new WaitForSeconds(1.8f);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}