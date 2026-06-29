using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalToScene : MonoBehaviour
{
    [Tooltip("Tên của Scene muốn chuyển tới (vd: Chapter6_Village)")]
    public string targetScene = "Chapter6_Village";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem đối tượng chạm vào cổng có phải là Player không
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"[Portal] Player bước vào cổng! Chuyển sang scene: {targetScene}");
            SceneManager.LoadScene(targetScene);
        }
    }
}
