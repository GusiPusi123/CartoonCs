using UnityEngine;

/// <summary>
/// Подписывается на событие OnWaveComplete у EnemyWaveManager и восстанавливает
/// здоровье игрока после того, как волна врагов полностью уничтожена.
/// Поддерживает три режима: фиксированное количество ХП, процент от максимума,
/// или полное восстановление.
/// </summary>
public class WaveHealthRestore : MonoBehaviour
{
    private enum HealMode
    {
        FixedAmount,   // восстановить фиксированное число ХП
        Percentage,    // восстановить процент от максимального здоровья
        FullHeal       // восстановить полностью
    }

    [Header("Ссылки")]
    [SerializeField] private EnemyWaveManager waveManager;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Настройка восстановления")]
    [SerializeField] private HealMode healMode = HealMode.FixedAmount;
    [Tooltip("Используется, если Heal Mode = Fixed Amount")]
    [SerializeField] private int fixedHealAmount = 25;
    [Tooltip("Используется, если Heal Mode = Percentage. 0.5 = восстановить 50% от максимального ХП")]
    [Range(0f, 1f)]
    [SerializeField] private float healPercentage = 0.5f;

    [Header("Ограничение по волнам (опционально)")]
    [Tooltip("Лечить только после волн, начиная с этого номера (1 = после каждой волны)")]
    [SerializeField] private int healEveryNWaves = 1;

    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = false;

    private void Awake()
    {
        if (waveManager == null)
            waveManager = FindObjectOfType<EnemyWaveManager>();

        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (waveManager != null)
            waveManager.onWaveComplete.AddListener(HandleWaveComplete);
    }

    private void OnDisable()
    {
        if (waveManager != null)
            waveManager.onWaveComplete.RemoveListener(HandleWaveComplete);
    }

    private void HandleWaveComplete(int completedWaveNumber)
    {
        if (playerHealth == null) return;

        // Например, healEveryNWaves = 2 -> лечим только после 2, 4, 6... волны
        if (healEveryNWaves > 1 && completedWaveNumber % healEveryNWaves != 0)
            return;

        switch (healMode)
        {
            case HealMode.FixedAmount:
                playerHealth.Heal(fixedHealAmount);
                break;

            case HealMode.Percentage:
                int amount = Mathf.RoundToInt(playerHealth.MaxHealth * healPercentage);
                playerHealth.Heal(amount);
                break;

            case HealMode.FullHeal:
                playerHealth.HealToFull();
                break;
        }

        if (debugLogs)
            Debug.Log($"[{name}] Волна {completedWaveNumber} завершена — здоровье восстановлено ({healMode}). Текущее ХП: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
    }
}
