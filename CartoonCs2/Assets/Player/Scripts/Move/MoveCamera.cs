// using UnityEngine;

// public class MoveCamera : MonoBehaviour {

//     public Transform player;

//     void Update() {
//         transform.position = player.transform.position;
//     }
// }

using UnityEngine;

public class MoveCamera : MonoBehaviour {

    public Transform player;
    public PlayerMovement playerMovement; // перетащи сюда компонент PlayerMovement

    [Header("Camera Shake / Bob")]
    public bool enableShake = true;
    public float shakeThresholdSpeed = 12f;   // скорость, начиная с которой появляется тряска
    public float maxShakeSpeed = 25f;         // скорость, при которой тряска максимальна
    public float maxShakeAmount = 0.06f;      // максимальная амплитуда смещения камеры
    public float shakeFrequency = 18f;        // частота "дрожи"
    public float shakeSmoothing = 20f;        // сглаживание перехода тряски

    private Rigidbody rb;
    private float currentShakeAmount;
    private Vector3 shakeOffset;
    private float noiseTime;

    void Start() {
        if (playerMovement != null)
            rb = playerMovement.GetComponent<Rigidbody>();
    }

    void Update() {
        Vector3 targetPos = player.transform.position;

        if (enableShake && rb != null) {
            float speed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;

            // Насколько сильно должна трястись камера при текущей скорости (0..1)
            float t = Mathf.InverseLerp(shakeThresholdSpeed, maxShakeSpeed, speed);
            float targetShake = t * maxShakeAmount;

            // Плавно подводим текущую амплитуду к целевой, чтобы не было резких скачков
            currentShakeAmount = Mathf.Lerp(currentShakeAmount, targetShake, Time.deltaTime * shakeSmoothing);

            if (currentShakeAmount > 0.0001f) {
                noiseTime += Time.deltaTime * shakeFrequency;

                // Perlin noise даёт плавную, но "случайную" дрожь без резких дёрганий
                float offsetX = (Mathf.PerlinNoise(noiseTime, 0f) - 0.5f) * 2f;
                float offsetY = (Mathf.PerlinNoise(0f, noiseTime) - 0.5f) * 2f;

                shakeOffset = new Vector3(offsetX, offsetY, 0f) * currentShakeAmount;
            } else {
                shakeOffset = Vector3.zero;
            }

            targetPos += shakeOffset;
        }

        transform.position = targetPos;
    }
}
