using UnityEngine;

public class Crouch : MonoBehaviour
{
    public KeyCode key = KeyCode.LeftControl;

    [Header("Slow Movement")]
    public FirstPersonMovement movement;
    public float movementSpeed = 2;

    [Header("Low Head")]
    public Transform headToLower;
    public float crouchYHeadPosition = 1;

    [Tooltip("Multiplier for spread when crouched (e.g., 0.5 for половинный разброс)")]
    public float crouchSpreadMultiplier = 0.5f;

    [Tooltip("Collider to lower when crouched.")]
    public CapsuleCollider colliderToLower;
    [HideInInspector]
    public float? defaultHeadYLocalPosition;
    [HideInInspector]
    public float? defaultColliderHeight;

    public bool IsCrouched { get; private set; }
    public event System.Action CrouchStart, CrouchEnd;

    void Reset()
    {
        movement = GetComponentInParent<FirstPersonMovement>();
        if (movement != null)
        {
            headToLower = movement.GetComponentInChildren<Camera>().transform;
            colliderToLower = movement.GetComponentInChildren<CapsuleCollider>();
        }
    }

    void LateUpdate()
    {
        if (Input.GetKey(key))
        {
            // Приседание
            if (!IsCrouched)
            {
                IsCrouched = true;
                SetSpeedOverrideActive(true);
                CrouchStart?.Invoke();
            }

            // Нижний голова
            if (headToLower)
            {
                if (!defaultHeadYLocalPosition.HasValue)
                {
                    defaultHeadYLocalPosition = headToLower.localPosition.y;
                }
                headToLower.localPosition = new Vector3(headToLower.localPosition.x, crouchYHeadPosition, headToLower.localPosition.z);
            }

            // Collider
            if (colliderToLower)
            {
                if (!defaultColliderHeight.HasValue)
                {
                    defaultColliderHeight = colliderToLower.height;
                }
                float loweringAmount = defaultHeadYLocalPosition.HasValue ? defaultHeadYLocalPosition.Value - crouchYHeadPosition : defaultColliderHeight.Value * 0.5f;
                colliderToLower.height = Mathf.Max(defaultColliderHeight.Value - loweringAmount, 0);
                colliderToLower.center = Vector3.up * colliderToLower.height * 0.5f;
            }
        }
        else
        {
            if (IsCrouched)
            {
                // Встать
                if (headToLower && defaultHeadYLocalPosition.HasValue)
                {
                    headToLower.localPosition = new Vector3(headToLower.localPosition.x, defaultHeadYLocalPosition.Value, headToLower.localPosition.z);
                }
                if (colliderToLower && defaultColliderHeight.HasValue)
                {
                    colliderToLower.height = defaultColliderHeight.Value;
                    colliderToLower.center = Vector3.up * colliderToLower.height * 0.5f;
                }
                IsCrouched = false;
                SetSpeedOverrideActive(false);
                CrouchEnd?.Invoke();
            }
        }
    }

    #region Speed override
    void SetSpeedOverrideActive(bool state)
    {
        if (!movement) return;
        if (state)
        {
            if (!movement.speedOverrides.Contains(SpeedOverride))
                movement.speedOverrides.Add(SpeedOverride);
        }
        else
        {
            if (movement.speedOverrides.Contains(SpeedOverride))
                movement.speedOverrides.Remove(SpeedOverride);
        }
    }

    float SpeedOverride() => movementSpeed;
    #endregion
}