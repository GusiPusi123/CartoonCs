using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasTrigger : MonoBehaviour
{
    public GameObject canvasUI;      // Canvas
    public MonoBehaviour cameraLook; // Скрипт поворота камеры (MouseLook и т.д.)

    private bool playerInTrigger = false;

    void Start()
    {
        // Скрываем Canvas
        canvasUI.SetActive(false);

        // Блокируем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Если игрок в триггере и нажал B
        if (playerInTrigger && Input.GetKeyDown(KeyCode.B))
        {
            bool isActive = !canvasUI.activeSelf;
            canvasUI.SetActive(isActive);

            if (isActive)
            {
                // Показываем курсор
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Отключаем поворот камеры
                if (cameraLook != null)
                    cameraLook.enabled = false;
            }
            else
            {
                // Прячем курсор
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // Включаем поворот камеры
                if (cameraLook != null)
                    cameraLook.enabled = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;

            // Закрываем Canvas
            canvasUI.SetActive(false);

            // Блокируем курсор
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Включаем поворот камеры
            if (cameraLook != null)
                cameraLook.enabled = true;
        }
    }
}