using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Отвечает за полёт трассера от точки спавна до цели. Висит НЕПОСРЕДСТВЕННО
/// на самом трассере, а не на оружии, которое его выпустило — поэтому корутина
/// движения продолжает работать, даже если оружие в этот момент деактивировано
/// (например, игрок переключил оружие, зажав кнопку стрельбы у автоматического).
/// </summary>
public class TracerProjectile : MonoBehaviour
{
    /// <summary>Запускает полёт трассера к targetPoint. onArrived вызывается по прилёту, перед уничтожением объекта.</summary>
    public void Launch(Vector3 targetPoint, float speed, Action onArrived)
    {
        StartCoroutine(Move(targetPoint, speed, onArrived));
    }

    private IEnumerator Move(Vector3 targetPoint, float speed, Action onArrived)
    {
        float distance = Vector3.Distance(transform.position, targetPoint);
        float duration = speed > 0f ? distance / speed : 0f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPoint, elapsed / duration);
            yield return null;
        }

        transform.position = targetPoint;
        onArrived?.Invoke();
        Destroy(gameObject);
    }
}
