using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Меню паузы: открывается/закрывается по Escape, останавливает время
/// (Time.timeScale = 0), отключает управление игроком и стрельбу на время
/// паузы (иначе камера продолжит вращаться, а оружие — стрелять, так как
/// Update() у них не завязан на timeScale и работает даже при полной остановке
/// времени). Кнопки: Resume, Restart Level, Quit to Menu.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Что отключать на время паузы")]
    [Tooltip("Сюда перетащи PlayerMovement, PlayerDash, WeaponSwitcher, скрипт вращения камеры (MouseLook/CameraController), активные скрипты оружия (WeaponRaycast/ShotgunWeapon) — всё, что не должно работать, пока открыто меню")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnPause;

    [Header("Сцены")]
    [Tooltip("Имя сцены главного меню (должно быть добавлено в Build Settings)")]
    [SerializeField] private string mainMenuSceneName = "MainSceneMenu";

    [Header("Клавиша паузы")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused;

    private void Awake()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    private void Update()
    {
        // Важно: этот Update НЕ должен быть отключён во время паузы (сам PauseMenu
        // не входит в scriptsToDisableOnPause), иначе Escape перестанет работать
        // и закрыть меню будет невозможно.
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    // Принудительно держим курсор разлоченным, пока меню открыто.
    // Работает в LateUpdate, то есть ПОСЛЕ всех обычных Update() —
    // в том числе после скрипта камеры, если он вдруг не попал
    // в scriptsToDisableOnPause и каждый кадр пытается залочить курсор обратно.
    // Это подстраховка на случай, если забудешь добавить в массив
    // какой-нибудь новый скрипт в будущем.
    private void LateUpdate()
    {
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;

        SetPlayerControlsEnabled(false);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;

        SetPlayerControlsEnabled(true);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>Кнопка "Restart" — перезапускает текущий уровень.</summary>
    public void RestartLevel()
    {
        // Время обязательно возвращаем к норме ДО загрузки сцены,
        // иначе новая сцена загрузится уже "замороженной".
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Кнопка "Quit to Menu" — выходит в главное меню.</summary>
    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        foreach (MonoBehaviour script in scriptsToDisableOnPause)
        {
            if (script != null)
                script.enabled = enabled;
        }
    }

    public bool IsPaused => isPaused;
}
