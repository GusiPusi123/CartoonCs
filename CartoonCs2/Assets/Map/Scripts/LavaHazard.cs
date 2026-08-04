using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Опасная зона лавы: наносит периодический урон всему, что реализует IDamageable
/// (работает и для игрока, и для врагов — можно заманивать врагов в лаву).
/// Спавнит частицы всплеска при входе, плюс амбиентные пузыри на поверхности
/// сами по себе, для атмосферы, даже если никто в лаву не заходит.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LavaHazard : MonoBehaviour
{
    [Header("Урон")]
    [SerializeField] private int damagePerTick = 10;
    [SerializeField] private float tickInterval = 0.5f;
    [Tooltip("Если включено — любой контакт с лавой сразу убивает, независимо от текущего HP")]
    [SerializeField] private bool instantKill = false;

    [Header("Эффекты при входе в лаву")]
    [SerializeField] private GameObject splashParticles;
    [Tooltip("Смещение по высоте (Y) точки спавна частиц всплеска относительно точки контакта")]
    [SerializeField] private float splashSpawnHeightOffset = 0f;
    [Tooltip("Через сколько секунд удалять заспавненный объект частиц всплеска. 0 или меньше — не удалять автоматически")]
    [SerializeField] private float splashParticlesLifetime = 3f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip splashSound;

    [Header("Выталкивание из лавы")]
    [Tooltip("Небольшой импульс вверх при каждом тике урона — не даёт утонуть и подталкивает выбраться. Работает только для целей с Rigidbody")]
    [SerializeField] private float pushForce = 3f;

    [Header("Амбиентные пузыри (фон, не требуют присутствия кого-либо)")]
    [SerializeField] private GameObject bubbleParticlesPrefab;
    [SerializeField] private float bubbleIntervalMin = 1f;
    [SerializeField] private float bubbleIntervalMax = 3f;
    [Tooltip("Область, в которой случайно спавнятся пузыри. Если не задано — используется Collider этого же объекта")]
    [SerializeField] private Collider lavaSurfaceBounds;
    [Tooltip("Смещение по высоте (Y) относительно верхней границы lavaSurfaceBounds, на которой спавнятся пузыри")]
    [SerializeField] private float bubbleSpawnHeightOffset = 0f;
    [Tooltip("Через сколько секунд удалять заспавненный объект пузырей. 0 или меньше — не удалять автоматически")]
    [SerializeField] private float bubbleParticlesLifetime = 2f;

    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = false;

    private readonly Dictionary<Collider, IDamageable> targetsInLava = new Dictionary<Collider, IDamageable>();
    private readonly Dictionary<Collider, float> tickTimers = new Dictionary<Collider, float>();

    private void Awake()
    {
        if (lavaSurfaceBounds == null)
            lavaSurfaceBounds = GetComponent<Collider>();
    }

    private void Start()
    {
        StartCoroutine(AmbientBubbles());
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        targetsInLava[other] = damageable;
        tickTimers[other] = 0f; // урон наносится сразу при входе, не дожидаясь первого интервала

        if (splashParticles != null)
        {
            Vector3 spawnPoint = other.ClosestPoint(transform.position) + Vector3.up * splashSpawnHeightOffset;
            GameObject splash = Instantiate(splashParticles, spawnPoint, Quaternion.identity);
            DestroyAfterLifetime(splash, splashParticlesLifetime);
        }

        if (audioSource != null && splashSound != null)
            audioSource.PlayOneShot(splashSound);

        if (debugLogs)
            Debug.Log($"[{name}] {other.name} вошёл в лаву.");
    }

    private void OnTriggerStay(Collider other)
    {
        if (!targetsInLava.TryGetValue(other, out IDamageable damageable)) return;

        tickTimers[other] -= Time.deltaTime;

        if (tickTimers[other] <= 0f)
        {
            ApplyLavaDamage(other, damageable);
            tickTimers[other] = tickInterval;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        targetsInLava.Remove(other);
        tickTimers.Remove(other);

        if (debugLogs)
            Debug.Log($"[{name}] {other.name} вышел из лавы.");
    }

    private void ApplyLavaDamage(Collider target, IDamageable damageable)
    {
        int amount = instantKill ? 999999 : damagePerTick;

        damageable.TakeDamage(amount);

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null && pushForce > 0f)
        {
            rb.AddForce(Vector3.up * pushForce, ForceMode.Impulse);
        }

        if (debugLogs)
            Debug.Log($"[{name}] Урон от лавы: {amount} для {target.name}");
    }

    private IEnumerator AmbientBubbles()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(bubbleIntervalMin, bubbleIntervalMax));

            if (bubbleParticlesPrefab == null || lavaSurfaceBounds == null) continue;

            Bounds bounds = lavaSurfaceBounds.bounds;
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y + bubbleSpawnHeightOffset,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            GameObject bubble = Instantiate(bubbleParticlesPrefab, randomPoint, Quaternion.identity);
            bubble.transform.rotation = Quaternion.LookRotation(Vector3.up);
            DestroyAfterLifetime(bubble, bubbleParticlesLifetime);
        }
    }

    /// <summary>
    /// Удаляет объект через заданное время. Если lifetime <= 0 — не удаляет
    /// (полезно, если у самого партикла настроен Stop Action = Destroy).
    /// </summary>
    private void DestroyAfterLifetime(GameObject obj, float lifetime)
    {
        if (lifetime > 0f)
            Destroy(obj, lifetime);
    }
}
