using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDash : MonoBehaviour
{
    [Header("Камера (для направления рывка)")]
    [SerializeField] private Transform cameraTransform;

    [Header("Параметры рывка")]
    [SerializeField] private float dashSpeed = 20f; // скорость во время рывка (не импульс, а именно скорость)
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
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
                if (debugLogs) Debug.Log($"[{name}] Рывок закончен");
            }
        }
    }

    // Используем FixedUpdate, так как работаем с физикой (Rigidbody)
    private void FixedUpdate()
    {
        if (isDashing)
        {
            // Каждый физический кадр принудительно задаём скорость — трение не успевает её погасить
            rb.velocity = new Vector3(dashDirection.x * dashSpeed, rb.velocity.y, dashDirection.z * dashSpeed);
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
            Debug.Log($"[{name}] Рывок начат, направление: {dashDirection}");
            Debug.DrawRay(transform.position, dashDirection * 5f, Color.red, 1f);
        }
    }

    public bool IsDashing => isDashing;
}
