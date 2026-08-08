using UnityEngine;

/// <summary>
/// Универсальная тряска камеры. Вешается на камеру (обычно автоматически,
/// первым же вызовом Shake()) и работает независимо от источника тряски —
/// взрыв бочки, отдача, урон игроку и т.п. могут дёргать её параллельно,
/// сильнейшая тряска на кадр просто перекрывает более слабую.
/// </summary>
public class CameraShake : MonoBehaviour
{
    private float shakeTimeRemaining;
    private float shakeDuration;
    private float shakeIntensity;

    // Смещение, которое тряска добавила от базовой позиции В ПРОШЛОМ кадре.
    // Храним именно его (а не саму базовую позицию), чтобы корректно работать
    // поверх других скриптов (sway, recoil), которые тоже двигают localPosition
    // каждый кадр — мы лишь добавляем/убираем СВОЙ вклад, не трогая их.
    private Vector3 previousShakeOffset = Vector3.zero;

    /// <summary>Запустить тряску. Если тряска уже идёт — берём максимум из текущей и новой силы,
    /// чтобы несколько взрывов подряд не складывали смещение (не даём камере "улететь").</summary>
    public void Shake(float intensity, float duration)
    {
        if (intensity > shakeIntensity)
            shakeIntensity = intensity;

        shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, duration);
        shakeDuration = Mathf.Max(shakeDuration, duration);
    }

    private void LateUpdate()
    {
        // Сначала всегда убираем вклад ПРОШЛОГО кадра — иначе смещения складываются
        // друг с другом и камера "уползает" всё дальше от исходной точки, никогда не возвращаясь.
        if (previousShakeOffset != Vector3.zero)
        {
            transform.localPosition -= previousShakeOffset;
            previousShakeOffset = Vector3.zero;
        }

        if (shakeTimeRemaining <= 0f)
        {
            // Тряска закончилась — камера уже вернулась в исходную точку строкой выше,
            // дальше ничего не трогаем, чтобы не мешать другим скриптам (sway, recoil и т.п.)
            shakeIntensity = 0f;
            shakeDuration = 0f;
            return;
        }

        shakeTimeRemaining -= Time.deltaTime;

        // Затухание силы тряски к концу длительности, чтобы не было резкого "обрыва" в конце
        float t = Mathf.Clamp01(shakeTimeRemaining / shakeDuration);
        float currentIntensity = shakeIntensity * t;

        Vector3 offset = Random.insideUnitSphere * currentIntensity;

        transform.localPosition += offset;
        previousShakeOffset = offset;
    }
}
