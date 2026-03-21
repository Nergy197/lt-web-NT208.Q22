using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Panel UI xuất hiện khi player bước vào SavePoint.
/// Kéo thả Panel GameObject trong BattleScene Canvas vào Inspector.
/// </summary>
public class SavePointUI : MonoBehaviour
{
    public static SavePointUI Instance;

    [Header("Panel Root")]
    [SerializeField] private GameObject panel; // Root Panel - bật/tắt để hiện/ẩn

    [Header("Buttons")]
    [SerializeField] private Button healButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button swapButton; // Nút thủ công đổi party
    [SerializeField] private Button closeButton;

    [Header("Status Text (optional)")]
    [SerializeField] private TMPro.TMP_Text statusText;

    private SavePoint currentPoint;

    // ================= INIT =================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Gắn sự kiện nút
        healButton?.onClick.AddListener(OnHeal);
        saveButton?.onClick.AddListener(OnSave);
        swapButton?.onClick.AddListener(OnSwapOrder);
        closeButton?.onClick.AddListener(OnClose);

        // Tắt panel khi khởi tạo
        panel?.SetActive(false);
    }

    // ================= OPEN/CLOSE =================

    public void Open(SavePoint point)
    {
        if (panel == null)
        {
            Debug.LogError("[SavePointUI] LỖI THIẾU PANEL: Biến 'panel' chưa được gán trong Inspector! Hãy kéo GameObject Panel vào ô Panel Root của SavePointUI!");
            return;
        }

        currentPoint = point;
        panel.SetActive(true);

        // Chặn di chuyển player khi menu đang mở
        InputController.Instance?.SetMode(InputMode.UI);

        SetStatus("Party Management. Bấm F để đổi vị trí.");
        Debug.Log($"[SavePointUI] Opened party menu.");
    }

    public void OnClose()
    {
        panel?.SetActive(false);

        // Trả lại quyền kiểm soát di chuyển
        InputController.Instance?.SetMode(InputMode.Map);
        currentPoint = null;

        Debug.Log("[SavePointUI] Closed");
    }

    // ================= ACTIONS =================

    public void OnHeal()
    {
        Debug.Log("[SavePointUI] Đã bấm nút Hồi máu (OnHeal)");

        if (GameManager.Instance == null)
        {
            Debug.LogError("[SavePointUI] LỖI: GameManager.Instance = null!");
            SetStatus("Lỗi: Không tìm thấy GameManager!");
            return;
        }

        if (GameManager.Instance.playerParty == null)
        {
            Debug.LogError("[SavePointUI] LỖI: playerParty trong GameManager = null!");
            SetStatus("Lỗi: Không tìm thấy dữ liệu Party!");
            return;
        }

        if (GameManager.Instance.playerParty.Members == null || GameManager.Instance.playerParty.Members.Count == 0)
        {
            Debug.LogWarning("[SavePointUI] CẢNH BÁO: Party trống, không có thành viên nào để hồi máu.");
            SetStatus("Cảnh báo: Party trống!");
            return;
        }

        int healedCount = 0;
        foreach (var member in GameManager.Instance.playerParty.Members)
        {
            if (!member.IsAlive) member.Revive(member.MaxHP);
            member.HealFull();
            healedCount++;
        }

        SetStatus("Đã hồi máu toàn bộ party!");
        Debug.Log($"[SavePointUI] Healed all. (Đã hồi cho {healedCount} thành viên)");
    }

    public void OnSave()
    {
        Debug.Log("[SavePointUI] Đã bấm nút Lưu (OnSave)");

        if (GameManager.Instance == null)
        {
            Debug.LogError("[SavePointUI] LỖI: GameManager.Instance = null!");
            SetStatus("Lỗi: Không tìm thấy GameManager!");
            return;
        }

        if (currentPoint == null)
        {
            Debug.LogError("[SavePointUI] LỖI: currentPoint = null! Player chưa đứng ở điểm Save nào.");
            SetStatus("Lỗi: Không xác định được vị trí!");
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;

        GameManager.Instance.SaveAtPoint(currentPoint.pointId, sceneName);
        QuestManager.Instance?.SaveProgress();   // lưu tiến độ quest cùng party

        SetStatus("Đang lưu...");

        // Lưu thêm một lần với callback để cập nhật UI khi hoàn tất
        GameManager.Instance.SavePlayerPartyWithCallback((serverOk) =>
        {
            if (serverOk)
                SetStatus("Lưu thành công!");
            else
                SetStatus("Đã lưu offline!");
        });

        Debug.Log($"[SavePointUI] Saving at {currentPoint.pointId} in {sceneName}");
    }

    public void OnSwapOrder()
    {
        if (GameManager.Instance?.playerParty == null) return;

        var party = GameManager.Instance.playerParty;
        if (party.Members.Count < 2)
        {
            SetStatus("Cần 2 người để đổi chỗ.");
            return;
        }

        party.SwapMembers(0, 1);

        string name1 = party.Members[0].entityName;
        string name2 = party.Members[1].entityName;
        SetStatus($"Đã đổi: {name1} ↔ {name2}");
        Debug.Log($"[SavePointUI] Party Order Swapped: {name1}, {name2}");
    }

    // ================= HELPER =================

    void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }
}
