using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Враг-камикадзе: преследует игрока по NavMesh, при приближении на дистанцию
/// детонации начинает мигать (предупреждение) и через fuseTime взрывается,
/// нанося урон по area всем IDamageable в радиусе взрыва. Также взрывается,
/// если его убить рядом с игроком (explodeOnDeath) — то есть застрелить его
/// в упор так же опасно, как дать ему добежать.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SuicideEnemy : MonoBehaviour, IDamageable
{
    [Header("Здоровье")]
    [SerializeField] private int maxHealth = 40;
    private int currentHealth;

    [Header("Цель")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRadius = 15f;

    [Header("Движение (погоня)")]
    [SerializeField] private float moveSpeed = 4f;
    [Tooltip("На каком расстоянии до игрока враг начинает ускоряться перед броском")]
    [SerializeField] private float chargeDistance = 6f;
    [Tooltip("Во сколько раз быстрее становится враг во время ускоренного броска")]
    [SerializeField] private float chargeSpeedMultiplier = 1.6f;
    [SerializeField] private float pathUpdateRate = 0.2f;
    [SerializeField] private float navMeshSampleRadius = 5f;
    private float pathUpdateTimer;

    [Header("Взрыв — срабатывание")]
    [Tooltip("На каком расстоянии до игрока начинается детонация (запал)")]
    [SerializeField] private float explosionTriggerRange = 2.5f;
    [Tooltip("Сколько секунд горит запал перед взрывом, после того как игрок оказался в радиусе")]
    [SerializeField] private float fuseTime = 1f;

    [Header("Взрыв — урон")]
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private int explosionDamage = 40;
    [SerializeField] private LayerMask explosionDamageMask;

    [Header("Взрыв — отдача")]
    [Tooltip("Сила отброса для целей с Rigidbody (например, игрок)")]
    [SerializeField] private float explosionPushForce = 8f;

    [Header("Взрыв при смерти от урона")]
    [Tooltip("Если враг убит выстрелом рядом с игроком — тоже взорвётся, а не просто умрёт молча")]
    [SerializeField] private bool explodeOnDeath = true;
    [Tooltip("Взрыв при смерти сработает, только если игрок находится не дальше этого расстояния от врага")]
    [SerializeField] private float explodeOnDeathRange = 4f;

    [Header("Мигание запала")]
    [SerializeField] private Renderer warningRenderer;
    [SerializeField] private Color warningColor = Color.red;
    [Tooltip("Мигание ускоряется по мере приближения взрыва: с этой скорости...")]
    [SerializeField] private float blinkSpeedStart = 4f;
    [Tooltip("...до этой скорости к моменту взрыва")]
    [SerializeField] private float blinkSpeedEnd = 14f;

    [Header("Эффекты")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip fuseSound;
    [SerializeField] private AudioClip explosionSound;

    [Header("Мигание при уроне")]
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float hitFlashDuration = 0.15f;

    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = false;

    private NavMeshAgent agent;
    private bool isDead;
    private bool hasExploded;
    private bool isFusing;
    private Coroutine fuseCoroutine;

    private Renderer[] renderers;
    private Color[] originalColors;
    private Coroutine flashCoroutine;
    private MaterialPropertyBlock warningPropBlock;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        currentHealth = maxHealth;

        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;

        if (warningRenderer != null)
            warningPropBlock = new MaterialPropertyBlock();
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
        if (isDead || hasExploded || player == null) return;
        if (!agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            UpdateChaseSpeed(distanceToPlayer);

            pathUpdateTimer -= Time.deltaTime;
            if (pathUpdateTimer <= 0f)
            {
                UpdateChaseTarget();
                pathUpdateTimer = pathUpdateRate;
            }

            if (!isFusing && distanceToPlayer <= explosionTriggerRange)
            {
                fuseCoroutine = StartCoroutine(FuseAndExplode());
            }
        }
        else
        {
            agent.ResetPath();
        }
    }

    private void UpdateChaseSpeed(float distanceToPlayer)
    {
        agent.speed = distanceToPlayer <= chargeDistance ? moveSpeed * chargeSpeedMultiplier : moveSpeed;
    }

    private void UpdateChaseTarget()
    {
        if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private IEnumerator FuseAndExplode()
    {
        isFusing = true;

        if (audioSource != null && fuseSound != null)
            audioSource.PlayOneShot(fuseSound);

        if (debugLogs) Debug.Log($"[{name}] Запал горит! Взрыв через {fuseTime} сек.");

        float timer = 0f;

        while (timer < fuseTime)
        {
            timer += Time.deltaTime;

            // Мигание ускоряется по мере приближения момента взрыва
            float progress = timer / fuseTime;
            float currentBlinkSpeed = Mathf.Lerp(blinkSpeedStart, blinkSpeedEnd, progress);
            bool blinkOn = Mathf.PingPong(Time.time * currentBlinkSpeed, 1f) > 0.5f;
            SetWarningState(blinkOn);

            yield return null;
        }

        Explode();
    }

    private void SetWarningState(bool on)
    {
        if (warningRenderer == null) return;

        if (on)
        {
            // MaterialPropertyBlock переопределяет цвет на уровне рендер-инстанса,
            // не создавая новый Material и не завися от того, какое свойство цвета
            // использует шейдер (_Color у Built-in, _BaseColor у URP/HDRP) —
            // выставляем оба сразу, лишнее свойство шейдер просто проигнорирует.
            warningRenderer.GetPropertyBlock(warningPropBlock);
            warningPropBlock.SetColor("_Color", warningColor);
            warningPropBlock.SetColor("_BaseColor", warningColor);
            warningRenderer.SetPropertyBlock(warningPropBlock);
        }
        else
        {
            // Пустой property block снимает переопределение — рендерер возвращается
            // к исходному цвету материала сам, без необходимости хранить его отдельно.
            warningRenderer.SetPropertyBlock(null);
        }
    }

    /// <summary>Реализация IDamageable — врага можно застрелить до того, как он добежит.</summary>
    public void TakeDamage(int damageAmount)
    {
        if (isDead || hasExploded) return;

        currentHealth -= damageAmount;

        if (animator != null)
            animator.SetTrigger("Hit");

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashHit());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashHit()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = hitFlashColor;
        }

        yield return new WaitForSeconds(hitFlashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }

        flashCoroutine = null;
    }

    private void Die()
    {
        if (isDead || hasExploded) return;
        isDead = true;

        if (fuseCoroutine != null)
            StopCoroutine(fuseCoroutine);

        bool playerNearby = player != null && Vector3.Distance(transform.position, player.position) <= explodeOnDeathRange;

        if (explodeOnDeath && playerNearby)
        {
            if (debugLogs) Debug.Log($"[{name}] Убит рядом с игроком — детонирует посмертно!");
            Explode();
        }
        else
        {
            agent.isStopped = true;
            agent.enabled = false;

            if (animator != null)
                animator.SetTrigger("Die");

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Destroy(gameObject, 2f);
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        isDead = true;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        if (audioSource != null && explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, explosionDamageMask);
        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            damageable?.TakeDamage(explosionDamage);

            Rigidbody hitRb = hit.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                Vector3 pushDirection = (hit.transform.position - transform.position).normalized;
                hitRb.AddForce(pushDirection * explosionPushForce, ForceMode.Impulse);
            }
        }

        if (debugLogs) Debug.Log($"[{name}] Взрыв! Задето целей: {hits.Length}");

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionTriggerRange);

        Gizmos.color = new Color(1f, 0.3f, 0f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
