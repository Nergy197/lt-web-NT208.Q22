using UnityEngine;

/// <summary>
/// Đảm bảo có SavePointUI + ít nhất một SavePoint trên map khi scene chưa đặt sẵn
/// (đặc biệt cho bản web). Theo pattern của QuestMapUIEnsurer.
/// </summary>
public static class SavePointEnsurer
{
    public static void Ensure()
    {
        // Ép về Map mode khi vào map (scene thiếu PlayerInputConnector) — nếu không,
        // Mode có thể kẹt ở Battle/UI sau khi từ trận về → chặn di chuyển/teleport/pause.
        // Không đụng tới Cutscene (intro tự quản mode riêng).
        var ic = InputController.Instance;
        if (ic != null && ic.Mode != InputMode.Cutscene)
            ic.SetMode(InputMode.Map);

        // 0. Pause menu — đảm bảo có instance + dựng UI runtime NGAY (không chờ Start).
        PauseMenuUI.EnsureExists();
        PauseMenuUI.Instance?.BuildRuntimeUI();

        // 1. SavePointUI — tạo runtime nếu scene chưa có.
        if (SavePointUI.Instance == null)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
                SavePointUI.BuildRuntime(canvas.transform);
            else
                Debug.LogWarning("[SavePoint] Không có Canvas — bỏ qua tạo SavePointUI.");
        }

        // KHÔNG tạo SavePoint auto ở spawn nữa: nó tranh phím F với Trụ Dịch Chuyển
        // (cũng ở gần spawn) và gây kẹt. Lưu game đã có qua Pause → "Lưu game", và
        // Trụ Dịch Chuyển vốn tự hồi máu. SavePointUI vẫn dựng sẵn cho save point đặt tay.
    }
}
