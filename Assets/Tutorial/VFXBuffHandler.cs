using UnityEngine;

public class VFXBuffHandler : MonoBehaviour
{
    // Hàm này để Manager ra lệnh xóa icon khi hết lượt buff
    public void DestroyIcon()
    {
        Destroy(gameObject);
    }
}