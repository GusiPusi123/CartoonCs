using UnityEngine;

/// <summary>
/// Заставляет предмет (оружие) плавно "отставать" от поворота камеры —
/// при резком повороте взгляда оружие на мгновение отклоняется в
/// противоположную сторону и плавно возвращается на место.
/// Вешается на сам объект оружия (или на его родителя-держатель).
/// Не зависит от WeaponRaycast / ShotgunWeapon — работает с любым оружием.
/// </summary>
public class WeaponSway : MonoBehaviour
{
    [Header("Камера")]
    [SerializeField] private Camera playerCamera;

    [Header("Отставание при повороте (Rotation Sway)")]
    [SerializeField] private bool enableRotationSway = true;
    [Tooltip("Насколько сильно оружие отклоняется при повороте камеры")]
    [SerializeField] private float rotationSwayAmount = 2f;
    [Tooltip("Максимальный угол отклонения по каждой оси, градусы")]
    [SerializeField] private float maxRotationSwayAngle = 6f;
    [Tooltip("Меньше значение = резче отклик и быстрее возврат, больше = мягче и \"ленивее\"")]
    [SerializeField] private float rotationSmoothTime = 0.08f;

    [Header("Смещение позиции при повороте (Position Sway)")]
    [SerializeField] private bool enablePositionSway = true;
    [SerializeField] private float positionSwayAmount = 0.01f;
    [SerializeField] private float maxPositionSwayOffset = 0.05f;
    [SerializeField] private float positionSmoothTime = 0.08f;

    [Header("Смещение при движении игрока (опционально)")]
    [SerializeField] private bool enableMovementSway = true;
    [SerializeField] private float movementSwayAmount = 0.02f;
    [SerializeField] private float movementSmoothTime = 0.1f;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private Vector3 currentRotationOffset;
    private Vector3 rotationVelocity;

    private Vector3 currentPositionOffset;
    private Vector3 positionVelocity;

    private Vector3 currentMovementOffset;
    private Vector3 movementVelocity;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Start()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (playerCamera == null) return;

        // Mouse X/Y — это именно СКОРОСТЬ поворота камеры за кадр,
        // а не сам угол, поэтому она отлично подходит для эффекта "отставания":
        // чем резче крутишь мышкой, тем сильнее отклоняется оружие.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        HandleRotationSway(mouseX, mouseY);
        HandlePositionSway(mouseX, mouseY);
        HandleMovementSway();

        ApplyOffsets();
    }

    private void HandleRotationSway(float mouseX, float mouseY)
    {
        if (!enableRotationSway)
        {
            currentRotationOffset = Vector3.SmoothDamp(currentRotationOffset, Vector3.zero, ref rotationVelocity, rotationSmoothTime);
            return;
        }

        // Поворот вправо (mouseX > 0) -> оружие "отстаёт" влево и слегка заваливается,
        // поворот вверх (mouseY > 0) -> оружие отклоняется вниз.
        float targetPitch = Mathf.Clamp(-mouseY * rotationSwayAmount, -maxRotationSwayAngle, maxRotationSwayAngle);
        float targetYaw = Mathf.Clamp(-mouseX * rotationSwayAmount, -maxRotationSwayAngle, maxRotationSwayAngle);
        float targetRoll = Mathf.Clamp(-mouseX * rotationSwayAmount * 0.5f, -maxRotationSwayAngle, maxRotationSwayAngle);

        Vector3 targetRotation = new Vector3(targetPitch, targetYaw, targetRoll);

        currentRotationOffset = Vector3.SmoothDamp(currentRotationOffset, targetRotation, ref rotationVelocity, rotationSmoothTime);
    }

    private void HandlePositionSway(float mouseX, float mouseY)
    {
        if (!enablePositionSway)
        {
            currentPositionOffset = Vector3.SmoothDamp(currentPositionOffset, Vector3.zero, ref positionVelocity, positionSmoothTime);
            return;
        }

        float targetX = Mathf.Clamp(-mouseX * positionSwayAmount, -maxPositionSwayOffset, maxPositionSwayOffset);
        float targetY = Mathf.Clamp(-mouseY * positionSwayAmount, -maxPositionSwayOffset, maxPositionSwayOffset);

        Vector3 targetOffset = new Vector3(targetX, targetY, 0f);

        currentPositionOffset = Vector3.SmoothDamp(currentPositionOffset, targetOffset, ref positionVelocity, positionSmoothTime);
    }

    private void HandleMovementSway()
    {
        if (!enableMovementSway)
        {
            currentMovementOffset = Vector3.SmoothDamp(currentMovementOffset, Vector3.zero, ref movementVelocity, movementSmoothTime);
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 targetOffset = new Vector3(-horizontal * movementSwayAmount, 0f, 0f);

        currentMovementOffset = Vector3.SmoothDamp(currentMovementOffset, targetOffset, ref movementVelocity, movementSmoothTime);

        // подавляем предупреждение о неиспользуемой переменной, если понадобится vertical в будущем
        _ = vertical;
    }

    private void ApplyOffsets()
    {
        transform.localRotation = initialLocalRotation * Quaternion.Euler(currentRotationOffset);
        transform.localPosition = initialLocalPosition + currentPositionOffset + currentMovementOffset;
    }
}
