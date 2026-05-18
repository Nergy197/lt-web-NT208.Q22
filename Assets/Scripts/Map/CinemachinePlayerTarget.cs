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
        if (vcam.Target.TrackingTarget != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        vcam.Target.TrackingTarget = player.transform;
        Debug.Log($"[CinemachinePlayerTarget] Gán Player → {gameObject.name}");

        // Snap camera ngay về vị trí player, không chờ damping
        vcam.ForceCameraPosition(
            new Vector3(player.transform.position.x, player.transform.position.y, vcam.transform.position.z),
            vcam.transform.rotation
        );

        var confiner = GetComponent<CinemachineConfiner2D>();
        if (confiner != null) confiner.InvalidateBoundingShapeCache();
    }
}
