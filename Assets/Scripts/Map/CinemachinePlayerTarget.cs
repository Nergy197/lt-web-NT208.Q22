using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Tự động gán Player cho CinemachineCamera khi runtime.
/// Gắn vào object "cm vcam1" bên trong Battlefield-Map prefab
/// hoặc dùng script này trên bất kỳ CinemachineCamera nào.
///
/// Giải quyết vấn đề: vcam trong prefab không thể reference Player
/// (vì Player nằm ngoài prefab), nên TrackingTarget luôn = null.
/// Script này tìm Player bằng tag rồi gán lúc runtime.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class CinemachinePlayerTarget : MonoBehaviour
{
    private CinemachineCamera vcam;

    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
    }

    void Start()
    {
        AssignPlayer();
    }

    void Update()
    {
        // Thử lại nếu Player chưa spawn kịp
        if (vcam != null && vcam.Target.TrackingTarget == null)
        {
            AssignPlayer();
        }
    }

    void AssignPlayer()
    {
        if (vcam == null) return;
        if (vcam.Target.TrackingTarget != null) return; // Đã có target

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            vcam.Target.TrackingTarget = player.transform;
            Debug.Log($"[CinemachinePlayerTarget] Đã gán Player cho {gameObject.name}");

            // Invalidate confiner cache để camera snap đúng vị trí
            CinemachineConfiner2D confiner = GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                confiner.InvalidateBoundingShapeCache();
            }
        }
    }
}
