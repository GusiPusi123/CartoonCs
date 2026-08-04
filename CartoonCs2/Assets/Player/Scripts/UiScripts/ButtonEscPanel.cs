// using UnityEngine;
// using UnityEngine.UI; // Для работы с UI Button

// public class ButtonEscPanel : MonoBehaviour
// {
//     public GameObject pausePanel;
//     public Button closeButton; // UI кнопка, которую можно назначить через инспектор

//     private bool isPaused = false;

//     void Start()
//     {
//         if (closeButton != null)
//         {
//             closeButton.onClick.AddListener(ClosePausePanel);
//         }
//     }

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.Escape))
//         {
//             TogglePause();
//         }
//     }

//     private void TogglePause()
//     {
//         if (pausePanel == null)
//         {
//             Debug.LogWarning("Pause Panel не назначен!");
//             return;
//         }

//         isPaused = !isPaused;
//         pausePanel.SetActive(isPaused);
//         ManageCursorAndTime(isPaused);
//     }

//     public void ManageCursorAndTime(bool pauseState)
//     {
//         if (pauseState)
//         {
//             Cursor.visible = true;
//             Cursor.lockState = CursorLockMode.None;
//             Time.timeScale = 0f;
//         }
//         else
//         {
//             Cursor.visible = false;
//             Cursor.lockState = CursorLockMode.Locked;
//             Time.timeScale = 1f;
//         }
//     }

//     // Метод вызывается при нажатии на UI кнопку
//     public void ClosePausePanel()
//     {
//         if (isPaused)
//         {
//             TogglePause();
//         }
//     }
// }

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Управление паузой, фиксирование камеры и UI‑кнопками «Restart» / «Menu».

/// </summary>
public class ButtonEscPanel : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;          // Пауза‑панель
    public Button closeButton;             // Кнопка «Resume» (закрыть)
    public Button restartButton;           // Кнопка «Restart Level»
    public Button menuButton;              // Кнопка «Back to Menu»

    [Header("Camera")]
    // Любой скрипт, который отвечает за вращение/перемещение камеры.
    // Например, FirstPersonController, MouseLook и т.п.
    public MonoBehaviour cameraController;

    [Header("Menu Settings")]
    [Tooltip("Имя сцены, которая открывается при нажатии «Back to Menu»")]
    public string menuSceneName = "MainMenu";

    private bool isPaused = false;

    #region Unity callbacks
    private void Start()
    {
        // Подписываемся на UI‑кнопки, если они назначены в инспекторе
        if (closeButton != null)   closeButton.onClick.AddListener(ClosePausePanel);
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (menuButton != null)    menuButton.onClick.AddListener(BackToMenu);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    #endregion

    #region Pause handling
    /// <summary>
    /// Переключает состояние паузы.
    /// </summary>
    private void TogglePause()
    {
        SetPauseState(!isPaused);
    }

    /// <summary>
    /// Открывает/закрывает панель паузы и управляет курсором, временем и камерой.
    /// </summary>
    /// <param name="pause">true – включить паузу, false – снять.</param>
    private void SetPauseState(bool pause)
    {
        if (pausePanel == null)
        {
            Debug.LogWarning("[ButtonEscPanel] Pause panel is not assigned!");
            return;
        }

        isPaused = pause;
        pausePanel.SetActive(isPaused);
        ManageCursorAndTime(isPaused);
        ManageCameraLock(isPaused);
    }

    /// <summary>
    /// Делает курсор видимым/невидимым и ставит/снимает Time.timeScale.
    /// </summary>
    private void ManageCursorAndTime(bool pauseState)
    {
        if (pauseState)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0f;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Отключает контроллер камеры, пока игра находится на паузе.
    /// </summary>
    private void ManageCameraLock(bool lockCamera)
    {
        if (cameraController != null)
        {
            cameraController.enabled = !lockCamera;
        }
    }

    /// <summary>
    /// Вызывается UI‑кнопкой «Resume» (или клавишей Esc, если панель уже открыта).
    /// </summary>
    public void ClosePausePanel()
    {
        if (isPaused)
            SetPauseState(false);
    }
    #endregion

    #region UI button callbacks
    /// <summary>
    /// Перезапуск текущей сцены.
    /// </summary>
    private void RestartLevel()
    {
        // Снимаем паузу, чтобы Time.timeScale вернулся к 1.
        SetPauseState(false);
        // Перезагружаем текущую сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Переход в главное меню (или любую другую сцену).
    /// </summary>
    private void BackToMenu()
    {
        // Снимаем паузу, иначе Time.timeScale останется 0 в новой сцене.
        SetPauseState(false);
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogWarning("[ButtonEscPanel] menuSceneName is empty – cannot load menu.");
        }
    }
    #endregion
}
