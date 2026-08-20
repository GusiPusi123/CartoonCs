using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Меняет цвет и размер текста при наведении курсора, вызывает Play/Exit при клике.
/// Повесить на тот же GameObject, где Image кнопки (Raycast Target должен быть включён у Image),
/// либо на родительский объект кнопки — главное, чтобы на нём (или на дочернем Image)
/// был компонент с включённым Raycast Target, иначе события наведения не будут ловиться.
/// Требуется EventSystem на сцене (Canvas обычно создаёт его автоматически).
/// </summary>
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Цвета")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;

    [Header("Увеличение текста")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float scaleSpeed = 10f; // скорость плавного перехода

    [Header("Ссылка на текст")]
    [SerializeField] private Text buttonText;

    [Header("Что происходит при клике")]
    [SerializeField] private ButtonAction action = ButtonAction.None;

    private enum ButtonAction { None, Play, Exit }

    private float _targetScale;

    private void Awake()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<Text>();

        _targetScale = normalScale;
        SetColor(normalColor);

        if (buttonText != null)
            buttonText.transform.localScale = Vector3.one * normalScale;
    }

    private void Update()
    {
        // Плавно увеличиваем/уменьшаем текст к целевому масштабу
        if (buttonText == null) return;

        Vector3 current = buttonText.transform.localScale;
        Vector3 target = Vector3.one * _targetScale;
        buttonText.transform.localScale = Vector3.Lerp(current, target, Time.deltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetColor(hoverColor);
        _targetScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetColor(normalColor);
        _targetScale = normalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (action)
        {
            case ButtonAction.Play:
                MainMenuController.Instance?.Play();
                break;
            case ButtonAction.Exit:
                MainMenuController.Instance?.Exit();
                break;
        }
    }

    private void SetColor(Color color)
    {
        if (buttonText != null)
            buttonText.color = color;
    }
}
