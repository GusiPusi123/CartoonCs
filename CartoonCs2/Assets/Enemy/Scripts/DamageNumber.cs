using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Одна всплывающая цифра урона. Появляется с лёгким "поп"-эффектом масштаба,
/// плывёт вверх со случайным небольшим отклонением в сторону, всегда развёрнута
/// лицом к камере, к концу жизни плавно растворяется и самоуничтожается.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class DamageNumber : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float horizontalRandomRange = 0.3f;
    [SerializeField] private float lifetime = 1f;

    [Header("Затухание")]
    [Tooltip("С какой секунды жизни (от 0 до lifetime) число начинает плавно исчезать")]
    [SerializeField] private float fadeStartTime = 0.5f;

    [Header("Поп-эффект появления")]
    [SerializeField] private float popScale = 1.3f;
    [SerializeField] private float popDuration = 0.08f;

    [Header("Цвета")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("Используется, если Init вызван с isCritical = true")]
    [SerializeField] private Color criticalColor = new Color(1f, 0.65f, 0f);
    [Tooltip("Насколько крупнее обычного будет критический урон")]
    [SerializeField] private float criticalScaleMultiplier = 1.3f;

    private TextMeshPro text;
    private Camera mainCamera;
    private Vector3 moveDirection;
    private Vector3 baseScale;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
        mainCamera = Camera.main;
        baseScale = transform.localScale;
    }

    /// <summary>Настраивает и запускает анимацию цифры урона. Вызывается сразу после Instantiate.</summary>
    public void Init(int damageAmount, bool isCritical = false)
    {
        text.text = damageAmount.ToString();
        text.color = isCritical ? criticalColor : normalColor;

        if (isCritical)
            transform.localScale = baseScale * criticalScaleMultiplier;

        float randomX = Random.Range(-horizontalRandomRange, horizontalRandomRange);
        moveDirection = new Vector3(randomX, 1f, 0f).normalized;

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        yield return StartCoroutine(PlayPopEffect());

        Color startColor = text.color;
        float timer = 0f;

        while (timer < lifetime)
        {
            timer += Time.deltaTime;
            transform.position += moveDirection * floatSpeed * Time.deltaTime;

            if (mainCamera != null)
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);

            if (timer > fadeStartTime)
            {
                float fadeProgress = Mathf.InverseLerp(fadeStartTime, lifetime, timer);
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                text.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator PlayPopEffect()
    {
        Vector3 startScale = transform.localScale;
        Vector3 poppedScale = startScale * popScale;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, poppedScale, t / popDuration);
            yield return null;
        }

        t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(poppedScale, startScale, t / popDuration);
            yield return null;
        }

        transform.localScale = startScale;
    }
}
