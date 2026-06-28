using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Panel UI xuất hiện khi player bước vào SavePoint.
/// Kéo thả Panel GameObject trong Chapter5a_Battle Canvas vào Inspector.
/// </summary>
public class SavePointUI : MonoBehaviour
{
    public static SavePointUI Instance;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

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

        // SFX mở panel
        SFXManager.Instance?.PlayPanelOpen();

        // Chặn di chuyển player khi menu đang mở
        InputController.Instance?.SetMode(InputMode.UI);

        SetStatus("Party Management. Bấm F để đổi vị trí.");
        Debug.Log($"[SavePointUI] Opened party menu.");
    }

    public void OnClose()
    {
        panel?.SetActive(false);

        // SFX đóng panel
        SFXManager.Instance?.PlayPanelClose();

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

        // Lưu Save Point (hồi máu + lưu local + backup server) — CHỈ gọi 1 lần
        // SaveRoutine() bên trong đã tự gọi QuestManager.SaveProgress()
        GameManager.Instance.SaveAtPoint(currentPoint.pointId, sceneName);

        // SFX lưu thành công
        SFXManager.Instance?.PlaySaveSuccess();

        SetStatus("Đã lưu thành công!");

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

    // ================= RUNTIME BUILDER =================

    /// <summary>
    /// Tạo SavePointUI hoàn chỉnh (panel + 4 nút + status) bằng code — dùng cho bản web
    /// nếu scene chưa đặt sẵn. Trả về Instance.
    /// </summary>
    public static SavePointUI BuildRuntime(Transform canvasRoot)
    {
        var go = new GameObject("SavePointUI");
        go.transform.SetParent(canvasRoot, false);
        var ui = go.AddComponent<SavePointUI>(); // Awake chạy với field null (no-op an toàn)

        // Overlay mờ toàn màn
        var overlay = NewRect("Panel", canvasRoot);
        var oImg = overlay.gameObject.AddComponent<Image>();
        oImg.color = new Color(0, 0, 0, 0.78f);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero; overlay.offsetMax = Vector2.zero;

        // Card giữa màn
        var card = NewRect("Card", overlay);
        var cImg = card.gameObject.AddComponent<Image>();
        cImg.color = new Color(0.08f, 0.1f, 0.16f, 0.98f);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(420, 360);

        MakeLabel(card, "ĐIỂM LƯU", new Vector2(0, 140), 26, true, new Color(0.75f, 0.9f, 1f));
        var status = MakeLabel(card, "", new Vector2(0, 96), 15, false, new Color(0.85f, 0.9f, 0.96f));

        var healBtn  = MakeButton(card, "Hồi máu",   new Vector2(0, 44),  new Color(0.12f, 0.45f, 0.25f));
        var saveBtn  = MakeButton(card, "Lưu game",  new Vector2(0, -6),  new Color(0.12f, 0.35f, 0.6f));
        var swapBtn  = MakeButton(card, "Đổi vị trí", new Vector2(0, -56), new Color(0.3f, 0.3f, 0.4f));
        var closeBtn = MakeButton(card, "Đóng",      new Vector2(0, -118), new Color(0.5f, 0.15f, 0.15f));

        // Gán field (BuildRuntime ở trong class nên truy cập được private)
        ui.panel = overlay.gameObject;
        ui.statusText = status;
        ui.healButton = healBtn;
        ui.saveButton = saveBtn;
        ui.swapButton = swapBtn;
        ui.closeButton = closeBtn;

        // Wire nút (Awake đã chạy với field null nên phải nối ở đây)
        healBtn.onClick.AddListener(ui.OnHeal);
        saveBtn.onClick.AddListener(ui.OnSave);
        swapBtn.onClick.AddListener(ui.OnSwapOrder);
        closeBtn.onClick.AddListener(ui.OnClose);

        overlay.gameObject.SetActive(false);
        Debug.Log("[SavePointUI] Đã tạo SavePointUI runtime.");
        return ui;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static TMPro.TMP_Text MakeLabel(Transform parent, string text, Vector2 pos, int size, bool bold, Color color)
    {
        var rt = NewRect("Lbl", parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(380, 40);
        var t = rt.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
        return t;
    }

    static Button MakeButton(Transform parent, string label, Vector2 pos, Color bg)
    {
        var rt = NewRect("Btn", parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(300, 44);
        rt.gameObject.AddComponent<Image>().color = bg;
        var btn = rt.gameObject.AddComponent<Button>();

        var trt = NewRect("Lbl", rt);
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var t = trt.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        t.text = label; t.fontSize = 18; t.color = Color.white;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.fontStyle = TMPro.FontStyles.Bold;
        return btn;
    }
}
