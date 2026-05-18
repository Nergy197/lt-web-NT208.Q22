using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên root prefab UI teleport. Gán field rồi kéo prefab vào
/// TeleportPillar hoặc TeleportMenuUI (xem tooltip trên các field đó).
/// </summary>
public class TeleportMenuBindings : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public Transform buttonContainer;
    public Button closeButton;
    [Tooltip("Nút mẫu (tắt active); runtime sẽ clone để mỗi map một nút.")]
    public GameObject buttonTemplate;
}
