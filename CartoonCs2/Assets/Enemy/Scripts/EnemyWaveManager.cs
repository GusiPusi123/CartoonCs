using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Управляет волнами врагов: спавн, увеличение количества от волны к волне,
/// отображение прогресса на Slider, события начала/конца волны.
/// Работает с ЛЮБЫМИ префабами врагов — ничего в их скриптах менять не нужно,
/// отслеживание смерти происходит через служебный компонент WaveEnemyTracker,
/// который добавляется автоматически при спавне.
/// </summary>
public class EnemyWaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyEntry
    {
        public GameObject prefab;
        [Tooltip("Относительный вес шанса появления. Чем больше — тем чаще выпадает этот враг.")]
        public float weight = 1f;
        [Tooltip("С какой волны этот враг начинает появляться (0 = с первой)")]
        public int minWave = 0;
    }

    [Header("Враги")]
    [SerializeField] private List<EnemyEntry> enemyPrefabs = new List<EnemyEntry>();

    [Header("Точки спавна")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool randomizeSpawnPoint = true;

    [Header("Настройки волн")]
    [SerializeField] private int baseEnemyCount = 5;
    [Tooltip("На сколько умножается количество врагов с каждой новой волной")]
    [SerializeField] private float enemyCountMultiplier = 1.25f;
    [Tooltip("0 = бесконечные волны")]
    [SerializeField] private int maxWaves = 0;
    [SerializeField] private float delayBeforeFirstWave = 3f;
    [SerializeField] private float delayBetweenWaves = 5f;
    [Tooltip("Пауза между спавном отдельных врагов внутри волны")]
    [SerializeField] private float spawnInterval = 0.5f;
    [Tooltip("Максимум живых врагов одновременно (0 = без ограничений)")]
    [SerializeField] private int maxAliveAtOnce = 0;

    [Header("UI")]
    [SerializeField] private Slider enemiesRemainingSlider;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI enemiesLeftText;

    [Header("События")]
    public UnityEvent<int> onWaveStart;      // передаёт номер волны
    public UnityEvent<int> onWaveComplete;   // передаёт номер завершённой волны
    public UnityEvent onAllWavesComplete;

    private int currentWave = 0;
    private int totalEnemiesInWave;
    private int enemiesRemaining;
    private int enemiesAliveNow;
    private int enemiesLeftToSpawn;
    private bool waveInProgress;
    private float totalWeight;

    private void Awake()
    {
        RecalculateWeights();
    }

    private void Start()
    {
        StartCoroutine(RunWaves());
    }

    private void RecalculateWeights()
    {
        totalWeight = 0f;
        foreach (var entry in enemyPrefabs)
            totalWeight += Mathf.Max(0f, entry.weight);
    }

    private IEnumerator RunWaves()
    {
        yield return StartCoroutine(Countdown(delayBeforeFirstWave, "Волна начнётся через"));

        while (maxWaves <= 0 || currentWave < maxWaves)
        {
            currentWave++;
            yield return StartCoroutine(RunSingleWave());

            if (maxWaves > 0 && currentWave >= maxWaves)
            {
                onAllWavesComplete?.Invoke();
                if (countdownText != null) countdownText.text = "Все волны пройдены!";
                yield break;
            }

            yield return StartCoroutine(Countdown(delayBetweenWaves, "Следующая волна через"));
        }
    }

    private IEnumerator RunSingleWave()
    {
        waveInProgress = true;

        totalEnemiesInWave = Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(enemyCountMultiplier, currentWave - 1));
        enemiesRemaining = totalEnemiesInWave;
        enemiesLeftToSpawn = totalEnemiesInWave;
        enemiesAliveNow = 0;

        UpdateWaveText();
        UpdateSlider();
        onWaveStart?.Invoke(currentWave);

        while (enemiesLeftToSpawn > 0)
        {
            if (maxAliveAtOnce > 0 && enemiesAliveNow >= maxAliveAtOnce)
            {
                yield return null;
                continue;
            }

            SpawnEnemy();
            enemiesLeftToSpawn--;

            yield return new WaitForSeconds(spawnInterval);
        }

        // Ждём, пока не убьют всех заспавненных врагов волны
        while (enemiesRemaining > 0)
        {
            yield return null;
        }

        waveInProgress = false;
        onWaveComplete?.Invoke(currentWave);
    }

    private void SpawnEnemy()
    {
        GameObject prefab = PickRandomEnemyPrefab();
        if (prefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"[{name}] Не заданы префабы врагов или точки спавна.");
            return;
        }

        Transform point = randomizeSpawnPoint
            ? spawnPoints[Random.Range(0, spawnPoints.Length)]
            : spawnPoints[(totalEnemiesInWave - enemiesLeftToSpawn) % spawnPoints.Length];

        GameObject enemy = Instantiate(prefab, point.position, point.rotation);
        enemiesAliveNow++;

        WaveEnemyTracker tracker = enemy.AddComponent<WaveEnemyTracker>();
        tracker.Init(this);
    }

    private GameObject PickRandomEnemyPrefab()
    {
        List<EnemyEntry> available = enemyPrefabs.FindAll(e => e.prefab != null && currentWave - 1 >= e.minWave);
        if (available.Count == 0) return null;

        float weightSum = 0f;
        foreach (var e in available) weightSum += Mathf.Max(0f, e.weight);

        float roll = Random.Range(0f, weightSum);
        float cumulative = 0f;

        foreach (var e in available)
        {
            cumulative += Mathf.Max(0f, e.weight);
            if (roll <= cumulative)
                return e.prefab;
        }

        return available[available.Count - 1].prefab;
    }

    /// <summary>Вызывается автоматически через WaveEnemyTracker при уничтожении врага.</summary>
    public void ReportEnemyDeath()
    {
        enemiesAliveNow = Mathf.Max(0, enemiesAliveNow - 1);
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        UpdateSlider();
    }

    private void UpdateSlider()
    {
        if (enemiesRemainingSlider != null)
        {
            enemiesRemainingSlider.maxValue = totalEnemiesInWave;
            enemiesRemainingSlider.value = enemiesRemaining;
        }

        if (enemiesLeftText != null)
            enemiesLeftText.text = $"{enemiesRemaining} / {totalEnemiesInWave}";
    }

    private void UpdateWaveText()
    {
        if (waveText != null)
            waveText.text = maxWaves > 0 ? $"Волна {currentWave} / {maxWaves}" : $"Волна {currentWave}";
    }

    private IEnumerator Countdown(float duration, string label)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            if (countdownText != null)
                countdownText.text = $"{label}: {Mathf.CeilToInt(remaining)}";

            remaining -= Time.deltaTime;
            yield return null;
        }

        if (countdownText != null)
            countdownText.text = "";
    }

    public bool IsWaveInProgress => waveInProgress;
    public int CurrentWave => currentWave;
    public int EnemiesRemaining => enemiesRemaining;
}

/// <summary>
/// Служебный компонент, автоматически добавляется на заспавненного врага.
/// При его уничтожении (смерть, Destroy) сообщает менеджеру волн.
/// Не требует изменений в существующих скриптах врагов.
/// </summary>
public class WaveEnemyTracker : MonoBehaviour
{
    private EnemyWaveManager manager;
    private bool reported;

    public void Init(EnemyWaveManager owner)
    {
        manager = owner;
    }

    private void OnDestroy()
    {
        if (reported || manager == null) return;
        reported = true;
        manager.ReportEnemyDeath();
    }
}
