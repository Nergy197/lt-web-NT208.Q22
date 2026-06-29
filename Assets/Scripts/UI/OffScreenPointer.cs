using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này vào một UI Image (Mũi tên) nằm trong Canvas.
/// Mũi tên sẽ luôn bám viền màn hình và chĩa về phía Target khi Target nằm ngoài tầm nhìn Camera.
/// </summary>
public class OffScreenPointer : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Kéo cục đen (Portal) vào đây")]
    public Transform target;
    
    [Tooltip("Khoảng cách cách mép viền màn hình (pixel)")]
    public float borderPadding = 50f;

    [Tooltip("Chỉnh số này nếu mũi tên bị chỉa sai hướng (0 đến 360)")]
    public float rotationOffset = -90f;

    private Image pointerImage;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        pointerImage = GetComponent<Image>();
    }

    void Update()
    {
        if (target == null || cam == null || pointerImage == null) return;

        // Chuyển toạ độ cục đen từ Thế giới (World) sang Màn hình (Screen)
        Vector3 screenPos = cam.WorldToScreenPoint(target.position);
        
        // Kiểm tra xem cục đen có đang nằm ngoài màn hình không?
        bool isOffScreen = screenPos.x <= 0 || screenPos.x >= Screen.width || 
                           screenPos.y <= 0 || screenPos.y >= Screen.height;

        if (isOffScreen)
        {
            pointerImage.enabled = true; // Bật mũi tên

            // Đưa toạ độ về gốc tâm màn hình (0,0 ở giữa) để dễ tính toán
            Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0) / 2;
            screenPos -= screenCenter;

            // 1. Tính góc xoay để mũi tên chĩa đúng hướng
            float angle = Mathf.Atan2(screenPos.y, screenPos.x) * Mathf.Rad2Deg;
            // Áp dụng góc xoay bù (rotationOffset) do hình gốc của mỗi người khác nhau
            pointerImage.rectTransform.localEulerAngles = new Vector3(0, 0, angle + rotationOffset); 

            // 2. Tính vị trí bám viền màn hình
            float slope = screenPos.y / screenPos.x;
            
            // Giả sử cắt cạnh dọc (trái/phải)
            float targetX = screenPos.x > 0 ? (Screen.width / 2f) - borderPadding : (-Screen.width / 2f) + borderPadding;
            float targetY = targetX * slope;

            // Nếu nó vọt qua cạnh ngang (trên/dưới) thì tính lại theo cạnh ngang
            if (targetY > (Screen.height / 2f) - borderPadding || targetY < (-Screen.height / 2f) + borderPadding)
            {
                targetY = screenPos.y > 0 ? (Screen.height / 2f) - borderPadding : (-Screen.height / 2f) + borderPadding;
                targetX = targetY / slope;
            }

            // Cộng trả lại tâm màn hình để set Position
            Vector3 finalPos = new Vector3(targetX, targetY, 0) + screenCenter;
            pointerImage.rectTransform.position = finalPos;
        }
        else
        {
            // Tắt mũi tên nếu người chơi đã nhìn thấy cục đen trên màn hình
            pointerImage.enabled = false;
        }
    }
}
