using UnityEngine;

/// <summary>
/// Gắn script này thả vào các bong bóng thoại (như BongBong_Cha, BongBong_Con, BongBong_GiaNo).
/// Công dụng: Cực sát vị trí ngay trên đỉnh đầu nhân vật, và điều quan trọng nhất: Text sẽ KHÔNG BỊ BAY RA XA hay LẬT NGƯỢC MẶT kị nhân vật quay đầu!
///
/// HƯỚNG DẪN:
/// 1. Bạn lôi thẳng BongBong_GiaNo ... vứt vào lại [UI] (không cần làm con của nhân vật nữa).
/// 2. Gắn script BongBongFollow này vào cái BongBong đó.
/// 3. Ở thanh Inspector của BongBong, bạn lấy chuột kéo thả nhân vật GiaNo vào cái biến Muc Tieu.
/// </summary>
public class BongBongFollow : MonoBehaviour
{
    [Tooltip("Kéo hình nhân vật (GiaNo, TranQuocTuan...) vào đây để bóng bám theo đuổi.")]
    public Transform mucTieu;

    [Tooltip("Độ cao nhô lên từ mạn sườn của NPC (Khuyến nghị 2 => 3)")]
    public float doCaoY = 2.5f;

    // LateUpdate được dùng để di chuyển UI sau khi mọi phép vật lý/chuyển động đi bộ đã tính toán xong
    void LateUpdate()
    {
        if (mucTieu != null)
        {
            // Ghi đè tuyệt đối tọa độ của Bóng thoại sang trên đầu NPC.
            transform.position = mucTieu.position + new Vector3(0, doCaoY, 0);

            // Bẻ khóa góc: Ép cái Scale về cố định + dương (Tránh trường hợp lọt vào làm con mà xoay chữ)
            var worldScale = transform.lossyScale;
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x), 
                Mathf.Abs(transform.localScale.y), 
                Mathf.Abs(transform.localScale.z)
            );
            
            // Ép local Rotation về 0 chặn lật ngược
            transform.localRotation = Quaternion.identity;
        }
    }
}
