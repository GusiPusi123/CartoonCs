using UnityEngine;
using UnityEngine.SceneManagement;

public class ColliderPortal : MonoBehaviour
{
    // Идентификатор сцены, на которую мы хотим переместиться
    public string targetSceneName = "TargetScene";

    private void OnTriggerEnter(Collider other)
    {
        // Если это игрок, то переехаем на другую сцену
        if (other.gameObject.tag == "Player")
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
