using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class StartMenuUI : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject saveSlotsPanel;

    [Header("Main Menu Buttons")]
    public Button startGameButton;
    public Button quitButton;

    [Header("Save Slots Settings")]
    public Button closeSlotsPanelButton;
    public Transform slotsContainer;
    public GameObject slotButtonPrefab;

    [Header("Transfer Code (tự tạo nếu để trống)")]
    public Button transferCodeButton;
    public GameObject transferCodePanel;
    public TMP_Text currentIdLabel;
    public TMP_InputField transferCodeInput;
    public Button confirmTransferButton;
    public Button closeTransferButton;
    public Button copyIdButton;
    public TMP_Text transferStatusText;

    [Header("First Visit Welcome (tự tạo nếu để trống)")]
    public GameObject welcomePanel;
    public TMP_Text welcomeIdLabel;
    public Button closeWelcomeButton;
    public Button copyWelcomeIdButton;

    void Awake()
    {
        // Tự tạo UI nếu chưa gán trong Inspector
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) BuildMissingUI(canvas.transform);
    }

    void Start()
    {
        // Đảm bảo Input System và TimeScale sạch khi vào menu
        // (có thể bị kẹt nếu BackToMainMenu từ Pause trong gameplay)
        Time.timeScale = 1f;
        InputController.Instance?.SetMode(InputMode.Map);

        // Gắn sự kiện nút cơ bản
        if (startGameButton != null)  startGameButton.onClick.AddListener(OnStartGameClicked);
        if (quitButton != null)       quitButton.onClick.AddListener(OnQuitClicked);
        if (closeSlotsPanelButton != null) closeSlotsPanelButton.onClick.AddListener(OnCloseSlotsPanel);

        // Transfer Code
        if (transferCodeButton != null)    transferCodeButton.onClick.AddListener(OnOpenTransferPanel);
        if (confirmTransferButton != null) confirmTransferButton.onClick.AddListener(OnConfirmTransfer);
        if (closeTransferButton != null)   closeTransferButton.onClick.AddListener(OnCloseTransferPanel);
        if (copyIdButton != null)          copyIdButton.onClick.AddListener(CopyIdToClipboard);

        // Welcome
        if (closeWelcomeButton != null)  closeWelcomeButton.onClick.AddListener(OnCloseWelcome);
        if (copyWelcomeIdButton != null) copyWelcomeIdButton.onClick.AddListener(CopyIdToClipboard);

        // Trạng thái ban đầu
        mainMenuPanel.SetActive(true);
        if (saveSlotsPanel != null)    saveSlotsPanel.SetActive(false);
        if (transferCodePanel != null) transferCodePanel.SetActive(false);

        // Tắt cảnh báo trình duyệt khi ở menu chính
        GameManager.Instance?.DeactivateBrowserWarning();

        // Lần đầu vào game → hiện Welcome
        ShowWelcomeIfFirstVisit();
    }

    // =====================================================================
    //  WELCOME PANEL
    // =====================================================================

    void ShowWelcomeIfFirstVisit()
    {
        if (GameManager.Instance == null) return;

        bool isFirstVisit = !PlayerPrefs.HasKey("DevicePlayerId");
        string playerId = GameManager.Instance.GetPlayerId();

        if (isFirstVisit && welcomePanel != null)
        {
            welcomePanel.SetActive(true);
            if (welcomeIdLabel != null) welcomeIdLabel.text = playerId;
        }
        else if (welcomePanel != null)
        {
            welcomePanel.SetActive(false);
        }
    }

    void OnCloseWelcome()
    {
        if (welcomePanel != null) welcomePanel.SetActive(false);
    }

    // =====================================================================
    //  TRANSFER CODE
    // =====================================================================

    void OnOpenTransferPanel()
    {
        if (transferCodePanel == null) return;

        SFXManager.Instance?.PlayPanelOpen();
        transferCodePanel.SetActive(true);

        if (currentIdLabel != null && GameManager.Instance != null)
            currentIdLabel.text = GameManager.Instance.GetPlayerId();

        if (transferCodeInput != null) transferCodeInput.text = "";
        if (transferStatusText != null) transferStatusText.text = "";
    }

    void OnConfirmTransfer()
    {
        if (GameManager.Instance == null || transferCodeInput == null) return;

        // Chuẩn hoá giống server (trim + lowercase) để khớp prefix _id trên DB
        string code = (transferCodeInput.text ?? "").Trim().ToLower();

        // 1. Kiểm tra định dạng trước (rẻ, không cần mạng)
        if (!code.StartsWith("guest_"))
        {
            if (transferStatusText != null)
                transferStatusText.text = "<color=#ff4444>✗ Sai định dạng! Mã phải là: guest_xxxxxxxx</color>";
            return;
        }

        // 2. Hỏi server xem mã có save thật không, rồi mới áp dụng
        if (confirmTransferButton != null) confirmTransferButton.interactable = false;
        if (transferStatusText != null)
            transferStatusText.text = "<color=#dddddd>Đang kiểm tra mã...</color>";

        StartCoroutine(CheckAndApplyTransferCode(code));
    }

    [System.Serializable]
    class TransferSlot { public int slotId; public string saveTime; }

    [System.Serializable]
    class TransferCheckResult { public bool exists; public TransferSlot[] slots; }

    // Slot có trên SERVER theo mã vừa nhập — để hiện ở màn chọn slot trên máy mới.
    private TransferSlot[] _transferSlots;

    TransferSlot FindServerSlot(int slotIndex)
    {
        if (_transferSlots == null || GameManager.Instance == null
            || !GameManager.Instance.HasPendingTransferLoad) return null;
        foreach (var s in _transferSlots)
            if (s != null && s.slotId == slotIndex) return s;
        return null;
    }

    IEnumerator CheckAndApplyTransferCode(string code)
    {
        string url = GameManager.Instance.backendBaseURL + "/player/check/" + UnityWebRequest.EscapeURL(code);
        UnityWebRequest req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Bypass-Tunnel-Reminder", "true"); // Bypass localtunnel warning page
        yield return req.SendWebRequest();

        if (confirmTransferButton != null) confirmTransferButton.interactable = true;

        // Không chạm được server → cho qua kèm cảnh báo (game vẫn chơi offline được)
        if (req.result != UnityWebRequest.Result.Success)
        {
            bool ok = GameManager.Instance.SetPlayerId(code);
            if (transferStatusText != null)
                transferStatusText.text = ok
                    ? "<color=#ffcc44>⚠ Không kết nối được máy chủ để kiểm tra. Đã áp dụng mã — ấn Start Game để thử tải.</color>"
                    : "<color=#ff4444>✗ Mã không hợp lệ.</color>";
            yield break;
        }

        TransferCheckResult res = null;
        try { res = JsonUtility.FromJson<TransferCheckResult>(req.downloadHandler.text); }
        catch { res = null; }

        if (res == null || !res.exists)
        {
            if (transferStatusText != null)
                transferStatusText.text = "<color=#ff4444>✗ Không tìm thấy dữ liệu cho mã này trên máy chủ.</color>";
            yield break;
        }

        // Mã hợp lệ và có save → áp dụng
        GameManager.Instance.SetPlayerId(code);
        _transferSlots = res.slots; // lưu để màn chọn slot hiện save từ server
        if (currentIdLabel != null) currentIdLabel.text = GameManager.Instance.GetPlayerId();

        // Nếu màn chọn slot đang mở → refresh để hiện slot server ngay
        if (saveSlotsPanel != null && saveSlotsPanel.activeSelf) PopulateSaveSlots();

        string slotList = "";
        if (res.slots != null && res.slots.Length > 0)
        {
            for (int i = 0; i < res.slots.Length; i++)
                slotList += (i > 0 ? ", " : "") + (res.slots[i].slotId + 1);
        }

        if (transferStatusText != null)
            transferStatusText.text = string.IsNullOrEmpty(slotList)
                ? "<color=#00ff88>✓ Thành công! Ấn Start Game để tải dữ liệu.</color>"
                : $"<color=#00ff88>✓ Tìm thấy save ở slot: {slotList}.</color>\n<color=#dddddd>Ấn Start Game và chọn đúng slot để tải.</color>";
    }

    void OnCloseTransferPanel()
    {
        SFXManager.Instance?.PlayPanelClose();

        if (transferCodePanel != null) transferCodePanel.SetActive(false);
        if (saveSlotsPanel != null && saveSlotsPanel.activeSelf) PopulateSaveSlots();
    }

    void CopyIdToClipboard()
    {
        if (GameManager.Instance == null) return;
        string id = GameManager.Instance.GetPlayerId();
        GUIUtility.systemCopyBuffer = id;

        if (transferStatusText != null)
            transferStatusText.text = "<color=#00ff88>✓ Đã sao chép!</color>";

        Debug.Log("[StartMenu] Copied Guest ID: " + id);
    }

    // =====================================================================
    //  MENU CHÍNH + SAVE SLOTS
    // =====================================================================

    void OnStartGameClicked()
    {
        SFXManager.Instance?.PlayButtonClick();

        mainMenuPanel.SetActive(false);
        saveSlotsPanel.SetActive(true);

        SFXManager.Instance?.PlayPanelOpen();

        Transform titleObj = transform.Find("Title");
        if (titleObj != null) titleObj.gameObject.SetActive(false);

        PopulateSaveSlots();
    }

    void OnCloseSlotsPanel()
    {
        SFXManager.Instance?.PlayPanelClose();

        mainMenuPanel.SetActive(true);
        saveSlotsPanel.SetActive(false);

        Transform titleObj = transform.Find("Title");
        if (titleObj != null) titleObj.gameObject.SetActive(true);
    }

    void OnQuitClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void PopulateSaveSlots()
    {
        if (GameManager.Instance == null) return;
        PlayerSave[] slots = GameManager.Instance.GetAllSaveSlotsMetadata();

        foreach (Transform child in slotsContainer) Destroy(child.gameObject);

        for (int i = 0; i < 4; i++)
        {
            GameObject btnObj = Instantiate(slotButtonPrefab, slotsContainer);
            btnObj.SetActive(true);
            Button btn = btnObj.GetComponent<Button>();
            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();

            int slotIndex = i;
            var saveData = slots[i];
            var serverSlot = FindServerSlot(slotIndex); // có save trên server (sau khi nhập mã)?

            if (saveData != null)
            {
                string time = string.IsNullOrEmpty(saveData.saveTime) ? "Không có mốc thời gian" : saveData.saveTime;
                string location = string.IsNullOrEmpty(saveData.lastSavePointId) ? "Khu vực khởi đầu" : saveData.lastSavePointId;
                string info = "Lv 1";
                if (saveData.party != null && saveData.party.Count > 0)
                    info = $"{saveData.party[0].entityName} (Lv {saveData.party[0].level})";

                txt.text = $"<size=120%><b>SLOT {slotIndex + 1}</b></size>\n<color=#dddddd>{location} - {info}</color>\n<size=80%><color=#aaaaaa>{time}</color></size>";
                btn.onClick.AddListener(() => OnSlotSelected(slotIndex, false));

                Button deleteBtn = null;
                foreach (Button b in btnObj.GetComponentsInChildren<Button>(true))
                    if (b.gameObject.name == "DeleteButton") { deleteBtn = b; break; }

                if (deleteBtn != null)
                {
                    deleteBtn.gameObject.SetActive(true);
                    deleteBtn.onClick.RemoveAllListeners();
                    deleteBtn.onClick.AddListener(() => OnDeleteSlotClicked(slotIndex));
                }
            }
            else if (serverSlot != null)
            {
                // Slot trống ở máy này nhưng CÓ trên server (vừa nhập mã chuyển máy) → cho tải.
                string time = string.IsNullOrEmpty(serverSlot.saveTime) ? "" : serverSlot.saveTime;
                txt.text = $"<size=120%><b>SLOT {slotIndex + 1}</b></size>\n<color=#7fdfff>Save trên máy chủ — bấm để tải</color>\n<size=80%><color=#aaaaaa>{time}</color></size>";
                btn.onClick.AddListener(() => OnSlotSelected(slotIndex, false)); // false → Load (server)

                foreach (Button b in btnObj.GetComponentsInChildren<Button>(true))
                    if (b.gameObject.name == "DeleteButton") { b.gameObject.SetActive(false); break; }
            }
            else
            {
                txt.text = $"<size=120%><b>SLOT {slotIndex + 1}</b></size>\n<color=#bbbbbb>Bắt đầu hành trình mới</color>";
                btn.onClick.AddListener(() => OnSlotSelected(slotIndex, true));

                foreach (Button b in btnObj.GetComponentsInChildren<Button>(true))
                    if (b.gameObject.name == "DeleteButton") { b.gameObject.SetActive(false); break; }
            }
        }
    }

    void OnSlotSelected(int slotIndex, bool isNewGame)
    {
        GameManager.Instance.currentSaveSlot = slotIndex;

        // Vừa nhập mã chuyển máy: slot có thể TRỐNG ở local máy này, nhưng save nằm trên
        // server → BUỘC đi nhánh Load (server) thay vì New Game.
        if (GameManager.Instance.HasPendingTransferLoad)
            isNewGame = false;

        Debug.Log($"[StartMenu] OnSlotSelected: slot={slotIndex} isNewGame={isNewGame}");

        if (isNewGame)
        {
            Debug.Log($"[StartMenu] → PrepareNewGame (slot trống)");
            GameManager.Instance.PrepareNewGame();
        }
        else
        {
            Debug.Log($"[StartMenu] → LoadGame (slot có data)");
        }

        GameManager.Instance.LoadAndStartGame();
    }

    void OnDeleteSlotClicked(int slotIndex)
    {
        GameManager.Instance.DeleteSaveSlot(slotIndex);
        PopulateSaveSlots();
    }

    // =====================================================================
    //  AUTO-BUILD UI (tự tạo nếu chưa gán trong Inspector)
    // =====================================================================

    void BuildMissingUI(Transform canvasRoot)
    {
        // Nút Transfer Code ở menu chính
        if (transferCodeButton == null && mainMenuPanel != null)
        {
            transferCodeButton = MakeButton(mainMenuPanel.transform, "TransferCodeBtn",
                "⚙ Mã chuyển máy", new Vector2(360f, 60f), new Color(0.2f, 0.2f, 0.3f, 0.9f));

            var rt = transferCodeButton.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 30f);
        }

        // Transfer Code Panel
        if (transferCodePanel == null)
            BuildTransferPanel(canvasRoot);

        // Welcome Panel
        if (welcomePanel == null)
            BuildWelcomePanel(canvasRoot);
    }

    void BuildTransferPanel(Transform root)
    {
        transferCodePanel = MakeOverlay(root, "TransferCodePanel");
        transferCodePanel.SetActive(false);

        GameObject card = MakeCard(transferCodePanel.transform, 440f, 380f);

        MakeLabel(card.transform, "MÃ CHUYỂN MÁY",
            new Vector2(0, 140f), 24, true, Color.white);

        MakeLabel(card.transform, "Dùng mã bên dưới để chơi trên thiết bị khác",
            new Vector2(0, 108f), 14, false, new Color(0.75f, 0.75f, 0.75f));

        MakeLabel(card.transform, "Mã của bạn:",
            new Vector2(-80f, 70f), 16, false, new Color(0.7f, 0.85f, 1f));

        currentIdLabel = MakeLabel(card.transform, "guest_xxxxxxxx",
            new Vector2(0f, 40f), 22, true, new Color(0.3f, 1f, 0.6f));

        copyIdButton = MakeButton(card.transform, "CopyBtn", "📋 Sao chép mã",
            new Vector2(200f, 38f), new Color(0.15f, 0.4f, 0.6f));
        copyIdButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);

        MakeLabel(card.transform, "──────── hoặc ────────",
            new Vector2(0, -35f), 12, false, new Color(0.5f, 0.5f, 0.5f));

        MakeLabel(card.transform, "Nhập mã từ máy khác:",
            new Vector2(0, -65f), 14, false, new Color(0.8f, 0.8f, 0.8f));

        transferCodeInput = MakeInputField(card.transform, "guest_xxxxxxxx",
            new Vector2(0, -100f), new Vector2(340f, 42f));

        confirmTransferButton = MakeButton(card.transform, "ConfirmBtn", "✓ Xác nhận",
            new Vector2(200f, 42f), new Color(0.1f, 0.5f, 0.2f));
        confirmTransferButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -148f);

        transferStatusText = MakeLabel(card.transform, "",
            new Vector2(0, -190f), 13, false, Color.white);

        closeTransferButton = MakeButton(card.transform, "CloseBtn", "✕",
            new Vector2(40f, 40f), new Color(0.6f, 0.1f, 0.1f));
        closeTransferButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(195f, 165f);
    }

    void BuildWelcomePanel(Transform root)
    {
        welcomePanel = MakeOverlay(root, "WelcomePanel");
        welcomePanel.SetActive(false);

        GameObject card = MakeCard(welcomePanel.transform, 480f, 320f);

        MakeLabel(card.transform, "🎮 CHÀO MỪNG ĐẾN VỚI GAME!",
            new Vector2(0, 110f), 22, true, Color.white);

        MakeLabel(card.transform,
            "Hệ thống đã tạo cho bạn một mã định danh.\nHãy ghi nhớ hoặc sao chép mã này\nđể chơi trên thiết bị khác!",
            new Vector2(0, 55f), 15, false, new Color(0.8f, 0.85f, 0.9f));

        MakeLabel(card.transform, "Mã của bạn:",
            new Vector2(0, 5f), 16, false, new Color(0.7f, 0.85f, 1f));

        welcomeIdLabel = MakeLabel(card.transform, "guest_xxxxxxxx",
            new Vector2(0, -30f), 28, true, new Color(0.3f, 1f, 0.6f));

        copyWelcomeIdButton = MakeButton(card.transform, "CopyWelcBtn", "📋 Sao chép mã",
            new Vector2(220f, 42f), new Color(0.15f, 0.4f, 0.6f));
        copyWelcomeIdButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -75f);

        closeWelcomeButton = MakeButton(card.transform, "CloseWelcBtn", "Đã hiểu, vào game!",
            new Vector2(260f, 44f), new Color(0.12f, 0.12f, 0.2f));
        closeWelcomeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -125f);
    }

    // =====================================================================
    //  UI FACTORY (tạo các element cơ bản)
    // =====================================================================

    GameObject MakeOverlay(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        return go;
    }

    GameObject MakeCard(Transform parent, float w, float h)
    {
        var go = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.15f, 0.97f);
        return go;
    }

    TMP_Text MakeLabel(Transform parent, string text, Vector2 pos, int size, bool bold, Color color)
    {
        var go = new GameObject("Lbl", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(420f, 60f);

        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size;
        t.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
        t.color = color; t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        return t;
    }

    Button MakeButton(Transform parent, string name, string label, Vector2 size, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = bg;

        var txtGo = new GameObject("Lbl", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(4f, 2f); txtRt.offsetMax = new Vector2(-4f, -2f);
        var tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 16; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = TMPro.FontStyles.Bold;

        return go.GetComponent<Button>();
    }

    TMP_InputField MakeInputField(Transform parent, string placeholder, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f);

        // Text Area
        var area = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        area.transform.SetParent(go.transform, false);
        var aRt = area.GetComponent<RectTransform>();
        aRt.anchorMin = Vector2.zero; aRt.anchorMax = Vector2.one;
        aRt.offsetMin = new Vector2(10f, 4f); aRt.offsetMax = new Vector2(-10f, -4f);

        // Placeholder
        var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGo.transform.SetParent(area.transform, false);
        var phRt = phGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = phRt.offsetMax = Vector2.zero;
        var ph = phGo.GetComponent<TextMeshProUGUI>();
        ph.text = placeholder; ph.fontSize = 16;
        ph.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        ph.fontStyle = TMPro.FontStyles.Italic;
        ph.alignment = TextAlignmentOptions.MidlineLeft;

        // Text
        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(area.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
        var txt = txtGo.GetComponent<TextMeshProUGUI>();
        txt.text = ""; txt.fontSize = 16; txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.MidlineLeft;

        // Wire
        var input = go.GetComponent<TMP_InputField>();
        input.textViewport = aRt;
        input.textComponent = txt;
        input.placeholder = ph;
        input.fontAsset = txt.font;
        input.pointSize = 16;
        input.characterLimit = 30;

        return input;
    }
}
