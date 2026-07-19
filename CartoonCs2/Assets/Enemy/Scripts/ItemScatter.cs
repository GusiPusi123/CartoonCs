using UnityEngine;

public class ItemScatter : MonoBehaviour
{
    [Header("Предметы для разброса")]
    [SerializeField] private GameObject[] itemPrefabs;
    [SerializeField] private int itemCount = 5;

    [Header("Режим выбора префаба")]
    [SerializeField] private bool randomPrefab = true;

    [Header("Сила разброса")]
    [SerializeField] private float minForce = 2f;
    [SerializeField] private float maxForce = 5f;
    [SerializeField] private float upwardForce = 3f;

    [Header("Разброс направления")]
    [SerializeField] private float spreadAngle = 45f;
    [SerializeField] private float spawnRadius = 0.3f;

    [Header("Вращение")]
    [SerializeField] private bool randomRotation = true;
    [SerializeField] private float maxTorque = 5f;

    [Header("Автоудаление предметов")]
    [SerializeField] private bool autoDestroy = true;
    [SerializeField] private float itemLifeTime = 10f;

    [Header("Автозапуск")]
    [SerializeField] private bool scatterOnStart = false;

    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = false;

    private void Start()
    {
        if (scatterOnStart)
        {
            Scatter();
        }
    }

    public void Scatter()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            if (debugLogs) Debug.LogWarning($"[{name}] ItemScatter: список префабов пуст!");
            return;
        }

        for (int i = 0; i < itemCount; i++)
        {
            SpawnSingleItem(i);
        }

        if (debugLogs)
            Debug.Log($"[{name}] ItemScatter: заспавнено {itemCount} предметов");
    }

    private void SpawnSingleItem(int index)
    {
        GameObject prefab = randomPrefab
            ? itemPrefabs[Random.Range(0, itemPrefabs.Length)]
            : itemPrefabs[index % itemPrefabs.Length];

        if (prefab == null) return;

        Vector3 spawnOffset = Random.insideUnitSphere * spawnRadius;
        spawnOffset.y = Mathf.Abs(spawnOffset.y);
        Vector3 spawnPosition = transform.position + spawnOffset;

        Quaternion spawnRotation = randomRotation
            ? Random.rotation
            : Quaternion.identity;

        GameObject item = Instantiate(prefab, spawnPosition, spawnRotation);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb == null)
        {
            if (debugLogs) Debug.LogWarning($"[{name}] На префабе {prefab.name} нет Rigidbody — предмет не будет разлетаться физически.");
        }
        else
        {
            Vector3 randomDirection = GetRandomConeDirection(Vector3.up, spreadAngle);
            float force = Random.Range(minForce, maxForce);
            Vector3 impulse = randomDirection * force + Vector3.up * upwardForce;

            rb.AddForce(impulse, ForceMode.Impulse);

            if (maxTorque > 0f)
            {
                Vector3 randomTorque = new Vector3(
                    Random.Range(-maxTorque, maxTorque),
                    Random.Range(-maxTorque, maxTorque),
                    Random.Range(-maxTorque, maxTorque)
                );
                rb.AddTorque(randomTorque, ForceMode.Impulse);
            }
        }

        // Просто удаляем предмет через заданное время, без отдельного скрипта
        if (autoDestroy)
        {
            Destroy(item, itemLifeTime);
        }
    }

    private Vector3 GetRandomConeDirection(Vector3 baseDirection, float angle)
    {
        float randomAngle = Random.Range(0f, angle);
        float randomRotationAroundAxis = Random.Range(0f, 360f);

        Quaternion tiltRotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);
        Quaternion spinRotation = Quaternion.AngleAxis(randomRotationAroundAxis, baseDirection);

        Vector3 tiltedDirection = tiltRotation * baseDirection;
        return spinRotation * tiltedDirection;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
