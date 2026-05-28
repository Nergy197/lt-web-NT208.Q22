using UnityEngine;

/// <summary>
/// Bootstrap chung cho mọi Cutscene scene.
/// Đảm bảo InputController và GameManager tồn tại khi mở thẳng scene trong Editor.
/// Trong build thực, các singleton đã được tạo từ StartScene (DontDestroyOnLoad).
///
/// Setup: gắn script này vào một GameObject rỗng tên "Bootstrap" trong mỗi cutscene scene.
/// </summary>
public class CutsceneBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (InputController.Instance == null)
        {
            new GameObject("InputController").AddComponent<InputController>();
            Debug.Log("[CutsceneBootstrap] Tạo InputController.");
        }

        if (GameManager.Instance == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
            Debug.Log("[CutsceneBootstrap] Tạo GameManager.");
        }
    }

    void Start()
    {
        InputController.Instance?.SetMode(InputMode.Cutscene);
        Debug.Log("[CutsceneBootstrap] InputMode → Cutscene.");
    }
}
