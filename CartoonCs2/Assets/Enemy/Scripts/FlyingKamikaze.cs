using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlyingKamikaze : MonoBehaviour, IDamageable
{
    private enum State { Chasing, Telegraphing, Charging, Exploded }
    private State currentState = State.Chasing;

    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = false;

    [Header("Здоровье")]
    [SerializeField] private int maxHealth = 30;
    private int currentHealth;

    [Header("Цель")]
    [SerializeField] private Transform player;

    [Header("Полёт (преследование)")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private float hoverHeight = 2f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Телеграф атаки (предупреждение перед рывком)")]
    [SerializeField] private float chargeRange = 6f;
    [SerializeField] private float telegraphDuration = 0.5f; // сколько секунд враг "целится" перед рывком — даёт время увернуться
    [SerializeField] private Color telegraphColor = Color.red; // цвет подсветки во время прицеливания
    [SerializeField] private GameObject telegraphEffect; // необязательно — партиклы прицеливания

    [Header("Рывок (атака)")]
    [SerializeField] private float chargeSpeed = 18f; // фиксированная скорость рывка (не набирается постепенно)
    [SerializeField] private float chargeMaxDuration = 1.2f; // если за это время никуда не влетел — рывок отменяется, враг возвращается к преследованию
    [SerializeField] private float chargeTurnRate = 60f; // градусов в секунду — насколько враг может корректировать курс ВО ВРЕМЯ рывка (0 = летит строго по прямой)

    [Header("Взрыв")]
    [SerializeField] private int explosionDamage = 30;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private LayerMask explosionDamageMask;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip telegraphSound; // звук прицеливания/предупреждения

    [Header("Мигание при уроне")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.3f;

    private Rigidbody rb;
    private Renderer[] renderers;
    private Color[] originalColors;
    private Coroutine flashCoroutine;

    private float telegraphTimer;
    private float chargeTimer;
    private Vector3 chargeDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

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
    }

    private void FixedUpdate()
    {
        if (currentState == State.Exploded || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Chasing:
                HandleChasing(distanceToPlayer);
                break;
            case State.Telegraphing:
                HandleTelegraphing();
                break;
            case State.Charging:
                HandleCharging();
                break;
        }
    }

    private void HandleChasing(float distanceToPlayer)
    {
        if (distanceToPlayer > detectionRadius)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.fixedDeltaTime * 2f);
            return;
        }

        if (distanceToPlayer <= chargeRange)
        {
            StartTelegraph();
            return;
        }

        Vector3 targetPosition = player.position + Vector3.up * hoverHeight;
        Vector3 direction = (targetPosition - transform.position).normalized;

        rb.velocity = direction * chaseSpeed;
        FaceDirection(direction, rotationSpeed);

        if (debugLogs)
            Debug.Log($"[{name}] Преследование. Дистанция: {distanceToPlayer:F1}");
    }

    private void StartTelegraph()
    {
        currentState = State.Telegraphing;
        telegraphTimer = telegraphDuration;

        // Останавливаем движение во время прицеливания — враг "зависает" на месте, готовясь к рывку
        rb.velocity = Vector3.zero;

        if (audioSource != null && telegraphSound != null)
            audioSource.PlayOneShot(telegraphSound);

        if (telegraphEffect != null)
            Instantiate(telegraphEffect, transform.position, Quaternion.identity, transform);

        SetColor(telegraphColor);

        if (debugLogs)
            Debug.Log($"[{name}] Прицеливание перед рывком...");
    }

    private void HandleTelegraphing()
    {
        // Во время прицеливания враг продолжает смотреть на игрока (видно, куда полетит рывок)
        Vector3 direction = (player.position - transform.position).normalized;
        FaceDirection(direction, rotationSpeed * 2f);

        telegraphTimer -= Time.fixedDeltaTime;

        if (telegraphTimer <= 0f)
        {
            StartCharge();
        }
    }

    private void StartCharge()
    {
        currentState = State.Charging;
        chargeTimer = chargeMaxDuration;

        // Направление фиксируется ОДИН РАЗ в момент начала рывка — дальше враг не "прилипает" к игроку
        chargeDirection = (player.position - transform.position).normalized;

        ResetColor();

        if (debugLogs)
            Debug.Log($"[{name}] РЫВОК!");
    }

    private void HandleCharging()
    {
        chargeTimer -= Time.fixedDeltaTime;

        // Если рывок истёк по времени и никуда не попал — отменяем атаку, возвращаемся к преследованию
        if (chargeTimer <= 0f)
        {
            currentState = State.Chasing;
            if (debugLogs) Debug.Log($"[{name}] Рывок промахнулся, возврат к преследованию.");
            return;
        }

        // Небольшая коррекция курса разрешена (chargeTurnRate), но враг не мгновенно доворачивается на игрока
        if (chargeTurnRate > 0f && player != null)
        {
            Vector3 desiredDirection = (player.position - transform.position).normalized;
            chargeDirection = Vector3.RotateTowards(chargeDirection, desiredDirection, chargeTurnRate * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f).normalized;
        }

        rb.velocity = chargeDirection * chargeSpeed;
        FaceDirection(chargeDirection, rotationSpeed * 3f);
    }

    private void FaceDirection(Vector3 direction, float speed)
    {
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != State.Charging) return;

        if (debugLogs)
            Debug.Log($"[{name}] Столкновение с: {collision.collider.name}");

        Explode();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState != State.Charging) return;
        if (!other.CompareTag("Player")) return;

        if (debugLogs)
            Debug.Log($"[{name}] Триггер-попадание в игрока: {other.name}");

        Explode();
    }

    private void Explode()
    {
        if (currentState == State.Exploded) return;
        currentState = State.Exploded;

        if (debugLogs)
            Debug.Log($"[{name}] ВЗРЫВ!");

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, explosionDamageMask);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(explosionDamage);
                }
            }
        }

        if (audioSource != null && explosionSound != null)
            audioSource.PlayOneShot(explosionSound);

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = false;
        }

        rb.velocity = Vector3.zero;

        Destroy(gameObject, 0.1f);
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentState == State.Exploded) return;

        currentHealth -= damageAmount;

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashWhite());

        if (currentHealth <= 0)
        {
            Explode();
        }
    }

    private System.Collections.IEnumerator FlashWhite()
    {
        SetColor(flashColor);
        yield return new WaitForSeconds(flashDuration);

        // Не сбрасываем цвет, если враг в этот момент как раз телеграфирует атаку (там свой цвет)
        if (currentState != State.Telegraphing)
            ResetColor();

        flashCoroutine = null;
    }

    private void SetColor(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = color;
        }
    }

    private void ResetColor()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chargeRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
