using UnityEngine;

/// <summary>
/// Разбивает стекло целиком (без физических осколков-кусков) —
/// проигрывает партикл-эффект (растянутый под размер конкретного стекла)
/// и убирает объект. Ломается либо от пули (через IDamageable/IShatterable),
/// либо от касания игроком.
/// </summary>
public interface IShatterable
{
    void Shatter(Vector3 hitPoint, Vector3 hitNormal);
}

[RequireComponent(typeof(Collider))]
public class BreakableGlass : MonoBehaviour, IDamageable, IShatterable
{
    [Header("Эффект разбития")]
    [Tooltip("Твоя партикл-система осколков. Shape должен быть Box (1x1x1), Scaling Mode = Shape")]
    [SerializeField] private GameObject shatterParticlesPrefab;
    [SerializeField] private float particlesLifetime = 3f;

    [Header("Масштабирование эффекта под размер стекла")]
    [Tooltip("Количество осколков для стекла размером 1x1x1. При увеличении размера стекла количество растёт пропорционально объёму")]
    [SerializeField] private int baseParticleCount = 40;
    [Tooltip("Не даём осколкам расплодиться до бесконечности на огромных стёклах")]
    [SerializeField] private int maxParticleCount = 300;

    [Header("Звук")]
    [SerializeField] private AudioClip shatterSound;
    [SerializeField] private float shatterSoundVolume = 1f;

    [Header("Разбитие от касания игроком")]
    [SerializeField] private bool breakOnPlayerTouch = true;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Минимальная скорость столкновения игрока, чтобы стекло разбилось (0 = ломается от любого касания)")]
    [SerializeField] private float minTouchSpeed = 0f;

    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = false;

    private bool isShattered;

    // === IDamageable — на случай, если оружие вызывает TakeDamage() напрямую (без IShatterable-проверки) ===
    public void TakeDamage(int damage)
    {
        // Урон не важен — стекло разбивается от ЛЮБОГО попадания пули,
        // вне зависимости от места и величины урона.
        Shatter(transform.position, Vector3.up);
    }

    // === IShatterable — вызывается оружием с точной точкой попадания пули ===
    public void Shatter(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isShattered) return;
        isShattered = true;

        PlayShatterEffects();

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        if (debugLogs)
            Debug.Log($"[{name}] Стекло разбито в точке {hitPoint}");

        // Даём партиклам время доиграть перед полным уничтожением объекта
        Destroy(gameObject, particlesLifetime);
    }

    private void PlayShatterEffects()
    {
        if (shatterParticlesPrefab != null)
        {
            // Спавним по центру стекла (не в hitPoint) — эффект растягивается
            // на весь объём куба, поэтому логичнее ставить его по центру объекта,
            // иначе Box-shape окажется смещён относительно самого стекла.
            GameObject particlesObj = Instantiate(shatterParticlesPrefab, transform.position, transform.rotation);

            Vector3 glassSize = GetGlassSize();

            // Растягиваем ТОЛЬКО форму эмиттера. Работает благодаря тому, что
            // на префабе Particle System выставлен Scaling Mode = Shape —
            // тогда localScale влияет на форму Box, но НЕ на размер/скорость
            // отдельных частиц (Start Size/Start Speed остаются фиксированными).
            particlesObj.transform.localScale = glassSize;

            // Количество осколков подгоняем под объём стекла,
            // чтобы большое окно не выглядело "пустым" с той же горсткой частиц
            ScaleEmissionByVolume(particlesObj, glassSize);

            Destroy(particlesObj, particlesLifetime);
        }

        if (shatterSound != null)
            AudioSource.PlayClipAtPoint(shatterSound, transform.position, shatterSoundVolume);
    }

    /// <summary>
    /// Возвращает реальный размер стекла в мировых координатах —
    /// используем bounds коллайдера, а не просто transform.localScale,
    /// т.к. если у меша исходно не единичный куб (1x1x1), localScale даст неверный масштаб.
    /// </summary>
    private Vector3 GetGlassSize()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            return col.bounds.size;

        return transform.lossyScale; // fallback, если коллайдера почему-то нет
    }

    private void ScaleEmissionByVolume(GameObject particlesObj, Vector3 glassSize)
    {
        ParticleSystem ps = particlesObj.GetComponent<ParticleSystem>();
        if (ps == null) return;

        float volume = glassSize.x * glassSize.y * glassSize.z;

        // Кубический корень (степень 0.66) — чтобы рост количества частиц был
        // не слишком резким. Без этого стекло 3x3x3 (объём 27) дало бы в 27 раз
        // больше осколков, что уже перебор.
        float volumeFactor = Mathf.Pow(Mathf.Max(volume, 0.01f), 0.66f);

        int particleCount = Mathf.Clamp(
            Mathf.RoundToInt(baseParticleCount * volumeFactor),
            baseParticleCount,
            maxParticleCount
        );

        ParticleSystem.EmissionModule emission = ps.emission;

        if (emission.burstCount > 0)
        {
            ParticleSystem.Burst burst = emission.GetBurst(0);
            burst.count = particleCount;
            emission.SetBurst(0, burst);
        }
        else if (debugLogs)
        {
            Debug.LogWarning($"[{name}] У префаба {shatterParticlesPrefab.name} нет Burst в Emission — количество осколков не масштабируется");
        }
    }

    // === Касание игроком (физическое столкновение) ===
    private void OnCollisionEnter(Collision collision)
    {
        if (!breakOnPlayerTouch || isShattered) return;
        if (!collision.collider.CompareTag(playerTag)) return;

        if (minTouchSpeed > 0f && collision.relativeVelocity.magnitude < minTouchSpeed)
            return;

        bool hasContact = collision.contacts.Length > 0;
        Vector3 hitPoint = hasContact ? collision.contacts[0].point : transform.position;
        Vector3 hitNormal = hasContact ? collision.contacts[0].normal : Vector3.up;

        Shatter(hitPoint, hitNormal);
    }

    // === Касание игроком (если коллайдер стекла — Is Trigger) ===
    private void OnTriggerEnter(Collider other)
    {
        if (!breakOnPlayerTouch || isShattered) return;
        if (!other.CompareTag(playerTag)) return;

        Shatter(other.ClosestPoint(transform.position), Vector3.up);
    }
}
