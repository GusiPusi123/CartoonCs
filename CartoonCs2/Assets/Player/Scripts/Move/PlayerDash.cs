using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDash : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerMovement playerMovement; // нужен, чтобы узнать, стоит ли игрок на земле

    [Header("Параметры рывка")]
    [SerializeField] private float dashSpeed = 40f;
    [SerializeField] private float airDashMultiplier = 0.6f; // во сколько раз слабее рывок в воздухе (1 = как на земле, 0.5 = вдвое слабее)
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Клавиша")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;

    [Header("Эффекты")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private GameObject dashEffect;

    [Header("Диагностика")]
    [SerializeField] private bool debugLogs = false;

    private Rigidbody rb;
    private float cooldownTimer;
    private bool isDashing;
    private float dashTimer;
    private Vector3 dashDirection;
    private float currentDashSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(dashKey) && cooldownTimer <= 0f && !isDashing)
        {
            StartDash();
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                if (debugLogs) Debug.Log($"[{name}] Рывок закончен, скорость на выходе: {rb.velocity.magnitude:F1}");
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.velocity = new Vector3(dashDirection.x * currentDashSpeed, rb.velocity.y, dashDirection.z * currentDashSpeed);
        }
    }

    private void StartDash()
    {
        Vector3 direction;

        if (cameraTransform != null)
        {
            direction = cameraTransform.forward;
        }
        else
        {
            direction = transform.forward;
        }

        direction.y = 0f;
        direction.Normalize();

        dashDirection = direction;

        // Определяем силу рывка в зависимости от того, в воздухе игрок или на земле
        bool isGrounded = playerMovement != null && playerMovement.grounded;
        currentDashSpeed = isGrounded ? dashSpeed : dashSpeed * airDashMultiplier;

        dashTimer = dashDuration;
        isDashing = true;
        cooldownTimer = dashCooldown;

        if (animator != null)
            animator.SetTrigger("Dash");

        if (audioSource != null && dashSound != null)
            audioSource.PlayOneShot(dashSound);

        if (dashEffect != null)
            Instantiate(dashEffect, transform.position, transform.rotation);

        if (debugLogs)
        {
            Debug.Log($"[{name}] Рывок начат. Grounded: {isGrounded}, скорость рывка: {currentDashSpeed:F1}, направление: {dashDirection}");
        }
    }

    public bool IsDashing => isDashing;
}
