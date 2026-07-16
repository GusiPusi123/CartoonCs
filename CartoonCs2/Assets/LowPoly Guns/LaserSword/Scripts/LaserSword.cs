// using UnityEngine;

// public class LaserSword : MonoBehaviour
// {
//     public float attackRadius = 2f; // Радиус урона
//     public int damage = 25; // Урон по врагам
//     public float attackCooldown = 1f; // Время между атаками
//     public Animator animator; // Аниматор для анимации атаки

//     public float moveOffsetAmount = 0.2f; // Максимальное смещение
//     public float moveSmoothTime = 0.1f; // Время сглаживания

//     public Camera mainCamera; // Ссылка на камеру

//     private float lastAttackTime;
//     private Vector3 initialLocalPosition;
//     private Vector3 targetOffset = Vector3.zero;
//     private Vector3 currentVelocity = Vector3.zero;

//     private Vector3 previousCameraEuler;

//     void Start()
//     {
//         initialLocalPosition = transform.localPosition;
//         if (mainCamera == null)
//         {
//             mainCamera = Camera.main;
//         }
//         previousCameraEuler = mainCamera.transform.eulerAngles;
//     }

//     void Update()
//     {
//         // --- Обработка смещения по камере ---
//         Vector3 currentCameraEuler = mainCamera.transform.eulerAngles;
//         float deltaYaw = Mathf.DeltaAngle(previousCameraEuler.y, currentCameraEuler.y);
//         float deltaPitch = Mathf.DeltaAngle(previousCameraEuler.x, currentCameraEuler.x);

//         Vector3 cameraOffset = Vector3.zero;

//         // Смещение по горизонтали (вправо или влево)
//         float yawOffset = deltaYaw / 10f; // чувствительность
//         if (Mathf.Abs(deltaYaw) > 1f)
//         {
//             cameraOffset += mainCamera.transform.right * yawOffset * moveOffsetAmount;
//         }

//         // Смещение по вертикали (вверх или вниз)
//         float pitchOffset = deltaPitch / 10f;
//         if (Mathf.Abs(deltaPitch) > 1f)
//         {
//             cameraOffset += mainCamera.transform.up * pitchOffset * moveOffsetAmount;
//         }

//         // --- Обработка смещения по движению игрока ---
//         float horizontal = Input.GetAxis("Horizontal");
//         float vertical = Input.GetAxis("Vertical");
//         Vector3 inputDirection = new Vector3(vertical, 0, horizontal).normalized;

//         Vector3 movementOffset = Vector3.zero;
//         if (inputDirection.magnitude > 0.1f)
//         {
//             Vector3 right = Vector3.Cross(Vector3.up, inputDirection).normalized;
//             movementOffset = right * moveOffsetAmount;
//         }

//         // Итоговое смещение — сумма
//         targetOffset = cameraOffset + movementOffset;

//         // Плавное смещение меча
//         transform.localPosition = Vector3.SmoothDamp(transform.localPosition, initialLocalPosition + targetOffset, ref currentVelocity, moveSmoothTime);

//         previousCameraEuler = currentCameraEuler;

//         // Атака
//         if (Input.GetKeyDown(KeyCode.Mouse0) && Time.time - lastAttackTime >= attackCooldown)
//         {
//             Attack();
//             lastAttackTime = Time.time;
//         }
//     }

//     void Attack()
//     {
//         if (animator != null)
//         {
//             animator.SetTrigger("Attack");
//         }

//         Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRadius);
//         foreach (Collider enemy in hitEnemies)
//         {
//             if (enemy.gameObject.CompareTag("Enemy"))
//             {
//                 Enemy enemyComponent = enemy.GetComponent<Enemy>();
//                 if (enemyComponent != null)
//                 {
//                     enemyComponent.TakeDamage(damage);
//                 }
//             }
//         }
//     }

//     private void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.red;
//         Gizmos.DrawWireSphere(transform.position, attackRadius);
//     }
// }


using UnityEngine;

public class LaserSword : MonoBehaviour
{
    public float attackRadius = 2f; // Радиус урона
    public int damage = 25; // Урон по врагам
    public float attackCooldown = 1f; // Время между атаками
    public Animator animator; // Аниматор для анимации атаки

    public float moveOffsetAmount = 0.2f; // Максимальное смещение
    public float moveSmoothTime = 0.1f; // Время сглаживания

    public Camera mainCamera; // Ссылка на камеру

    public TrailRenderer trailRenderer; // Ссылка на TrailRenderer, присвойте через инспектор

    private float lastAttackTime;
    private Vector3 initialLocalPosition;
    private Vector3 targetOffset = Vector3.zero;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 previousCameraEuler;

    void Start()
    {
        initialLocalPosition = transform.localPosition;
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Убедитесь, что trailRenderer присвоен через инспектор
        if (trailRenderer == null)
        {
            Debug.LogWarning("TrailRenderer не присвоен. Назначьте его через инспектор.");
        }
        else
        {
            trailRenderer.enabled = false; // по умолчанию выключен
        }

        previousCameraEuler = mainCamera.transform.eulerAngles;
    }

    void Update()
    {
        // Обработка смещения по камере
        Vector3 currentCameraEuler = mainCamera.transform.eulerAngles;
        float deltaYaw = Mathf.DeltaAngle(previousCameraEuler.y, currentCameraEuler.y);
        float deltaPitch = Mathf.DeltaAngle(previousCameraEuler.x, currentCameraEuler.x);

        Vector3 cameraOffset = Vector3.zero;

        float yawOffset = deltaYaw / 10f;
        if (Mathf.Abs(deltaYaw) > 1f)
            cameraOffset += mainCamera.transform.right * yawOffset * moveOffsetAmount;

        float pitchOffset = deltaPitch / 10f;
        if (Mathf.Abs(deltaPitch) > 1f)
            cameraOffset += mainCamera.transform.up * pitchOffset * moveOffsetAmount;

        // Обработка смещения по движению игрока
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(vertical, 0, horizontal).normalized;

        Vector3 movementOffset = Vector3.zero;
        if (inputDirection.magnitude > 0.1f)
        {
            Vector3 right = Vector3.Cross(Vector3.up, inputDirection).normalized;
            movementOffset = right * moveOffsetAmount;
        }

        // Итоговое смещение
        targetOffset = cameraOffset + movementOffset;

        // Плавное смещение меча
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, initialLocalPosition + targetOffset, ref currentVelocity, moveSmoothTime);

        previousCameraEuler = currentCameraEuler;

        // Обработка атаки
        if (Input.GetKeyDown(KeyCode.Mouse0) && Time.time - lastAttackTime >= attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    void Attack()
    {
        // Воспроизведение анимации атаки
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Включение следа
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.enabled = true;
        }

        // Урон врагам
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRadius);
        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                Enemy enemyComponent = enemy.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    enemyComponent.TakeDamage(damage);
                }
            }
        }

        // Отключение следа через короткое время
        Invoke("DisableTrail", 0.2f);
    }

    void DisableTrail()
    {
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
