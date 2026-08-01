using UnityEngine;

/// <summary>
/// Единая точка входа для спавна летящих цифр урона. Синглтон — достаточно
/// один раз положить в сцену, дальше вызывается откуда угодно:
/// DamageNumberSpawner.Instance?.Spawn(hit.point, damage);
/// </summary>
public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    [SerializeField] private GameObject damageNumberPrefab;
    [Tooltip("Небольшой подъём над точкой попадания, чтобы цифра не спавнилась прямо в поверхности")]
    [SerializeField] private float spawnHeightOffset = 0.15f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>Создаёт всплывающую цифру урона в указанной мировой точке.</summary>
    public void Spawn(Vector3 worldPosition, int damageAmount, bool isCritical = false)
    {
        if (damageNumberPrefab == null) return;

        Vector3 spawnPosition = worldPosition + Vector3.up * spawnHeightOffset;
        GameObject instance = Instantiate(damageNumberPrefab, spawnPosition, Quaternion.identity);

        DamageNumber damageNumber = instance.GetComponent<DamageNumber>();
        if (damageNumber != null)
            damageNumber.Init(damageAmount, isCritical);
    }
}
