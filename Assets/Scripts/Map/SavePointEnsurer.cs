using UnityEngine;

/// <summary>
/// Đảm bảo có SavePointUI + ít nhất một SavePoint trên map khi scene chưa đặt sẵn
/// (đặc biệt cho bản web). Theo pattern của QuestMapUIEnsurer.
/// </summary>
public static class SavePointEnsurer
{
    public static void Ensure()
    {
        // 1. SavePointUI — tạo runtime nếu scene chưa có.
        if (SavePointUI.Instance == null)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
                SavePointUI.BuildRuntime(canvas.transform);
            else
                Debug.LogWarning("[SavePoint] Không có Canvas — bỏ qua tạo SavePointUI.");
        }

        // 2. Một SavePoint nếu scene chưa có cái nào (đặt tại vị trí player spawn).
        if (Object.FindAnyObjectByType<SavePoint>() == null)
        {
            var go = new GameObject("RuntimeSavePoint");

            var player = GameObject.FindWithTag("Player");
            if (player != null)
                go.transform.position = player.transform.position;

            var sp = go.AddComponent<SavePoint>();
            sp.pointId = "web_autosavepoint";
            sp.useDistanceDetection = true; // phát hiện theo khoảng cách → không cần collider, overlap-spawn vẫn OK
            sp.detectionRadius = 2.0f;

            Debug.Log("[SavePoint] Đã tạo SavePoint runtime tại vị trí player spawn (nhấn F để lưu/hồi máu).");
        }
    }
}
