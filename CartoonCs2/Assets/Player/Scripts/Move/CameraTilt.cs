// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class CameraTilt : MonoBehaviour
// {
//     [Header("Mouse Look")]
//     [SerializeField] private float sensitivity = 1.0f;
//     [SerializeField] private float verticalLimit = 80f;
//     public float tiltAmount = 15f; // Примерное значение наклона
//     public float tiltStartSpeed = 5f; // Скорость наклона к цели
//     public float tiltEndSpeed = 5f;

//     [Header("References")]
//     [SerializeField] private Transform orientation;
//     [SerializeField] private Transform body;

//     private float xRotation;
//     private float yRotation;

//     private float currentX;
//     private float currentY;

//     private float currentTilt = 0f;
//     private float targetTilt = 0f;

//     private void Start()
//     {
//         Cursor.lockState = CursorLockMode.Locked;
//         Cursor.visible = false;

//         currentX = xRotation;
//         currentY = yRotation;
//     }

//     private void Update()
//     {
//         MouseLook();
//         Tilt(); // вызываем наклон
//     }

//     private void MouseLook()
//     {
//         float mouseX = Input.GetAxis("Mouse X") * sensitivity;
//         float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

//         yRotation += mouseX;
//         xRotation -= mouseY;
//         xRotation = Mathf.Clamp(xRotation, -verticalLimit, verticalLimit);

//         transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);

//         if (orientation != null)
//             orientation.localRotation = Quaternion.Euler(0, yRotation, 0);

//         if (body != null)
//             body.localRotation = Quaternion.Euler(0, yRotation, 0);
//     }

//     private void Tilt()
//     {
//         bool leftStrafe = Input.GetKey(KeyCode.A);
//         bool rightStrafe = Input.GetKey(KeyCode.D);

//         if (leftStrafe && !rightStrafe)
//             targetTilt = tiltAmount;
//         else if (rightStrafe && !leftStrafe)
//             targetTilt = -tiltAmount;
//         else
//             targetTilt = 0f;

//         float smoothSpeed = (targetTilt == 0) ? tiltEndSpeed : tiltStartSpeed;
//         currentTilt = Mathf.Lerp(currentTilt, targetTilt, smoothSpeed * Time.deltaTime);

//         // Применяем наклон к камере (или к объекту)
//         // Например, наклон по Z-оси
//         transform.localRotation = Quaternion.Euler(xRotation, yRotation, currentTilt);
//     }
// }


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTilt : MonoBehaviour
{
    [Header("Tilt Settings")]
    public float tiltAmount = 15f; // Максимальный угол наклона
    public float tiltStartSpeed = 5f; // Скорость наклона при начале движения
    public float tiltEndSpeed = 5f; // Скорость возврата к нейтральной позиции

    private float currentTilt = 0f; // Текущий угол наклона
    private float targetTilt = 0f; // Целевой угол наклона

    private void Update()
    {
        HandleTilt();
    }

    private void HandleTilt()
    {
        bool leftStrafe = Input.GetKey(KeyCode.A);
        bool rightStrafe = Input.GetKey(KeyCode.D);

        if (leftStrafe && !rightStrafe)
            targetTilt = tiltAmount;
        else if (rightStrafe && !leftStrafe)
            targetTilt = -tiltAmount;
        else
            targetTilt = 0f;

        float smoothSpeed = (targetTilt == 0) ? tiltEndSpeed : tiltStartSpeed;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, smoothSpeed * Time.deltaTime);

        // Применяем наклон по Z-оси
        transform.localRotation = Quaternion.Euler(0, 0, currentTilt);
    }
}