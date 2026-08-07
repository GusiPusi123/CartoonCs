using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExplosiveBarrel : MonoBehaviour, IDamageable
{
    [Header("Здоровье бочки")]
    [Tooltip("Сколько урона нужно нанести бочке, чтобы она взорвалась")]
    [SerializeField] private int maxHealth = 30;
    private int currentHealth;

    [Header("Взрыв")]
    [Tooltip("Урон в центре взрыва")]
    [SerializeField] private int explosionDamage = 80;
    [Tooltip("Радиус, в котором взрыв наносит урон")]
    [SerializeField] private float explosionRadius = 5f;
    [Tooltip("Если true — урон линейно затухает от центра к краю радиуса")]
    [SerializeField] private bool falloffDamage = true;
    [Tooltip("Слои, по которым бочка ищет цели для урона")]
    [SerializeField] private LayerMask damageMask = ~0;

    [Header("Физическая сила взрыва")]
    [Tooltip("Сила, отбрасывающая объекты с Rigidbody в радиусе взрыва")]
    [SerializeField] private float explosionForce = 700f;
    [Tooltip("Дополнительный подброс вверх, чтобы объекты не 'скользили' по земле")]
    [SerializeField] private float upwardsModifier = 0.5f;

    [Header("Цепная реакция")]
    [Tooltip("Взрывать ли другие бочки, попавшие в радиус взрыва")]
    [SerializeField] private bool chainReaction = true;
    [Tooltip("Небольшая задержка перед взрывом соседних бочек, чтобы был эффект цепочки")]
    [SerializeField] private float chainReactionDelay = 0.15f;

    [Header("Эффекты")]
    [Tooltip("Префаб визуального эффекта взрыва (частицы, свет и т.п.)")]
    [SerializeField] private GameObject explosionVFXPrefab;
    [Tooltip("Звук взрыва")]
    [SerializeField] private AudioClip explosionSound;
    [Range(0f, 1f)]
    [SerializeField] private float explosionVolume = 1f;

    [Header("Взрыв при столкновении")]
    [Tooltip("Взрываться ли от сильного физического удара (например, если бочку сбросили с высоты)")]
    [SerializeField] private bool explodeOnHardImpact = true;
    [Tooltip("Минимальная скорость удара, при которой бочка взрывается")]
    [SerializeField] private float impactVelocityThreshold = 8f;

    private bool hasExploded = false;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        if (hasExploded) return;

        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Explode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!explodeOnHardImpact || hasExploded) return;

        if (collision.relativeVelocity.magnitude >= impactVelocityThreshold)
        {
            Explode();
        }
    }

    /// <summary>Принудительно взрывает бочку (можно вызвать извне, например из триггера или скрипта уровня).</summary>
    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        SpawnVFXAndSound();
        ApplyDamageAndForce();

        if (chainReaction)
        {
            TriggerChainReaction();
        }

        // Саму бочку уничтожаем отдельно, чтобы дать время звуку доиграть,
        // если AudioSource не на самой бочке, а создаётся отдельно в SpawnVFXAndSound().
        Destroy(gameObject, 0.05f);
    }

    private void ApplyDamageAndForce()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, damageMask);
        HashSet<IDamageable> alreadyDamaged = new HashSet<IDamageable>();

        foreach (Collider hit in hits)
        {
            // Урон
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && !alreadyDamaged.Contains(damageable) && !ReferenceEquals(hit.gameObject, gameObject))
            {
                alreadyDamaged.Add(damageable);

                float distance = Vector3.Distance(transform.position, hit.transform.position);
                int finalDamage = falloffDamage
                    ? Mathf.RoundToInt(Mathf.Lerp(explosionDamage, 0f, distance / explosionRadius))
                    : explosionDamage;

                damageable.TakeDamage(finalDamage);
            }

            // Физический импульс
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);
            }
        }
    }

    private void TriggerChainReaction()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, damageMask);

        foreach (Collider hit in hits)
        {
            if (ReferenceEquals(hit.gameObject, gameObject)) continue;

            ExplosiveBarrel otherBarrel = hit.GetComponentInParent<ExplosiveBarrel>();
            if (otherBarrel != null && !otherBarrel.hasExploded)
            {
                StartCoroutine(DelayedChainExplode(otherBarrel));
            }
        }
    }

    private IEnumerator DelayedChainExplode(ExplosiveBarrel otherBarrel)
    {
        yield return new WaitForSeconds(chainReactionDelay);

        if (otherBarrel != null)
        {
            otherBarrel.Explode();
        }
    }

    private void SpawnVFXAndSound()
    {
        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, explosionVolume);
        }
    }

    // Наглядно показывает радиус взрыва в редакторе, не влияет на игру
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
