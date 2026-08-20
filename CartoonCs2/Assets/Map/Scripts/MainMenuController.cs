using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Повесить на пустой объект-менеджер меню (например, "MainMenuController").
/// Методы Play() и Exit() назначаются в инспекторе в UnityEvent "On Click"
/// компонента MenuButtonHover3D у соответствующих кнопок.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("Название сцены игры для загрузки при Play")]
    public string gameSceneName = "Game";

    private void Awake()
    {
        Instance = this;
    }

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
