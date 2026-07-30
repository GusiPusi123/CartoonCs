using UnityEngine;

public class RotateText : MonoBehaviour
{
    public GameObject[] objects; // Массив объектов для вращения
    public Vector3 rotationAxis = Vector3.up; // Ось вращения, по умолчанию Y
    public float rotationSpeed = 30f; // Скорость вращения в градусах в секунду

    void Update()
    {
        foreach (GameObject obj in objects)
        {
            // Вращение вокруг выбранной оси
            obj.transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }
}