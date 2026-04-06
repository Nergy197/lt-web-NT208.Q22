using UnityEngine;

public class VFXSelfDestruct : MonoBehaviour
{
    public float delay = 0.5f; // Thời gian diễn xong animation (anh chỉnh theo ý muốn)

    void Start()
    {
        // Tự xóa bản thân sau một khoảng thời gian
        Destroy(gameObject, delay);
    }
}