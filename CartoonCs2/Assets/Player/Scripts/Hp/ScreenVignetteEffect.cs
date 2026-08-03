using UnityEngine;

/// <summary>
/// Управляет экранной виньеткой через обычный UI (CanvasGroup на полноэкранном
/// Image с текстурой виньетки): короткая вспышка при получении урона +
/// постоянная пульсация, когда здоровье опускается ниже порога.
/// Синглтон — вызывается откуда угодно: ScreenVignetteEffect.Instance?.FlashDamage();
///
/// Не зависит от пайплайна рендеринга (Built-in/URP/HDRP) и не требует
/// никаких дополнительных пакетов — работает всегда.
/// </summary>
public class ScreenVignetteEffect : MonoBehaviour
{
    public static ScreenVignetteEffect Instance { get; private set; }

    [Header("Ссылки")]
    [Tooltip("CanvasGroup на полноэкранном UI Image с текстурой виньетки (затемнение по краям экрана)")]
    [SerializeField] private CanvasGroup vignetteGroup;

    [Header("Вспышка при уроне")]
    [SerializeField] private float damageFlashAlpha = 0.6f;
    [Tooltip("Скорость затухания вспышки после урона (единиц альфы в секунду)")]
    [SerializeField] private float damageFlashFadeSpeed = 2f;

    [Header("Пульсация при низком HP")]
    [SerializeField] private float lowHealthMinAlpha = 0.15f;
    [SerializeField] private float lowHealthMaxAlpha = 0.45f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = true;
    [Tooltip("Клавиша для ручного теста вспышки прямо в игре")]
    [SerializeField] private KeyCode testFlashKey = KeyCode.T;

    private bool isLowHealth;
    private float damageFlashCurrent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (vignetteGroup != null)
        {
            vignetteGroup.alpha = 0f;
            if (debugLogs) Debug.Log($"[{name}] Awake. Vignette Group найден: {vignetteGroup.name}");
        }
        else
        {
            Debug.LogWarning($"[{name}] Vignette Group не назначен в инспекторе!");
        }
    }

    private void Update()
    {
        if (vignetteGroup == null) return;

        if (Input.GetKeyDown(testFlashKey))
        {
            if (debugLogs) Debug.Log($"[{name}] Тестовая вспышка по клавише {testFlashKey}");
            FlashDamage();
        }

        damageFlashCurrent = Mathf.MoveTowards(damageFlashCurrent, 0f, damageFlashFadeSpeed * Time.deltaTime);

        if (isLowHealth)
        {
            float pulse01 = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float pulseAlpha = Mathf.Lerp(lowHealthMinAlpha, lowHealthMaxAlpha, pulse01);
            vignetteGroup.alpha = Mathf.Max(pulseAlpha, damageFlashCurrent);
        }
        else
        {
            vignetteGroup.alpha = damageFlashCurrent;
        }
    }

    /// <summary>Короткая вспышка виньетки — вызывать при получении урона.</summary>
    public void FlashDamage()
    {
        damageFlashCurrent = damageFlashAlpha;
    }

    /// <summary>Включает/выключает постоянную пульсацию низкого здоровья.</summary>
    public void SetLowHealth(bool isLow)
    {
        isLowHealth = isLow;
    }
}