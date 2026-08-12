using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; // если используешь TextMeshPro, если нет — можешь удалить

/// <summary>
/// Меняет цвет текста при наведении курсора мыши.
/// Повесь этот скрипт на тот же GameObject, где находится Text/TextMeshProUGUI
/// (или на родительский объект с Image/Button — главное, чтобы на нём был Raycast Target).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HoverTextColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Цвета")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;

    [Header("Ссылки на текст (заполни то, что используешь)")]
    [SerializeField] private Text legacyText;
    [SerializeField] private TextMeshProUGUI tmpText;

    private void Awake()
    {
        // Если ссылки не заданы вручную — попробуем найти автоматически
        if (legacyText == null) legacyText = GetComponentInChildren<Text>();
        if (tmpText == null) tmpText = GetComponentInChildren<TextMeshProUGUI>();

        SetColor(normalColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetColor(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetColor(normalColor);
    }

    private void SetColor(Color color)
    {
        if (legacyText != null) legacyText.color = color;
        if (tmpText != null) tmpText.color = color;
    }
}
