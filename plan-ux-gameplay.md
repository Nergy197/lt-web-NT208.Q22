# Plan: UX / Gameplay Issues

> Style tham khảo: **Octopath Traveler** — dark background, list dọc, cursor ▶, viền ornate.

---

## Tổng quan

| # | Vấn đề | Ưu tiên | Ước tính |
|---|--------|---------|----------|
| UX-1 | Pause Menu (ESC in-game) | **Cao** | 0.5 ngày |
| UX-2 | Settings Hub + Audio + Graphics | **Cao** | 1–2 ngày |
| UX-3 | Feedback khi save thất bại | Trung bình | 2 giờ |
| UX-4 | Transfer Code validate trên server | Trung bình | 3 giờ |
| UX-5 | Encounter rate scale theo số bước | Thấp | 1 giờ |
| **MOB-1** | **Safe Area / Notch — UI bị che trên điện thoại** | **Cao** | 2 giờ |
| **MOB-2** | **Dynamic Joystick (floating)** | **Cao** | 3 giờ |
| **MOB-3** | **Nút Pause trên mobile + panel Pause** | **Cao** | 1 giờ |
| **MOB-4** | **Android Back Button** | Trung bình | 1 giờ |
| **MOB-5** | **Touch target size + Haptic feedback** | Trung bình | 2 giờ |
| **MOB-6** | **Lock Screen Orientation (Landscape)** | Trung bình | 30 phút |

---

## UX-1: Pause Menu

### Vấn đề
ESC không làm gì trong MapScene và BattleScene. Người chơi không có cách tạm dừng, truy cập settings, hoặc thoát an toàn trong lúc chơi.

### Phạm vi thay đổi
- `Assets/Scripts/System/InputController.cs` — thêm `InputMode.Pause` và bind ESC global
- `Assets/Data/Input/GameInput.inputactions` — thêm action `Pause` vào action map chung
- `Assets/Scripts/UI/PauseMenuUI.cs` — **tạo mới**
- Scene `MapScene` và `BattleScene` — thêm PauseMenu Canvas + PauseMenuUI component

### Thiết kế PauseMenu

```
┌─────────────────────────┐
│         PAUSE           │
├─────────────────────────┤
│  ▶  Tiếp tục            │
│     Cài đặt             │
│     Lưu game            │
│     Về Main Menu        │
└─────────────────────────┘
```

### Hướng implement

**Bước 1 — Thêm InputMode.Pause vào enum:**
```csharp
// InputController.cs
public enum InputMode { Map, Battle, BattleSkillMenu, UI, Cutscene, Pause }
```

**Bước 2 — Bind ESC global (không thuộc action map riêng):**
```csharp
// InputController.cs — trong Awake()
Input.Map.Pause.performed    += _ => PauseMenuUI.Instance?.Toggle();
Input.Battle.Pause.performed += _ => PauseMenuUI.Instance?.Toggle();
// Thêm action Pause vào cả Map và Battle action map trong .inputactions
```

**Bước 3 — PauseMenuUI.cs (tạo mới):**
```csharp
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance;
    [SerializeField] GameObject panel;
    [SerializeField] Button btnResume, btnSettings, btnSave, btnMainMenu;

    bool _isPaused;

    void Awake() { Instance = this; panel.SetActive(false); }

    public void Toggle() { if (_isPaused) Resume(); else Pause(); }

    void Pause()
    {
        _isPaused = true;
        panel.SetActive(true);
        Time.timeScale = 0f;                          // dừng game logic
        InputController.Instance?.SetMode(InputMode.Pause);
    }

    public void Resume()
    {
        _isPaused = false;
        panel.SetActive(false);
        Time.timeScale = 1f;
        InputController.Instance?.SetMode(InputMode.Map); // hoặc Battle nếu đang battle
    }

    void Start()
    {
        btnResume.onClick.AddListener(Resume);
        btnSettings.onClick.AddListener(() => SettingsHubUI.Instance?.Open(Resume));
        btnSave.onClick.AddListener(OnSave);
        btnMainMenu.onClick.AddListener(OnMainMenu);
    }

    void OnSave()
    {
        // Lưu tại điểm hiện tại (không cần SavePoint)
        GameManager.Instance?.QuickSaveAndSync();
        // Hiện thông báo "Đã lưu" (xem UX-3)
    }

    void OnMainMenu()
    {
        // Confirm dialog trước khi thoát
        ConfirmDialog.Show("Về Main Menu? Tiến trình chưa lưu sẽ mất.",
            onConfirm: () => {
                Time.timeScale = 1f;
                SceneManager.LoadScene("StartScene");
            });
    }
}
```

**Bước 4 — Xử lý BattleScene:**
- Trong BattleScene, `Time.timeScale = 0f` dừng animation và coroutine
- Khi Resume, restore đúng `InputMode.Battle` thay vì `InputMode.Map`
- Cần lưu `InputMode` trước khi pause để restore đúng:
```csharp
private InputMode _modeBeforePause;

void Pause()
{
    _modeBeforePause = InputController.Instance?.Mode ?? InputMode.Map;
    // ...
}

public void Resume()
{
    InputController.Instance?.SetMode(_modeBeforePause);
    // ...
}
```

### Lưu ý
- **Không** pause trong Cutscene — check mode trước khi cho phép ESC
- Đảm bảo `Time.timeScale` luôn được reset khi load scene mới (tránh game bị freeze)

---

## UX-2: Settings Hub + Audio + Graphics

### Vấn đề
Không có menu cài đặt nào trong game. Người chơi không thể điều chỉnh âm lượng hay chất lượng đồ họa.

### Phạm vi thay đổi
- `Assets/Audio/MainMixer.mixer` — **tạo mới** AudioMixer asset
- `Assets/Scripts/UI/SettingsHubUI.cs` — **tạo mới**
- `Assets/Scripts/UI/AudioSettingsUI.cs` — **tạo mới**
- `Assets/Scripts/UI/GraphicsSettingsUI.cs` — **tạo mới**
- `Assets/Scripts/UI/StartMenuUI.cs` — thêm nút "Cài đặt"
- `Assets/Scripts/UI/PauseMenuUI.cs` — nút "Cài đặt" gọi SettingsHub

### 2a. AudioMixer Setup

**Trước tiên tạo AudioMixer trong Unity:**
1. `Assets → Create → Audio → Audio Mixer` → đặt tên `MainMixer`
2. Tạo 3 groups: `Master`, `Music`, `SFX`
3. Expose parameters: `MasterVolume`, `MusicVolume`, `SFXVolume`
4. Gán `MainMixer` vào tất cả AudioSource trong game

**AudioSettingsUI.cs:**
```csharp
public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] Slider masterSlider, musicSlider, sfxSlider;
    [SerializeField] AudioMixer mixer;

    const string KEY_MASTER = "Vol_Master";
    const string KEY_MUSIC  = "Vol_Music";
    const string KEY_SFX    = "Vol_SFX";

    void OnEnable()
    {
        // Load saved values (default 0.75)
        masterSlider.value = PlayerPrefs.GetFloat(KEY_MASTER, 0.75f);
        musicSlider.value  = PlayerPrefs.GetFloat(KEY_MUSIC,  0.75f);
        sfxSlider.value    = PlayerPrefs.GetFloat(KEY_SFX,    0.75f);

        masterSlider.onValueChanged.AddListener(v => SetVolume("MasterVolume", KEY_MASTER, v));
        musicSlider.onValueChanged.AddListener(v  => SetVolume("MusicVolume",  KEY_MUSIC,  v));
        sfxSlider.onValueChanged.AddListener(v    => SetVolume("SFXVolume",    KEY_SFX,    v));

        ApplyAllVolumes();
    }

    void SetVolume(string param, string key, float value)
    {
        // AudioMixer dùng dB: 0.0001 → -80dB, 1.0 → 0dB
        mixer.SetFloat(param, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat(key, value);
    }

    void ApplyAllVolumes()
    {
        SetVolume("MasterVolume", KEY_MASTER, masterSlider.value);
        SetVolume("MusicVolume",  KEY_MUSIC,  musicSlider.value);
        SetVolume("SFXVolume",    KEY_SFX,    sfxSlider.value);
    }
}
```

**Cần gọi `ApplyAllVolumes()` khi game khởi động** — thêm vào `GameManager.Awake()`:
```csharp
// Khôi phục volume đã lưu
AudioSettingsUI.ApplyVolumesFromPrefs(mixer);
```

### 2b. GraphicsSettingsUI.cs

```csharp
public class GraphicsSettingsUI : MonoBehaviour
{
    [SerializeField] Dropdown qualityDropdown;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] Toggle vsyncToggle;
    [SerializeField] Slider brightnessSlider;   // Dùng nếu có Post Processing

    void OnEnable()
    {
        // Quality
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();

        // Fullscreen & VSync
        fullscreenToggle.isOn = Screen.fullScreen;
        vsyncToggle.isOn = QualitySettings.vSyncCount > 0;

        qualityDropdown.onValueChanged.AddListener(i => QualitySettings.SetQualityLevel(i, true));
        fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
        vsyncToggle.onValueChanged.AddListener(v => QualitySettings.vSyncCount = v ? 1 : 0);
    }
}
```

> **Lưu ý WebGL:** `Screen.fullScreen` trên WebGL cần `Screen.SetResolution()` và phụ thuộc browser API. Test kỹ trên Chrome/Firefox.

### 2c. SettingsHubUI.cs

```
┌──────────────────────────────┐
│          CÀI ĐẶT             │
├──────────────────────────────┤
│  [Tab: Âm thanh] [Đồ họa]   │
├──────────────────────────────┤
│                              │
│  < nội dung tab hiện tại >   │
│                              │
├──────────────────────────────┤
│           [Quay lại]         │
└──────────────────────────────┘
```

```csharp
public class SettingsHubUI : MonoBehaviour
{
    public static SettingsHubUI Instance;
    [SerializeField] GameObject panel;
    [SerializeField] GameObject audioPanel, graphicsPanel;
    [SerializeField] Button tabAudio, tabGraphics, btnBack;

    Action _onClose;

    void Awake() { Instance = this; panel.SetActive(false); }

    public void Open(Action onClose = null)
    {
        _onClose = onClose;
        panel.SetActive(true);
        ShowTab(audioPanel);   // mặc định tab Âm thanh
    }

    public void Close()
    {
        panel.SetActive(false);
        PlayerPrefs.Save();    // lưu settings
        _onClose?.Invoke();
    }

    void ShowTab(GameObject target)
    {
        audioPanel.SetActive(target == audioPanel);
        graphicsPanel.SetActive(target == graphicsPanel);
    }

    void Start()
    {
        tabAudio.onClick.AddListener(() => ShowTab(audioPanel));
        tabGraphics.onClick.AddListener(() => ShowTab(graphicsPanel));
        btnBack.onClick.AddListener(Close);
    }
}
```

### 2d. Tích hợp vào StartMenuUI

```csharp
// StartMenuUI.cs — thêm:
[Header("Settings")]
public Button settingsButton;

void Start()
{
    // ...existing code...
    settingsButton?.onClick.AddListener(() => SettingsHubUI.Instance?.Open());
}
```

---

## UX-3: Feedback khi Save thất bại

### Vấn đề
`PlayerPrefs.Save()` và `POST /player/save` có thể thất bại silently. Người chơi bấm "Lưu" thấy "Đã lưu thành công!" mà thực tế có thể không lưu được.

### Hướng implement

**Tạo SaveNotificationUI.cs (hoặc dùng SavePointUI.SetStatus đang có):**

```csharp
// Trong GameManager.cs — refactor SaveRoutine để trả về kết quả
IEnumerator SaveRoutine(string pointId, string sceneName)
{
    // 1. Local save
    bool localOk = TrySaveToLocal(pointId, sceneName);
    if (!localOk)
    {
        NotifySaveResult(success: false, "Lỗi: Không lưu được vào bộ nhớ trình duyệt!");
        yield break;
    }

    // 2. Server backup (async, không block)
    bool serverOk = false;
    yield return StartCoroutine(SaveToServer(result => serverOk = result));

    if (serverOk)
        NotifySaveResult(success: true, "Đã lưu thành công!");
    else
        NotifySaveResult(success: true, "Đã lưu cục bộ (server không phản hồi — sẽ đồng bộ sau).");
}

void NotifySaveResult(bool success, string message)
{
    // Hiện trong SavePointUI nếu đang mở
    SavePointUI.Instance?.SetStatus(message);
    // Hiện toast notification toàn cục nếu SavePointUI đóng
    ToastUI.Instance?.Show(message, success ? Color.green : Color.red);
    Debug.Log($"[SAVE] {message}");
}
```

**Tạo ToastUI.cs đơn giản (hiện thông báo nổi 2 giây):**
```csharp
public class ToastUI : MonoBehaviour
{
    public static ToastUI Instance;
    [SerializeField] TMP_Text label;
    [SerializeField] CanvasGroup group;

    public void Show(string msg, Color color, float duration = 2f)
    {
        label.text = msg;
        label.color = color;
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(duration));
    }

    IEnumerator FadeRoutine(float duration)
    {
        group.alpha = 1f;
        yield return new WaitForSeconds(duration - 0.5f);
        float t = 0;
        while (t < 0.5f) { group.alpha = 1f - t / 0.5f; t += Time.deltaTime; yield return null; }
        group.alpha = 0f;
    }
}
```

---

## UX-4: Transfer Code — Validate trên Server trước khi áp dụng

### Vấn đề
Khi người chơi nhập Transfer Code sai (ID không tồn tại trên server), game vẫn ghi đè `DevicePlayerId` và xóa mất save cũ. Không có cảnh báo nào.

### File liên quan
- `Assets/Scripts/UI/StartMenuUI.cs` — `OnConfirmTransfer()`
- `Backend/server.js` — cần endpoint check

### Hướng implement

**Backend — thêm endpoint kiểm tra ID:**
```js
// server.js
app.get("/player/exists/:id", async (req, res) => {
    try {
        const count = await db.collection("players")
            .countDocuments({ _id: { $regex: `^${req.params.id}_slot_` } });
        res.json({ exists: count > 0, slotCount: count });
    } catch (err) {
        res.status(500).json({ exists: false });
    }
});
```

**Unity — StartMenuUI.OnConfirmTransfer() — thêm server check:**
```csharp
void OnConfirmTransfer()
{
    string code = transferCodeInput.text.Trim().ToLower();

    if (!GameManager.Instance.IsValidTransferCodeFormat(code))
    {
        transferStatusText.text = "Mã không hợp lệ (phải bắt đầu bằng 'guest_').";
        return;
    }

    transferStatusText.text = "Đang kiểm tra...";
    StartCoroutine(ValidateAndApplyTransferCode(code));
}

IEnumerator ValidateAndApplyTransferCode(string code)
{
    using var req = UnityWebRequest.Get(GameManager.Instance.FullURL($"/player/exists/{code}"));
    yield return req.SendWebRequest();

    if (req.result != UnityWebRequest.Result.Success)
    {
        transferStatusText.text = "Không thể kết nối server. Thử lại sau.";
        yield break;
    }

    var data = JsonUtility.FromJson<ExistsResponse>(req.downloadHandler.text);
    if (!data.exists)
    {
        transferStatusText.text = "Không tìm thấy tài khoản với mã này.";
        yield break;
    }

    // Chỉ áp dụng sau khi xác nhận tồn tại
    bool applied = GameManager.Instance.SetPlayerId(code);
    transferStatusText.text = applied
        ? $"Thành công! Tìm thấy {data.slotCount} slot. Vui lòng khởi động lại game."
        : "Lỗi áp dụng mã.";
}

[System.Serializable]
struct ExistsResponse { public bool exists; public int slotCount; }
```

---

## UX-5: Encounter Rate Scale theo Số Bước

### Vấn đề
`stepsSinceLastEncounter` được đếm nhưng không ảnh hưởng gì. Người chơi có thể đi 500 bước không gặp enemy, hoặc gặp ngay sau 5 bước. Không có "soft guarantee".

### File liên quan
- `Assets/Scripts/Map/MapManager.cs` — `CheckForEncounter()`

### Hướng implement

```csharp
// MapManager.cs — thay thế CheckForEncounter()

[Header("Random Encounter")]
[Range(0f, 1f)] public float encounterRate = 0.001f;
[Tooltip("Bước đi tối thiểu trước khi encounter có thể xảy ra")]
public int minStepsBeforeEncounter = 30;
[Tooltip("Số bước sau đó xác suất đạt tối đa (100%)")]
public int maxStepsHardCap = 250;

private int stepsSinceLastEncounter = 0;

public void CheckForEncounter()
{
    if (isInBattle) return;
    if (currentMap == null || currentMap.possibleEnemies.Count == 0) return;

    stepsSinceLastEncounter++;

    // Chưa đủ bước tối thiểu → không encounter
    if (stepsSinceLastEncounter < minStepsBeforeEncounter) return;

    // Hard cap: đảm bảo encounter xảy ra trong maxStepsHardCap bước
    if (stepsSinceLastEncounter >= maxStepsHardCap)
    {
        TriggerRandomEncounter();
        return;
    }

    // Xác suất tăng dần tuyến tính từ encounterRate → 1.0
    float progress = (float)(stepsSinceLastEncounter - minStepsBeforeEncounter)
                   / (maxStepsHardCap - minStepsBeforeEncounter);
    float adjustedRate = Mathf.Lerp(encounterRate, 0.05f, progress);

    if (Random.value < adjustedRate)
        TriggerRandomEncounter();
}

// Reset counter sau mỗi encounter
private void TriggerRandomEncounter()
{
    stepsSinceLastEncounter = 0;  // reset
    // ... existing code ...
}
```

**Giá trị tham chiếu (có thể tune trong Inspector):**
| Tham số | Giá trị gợi ý | Nghĩa |
|---------|--------------|-------|
| `minStepsBeforeEncounter` | 30 | ~6 giây đi liên tục (50Hz) |
| `maxStepsHardCap` | 250 | Không thể đi quá ~50 giây không gặp |
| `encounterRate` | 0.001 | Xác suất cơ bản mỗi bước |

---

---

## MOB-1: Safe Area / Notch Handling

### Vấn đề
`ProjectSettings.asset` có `androidRenderOutsideSafeArea: 1` — UI Canvas render tràn ra ngoài vùng safe area. Trên điện thoại có notch (camera punch-hole, tai thỏ), các nút góc màn hình như joystick, nút attack, nút Pause sẽ bị che khuất hoặc chồng lên camera.

### Hướng implement

**Tạo `SafeAreaFitter.cs` — gắn vào root của mỗi Canvas:**
```csharp
/// <summary>
/// Tự động thu nhỏ RectTransform theo Screen.safeArea.
/// Gắn vào Panel root (con trực tiếp của Canvas) chứa các nút UI.
/// </summary>
[ExecuteAlways]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform _rt;
    Rect _lastSafeArea;

    void Awake() => _rt = GetComponent<RectTransform>();

    void Update()
    {
        if (Screen.safeArea == _lastSafeArea) return;
        Apply();
    }

    void Apply()
    {
        _lastSafeArea = Screen.safeArea;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        // Chuyển safeArea từ pixel → anchor (0–1)
        Vector2 anchorMin = _lastSafeArea.position / screenSize;
        Vector2 anchorMax = (_lastSafeArea.position + _lastSafeArea.size) / screenSize;

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
        _rt.offsetMin = _rt.offsetMax = Vector2.zero;
    }
}
```

**Setup:**
1. Gắn `SafeAreaFitter` vào `MapPanel`, `BattlePanel`, `SkillMenuPanel` trong `MobileInputUI`
2. Gắn vào panel chứa minimap và HUD của MapScene
3. **Không** gắn vào toàn bộ Canvas (sẽ làm background bị hở)

**Đổi Project Setting:**
- `Edit → Project Settings → Player → Android → Resolution and Presentation`
- Tắt **"Render outside safe area"** → `androidRenderOutsideSafeArea: 0`

---

## MOB-2: Dynamic Joystick (Floating)

### Vấn đề
`VirtualJoystick.cs` hiện là joystick **cố định** — người chơi phải chạm đúng vào vị trí joystick ở góc dưới trái. Dynamic joystick (xuất hiện tại điểm chạm, tự ẩn khi nhả tay) thoải mái hơn nhiều, nhất là trên màn hình lớn (tablet).

### File liên quan
- `Assets/Scripts/UI/Mobile/VirtualJoystick.cs` — refactor hoặc tạo `DynamicJoystick.cs` song song
- `Assets/Scripts/UI/Mobile/MobileInputUI.cs` — chuyển sang dùng DynamicJoystick

### Hướng implement

**Tạo `DynamicJoystick.cs`:**
```csharp
/// <summary>
/// Joystick động: xuất hiện tại điểm chạm, tự ẩn khi nhả.
/// Gắn vào một Image trong suốt phủ toàn màn hình (half screen trái).
/// </summary>
public class DynamicJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] RectTransform background;   // vòng tròn ngoài
    [SerializeField] RectTransform handle;       // vòng tròn trong
    [SerializeField] float radius = 80f;
    [SerializeField] [Range(0f, 0.3f)] float deadZone = 0.1f;

    public Vector2 InputVector { get; private set; }
    public event System.Action<Vector2> OnInputChanged;

    Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        background.gameObject.SetActive(false);  // ẩn lúc đầu
    }

    public void OnPointerDown(PointerEventData data)
    {
        // Di chuyển background đến điểm chạm
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, data.position, data.pressEventCamera, out Vector2 localPos);

        background.anchoredPosition = localPos;
        handle.anchoredPosition = Vector2.zero;
        background.gameObject.SetActive(true);

        ProcessDrag(data);
    }

    public void OnDrag(PointerEventData data) => ProcessDrag(data);

    public void OnPointerUp(PointerEventData data)
    {
        background.gameObject.SetActive(false);
        InputVector = Vector2.zero;
        OnInputChanged?.Invoke(Vector2.zero);
    }

    void ProcessDrag(PointerEventData data)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, data.position, data.pressEventCamera, out Vector2 touchPos);

        Vector2 delta = touchPos - background.anchoredPosition;
        Vector2 clamped = Vector2.ClampMagnitude(delta, radius);
        handle.anchoredPosition = clamped;

        Vector2 raw = clamped / radius;
        InputVector = raw.magnitude < deadZone ? Vector2.zero : raw;
        OnInputChanged?.Invoke(InputVector);
    }
}
```

**Cập nhật `MobileInputUI.cs` — thêm field:**
```csharp
[Header("Map – Dynamic Joystick")]
[SerializeField] private DynamicJoystick dynamicJoystick;
[SerializeField] private bool useDynamicJoystick = true;

void Start()
{
    // ...
    if (useDynamicJoystick && dynamicJoystick != null)
        dynamicJoystick.OnInputChanged += v => playerMovement?.SetMobileInput(v);
    else if (joystick != null)
        joystick.OnInputChanged += v => playerMovement?.SetMobileInput(v);
}
```

**Setup trong Unity:**
1. Tạo Image trong suốt (`alpha = 0`) phủ nửa trái màn hình, gắn `DynamicJoystick`
2. Tạo child `Background` (circle sprite 160×160) và `Handle` (circle sprite 80×80)
3. Joystick cũ vẫn giữ làm fallback khi `useDynamicJoystick = false`

---

## MOB-3: Nút Pause trên Mobile + Panel Pause trong MobileInputUI

### Vấn đề
Trên mobile không có phím ESC. Cần nút Pause vật lý trên màn hình. Ngoài ra `MobileInputUI.RefreshPanels()` không xử lý `InputMode.Pause` nên khi vào Pause, joystick vẫn hiển thị đè lên PauseMenu.

### Hướng implement

**Bước 1 — Thêm nút Pause vào MobileInputUI:**
```csharp
[Header("Global – Pause Button")]
[SerializeField] private Button pauseButton;   // Nút ☰ góc trên phải, luôn visible

void BindButtons()
{
    // ... existing ...
    pauseButton?.onClick.AddListener(() => PauseMenuUI.Instance?.Toggle());
}
```

**Bước 2 — Thêm `pausePanel` vào MobileInputUI:**
```csharp
[Header("Panels")]
[SerializeField] private GameObject mapPanel;
[SerializeField] private GameObject battlePanel;
[SerializeField] private GameObject skillMenuPanel;
[SerializeField] private GameObject pausePanel;   // thêm mới — hiện nút Resume, Back to Menu

void RefreshPanels(InputMode mode)
{
    bool isPaused = mode == InputMode.Pause;
    bool isMap    = !isPaused && (mode == InputMode.Map || mode == InputMode.Cutscene);
    bool isBattle = !isPaused && (mode == InputMode.Battle || mode == InputMode.BattleItemMenu);
    bool isSkill  = !isPaused && mode == InputMode.BattleSkillMenu;

    if (mapPanel)       mapPanel.SetActive(isMap);
    if (battlePanel)    battlePanel.SetActive(isBattle);
    if (skillMenuPanel) skillMenuPanel.SetActive(isSkill);
    if (pausePanel)     pausePanel.SetActive(isPaused);

    // Nút Pause luôn ẩn khi đang trong Cutscene hoặc chính pause panel
    if (pauseButton != null)
        pauseButton.gameObject.SetActive(!isPaused && mode != InputMode.Cutscene);
}
```

**Layout nút Pause:**
```
┌─────────────────────────────────┐
│  [☰]                            │  ← góc trên phải, 60×60dp, luôn hiển thị
│                                 │
│                                 │
│  [Joystick]         [Attack]    │
│               [Skill] [Flee]    │
└─────────────────────────────────┘
```

---

## MOB-4: Android Back Button

### Vấn đề
Trên Android, nút Back vật lý (`KeyCode.Escape`) mặc định thoát app ngay. Cần handle: trong gameplay → mở Pause; trong menu → về trang trước; trong Pause → đóng Pause.

### File liên quan
- `Assets/Scripts/System/InputController.cs` hoặc tạo `AndroidBackHandler.cs` riêng

### Hướng implement

**Tạo `AndroidBackHandler.cs` — DontDestroyOnLoad:**
```csharp
public class AndroidBackHandler : MonoBehaviour
{
    public static AndroidBackHandler Instance;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        HandleBack();
#endif
    }

    void HandleBack()
    {
        var ic = InputController.Instance;
        if (ic == null) return;

        switch (ic.Mode)
        {
            // Gameplay → mở Pause
            case InputMode.Map:
            case InputMode.Battle:
                PauseMenuUI.Instance?.Toggle();
                break;

            // Pause đang mở → đóng Pause
            case InputMode.Pause:
                PauseMenuUI.Instance?.Resume();
                break;

            // Menu UI (SavePoint, Teleport) → đóng menu
            case InputMode.UI:
                SavePointUI.Instance?.OnClose();
                TeleportMenuUI.Instance?.Close();
                break;

            // SkillMenu → back về ActionMenu
            case InputMode.BattleSkillMenu:
                BattleManager.Instance?.BackToActionMenu();
                break;

            // Cutscene → không làm gì
            case InputMode.Cutscene:
                break;
        }
    }
}
```

---

## MOB-5: Touch Target Size + Haptic Feedback

### Vấn đề
**Touch Target:** Unity Button mặc định có `targetGraphic` nhỏ. Trên mobile, ngón tay cần vùng chạm tối thiểu 44×44dp (~ 88×88px ở 2x). Các nút battle hiện tại chưa đảm bảo kích thước này.

**Haptic:** Không có phản hồi rung khi tấn công hay nhận sát thương — giảm immersion trên mobile.

### 5a. Touch Target — Quy tắc trong Inspector

| Nút | Kích thước tối thiểu |
|-----|---------------------|
| Joystick Handle | 120×120 px |
| Attack / Skill | 100×100 px |
| Parry | 110×110 px (vì cần reaction nhanh) |
| Flee / Cancel | 90×90 px |
| Pause ☰ | 80×80 px |

**Cách tăng touch target mà không thay đổi visual — dùng `Expand Hit Area`:**
```csharp
/// <summary>
/// Gắn vào Button để mở rộng vùng chạm mà không ảnh hưởng visual.
/// </summary>
public class ExpandHitArea : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Vector2 extraPadding = new Vector2(20, 20);
    RectTransform _rt;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        // Tạo invisible child rect lớn hơn để catch raycast
    }
}
```
> Thực tế hơn: dùng **`Button.targetGraphic`** với một `Image` transparent lớn hơn đặt chồng lên, tắt `RaycastTarget` trên image visual gốc.

### 5b. Haptic Feedback

**Tạo `HapticManager.cs`:**
```csharp
public static class HapticManager
{
#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject _vibrator;

    static HapticManager()
    {
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
    }
#endif

    public static void Light()  => Vibrate(30);
    public static void Medium() => Vibrate(60);
    public static void Heavy()  => Vibrate(100);

    static void Vibrate(long ms)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _vibrator?.Call("vibrate", ms);
#elif UNITY_IOS && !UNITY_EDITOR
        // iOS: dùng iOSHapticFeedback plugin hoặc Handheld.Vibrate()
        Handheld.Vibrate();
#endif
    }
}
```

**Tích hợp vào game:**
```csharp
// Trong AttackPhase.cs hoặc BattleManager — khi tấn công
HapticManager.Light();   // player tấn công

// Khi nhận sát thương
HapticManager.Medium();  // nhận hit

// Khi unit chết / boss defeated
HapticManager.Heavy();
```

---

## MOB-6: Lock Screen Orientation (Landscape)

### Vấn đề
`ProjectSettings.asset` có `defaultScreenOrientation: 4` (Auto Rotation). Trong lúc chơi, nếu người dùng xoay điện thoại thì UI bị vỡ layout vì Canvas được thiết kế cho Landscape 1920×1080.

### Hướng implement

**Cách 1 — Project Settings (đơn giản nhất):**
- `Edit → Project Settings → Player → Android/iOS → Resolution and Presentation`
- **Default Orientation:** `Landscape Left`
- Tắt tất cả Auto Rotation checkboxes

**Cách 2 — Force bằng code khi khởi động:**
```csharp
// GameManager.Awake()
#if UNITY_ANDROID || UNITY_IOS
    Screen.orientation = ScreenOrientation.LandscapeLeft;
    Screen.autorotateToPortrait = false;
    Screen.autorotateToPortraitUpsideDown = false;
    Screen.autorotateToLandscapeLeft = true;
    Screen.autorotateToLandscapeRight = true;
#endif
```

**Canvas Reference Resolution:**
- Tất cả Canvas → `Canvas Scaler → Scale with Screen Size`
- Reference Resolution: **1920 × 1080**
- Screen Match Mode: **Match Width Or Height → 0.5** (cân bằng cho mọi tỉ lệ màn hình)

---

## Thứ tự thực hiện

```
Giai đoạn 1 — Hoàn thiện core (1–2 ngày):
├── [UX-1] Pause Menu
│   ├── Thêm InputMode.Pause
│   ├── Bind ESC trong .inputactions
│   └── Tạo PauseMenuUI.cs + Prefab
└── [UX-2] Settings
    ├── Tạo AudioMixer asset (MainMixer)
    ├── Tạo AudioSettingsUI.cs
    ├── Tạo GraphicsSettingsUI.cs
    ├── Tạo SettingsHubUI.cs
    └── Gắn nút Settings vào StartMenuUI

Giai đoạn 2 — Feedback & Safety (nửa ngày):
├── [UX-3] ToastUI.cs + SaveRoutine refactor
└── [UX-4] /player/exists endpoint + ValidateAndApplyTransferCode()

Giai đoạn 3 — Mobile Foundation (1 ngày):
├── [MOB-6] Lock orientation (30 phút) — làm TRƯỚC vì ảnh hưởng layout
├── [MOB-1] SafeAreaFitter.cs + tắt androidRenderOutsideSafeArea
├── [MOB-3] Nút Pause mobile + MobileInputUI.RefreshPanels() fix
└── [MOB-4] AndroidBackHandler.cs

Giai đoạn 4 — Mobile Polish (1 ngày):
├── [MOB-2] DynamicJoystick.cs
├── [MOB-5a] Touch target size audit
└── [MOB-5b] HapticManager.cs

Giai đoạn 5 — Gameplay Polish (1–2 giờ):
└── [UX-5] CheckForEncounter() scale theo bước
```

---

## Checklist hoàn thành

### Desktop / WebGL
- [ ] UX-1: ESC mở Pause Menu trong MapScene
- [ ] UX-1: ESC mở Pause Menu trong BattleScene (không dừng animation battle khi đang trong phase attack)
- [ ] UX-1: Time.timeScale reset về 1 khi load scene
- [ ] UX-2: AudioMixer tạo, 3 groups exposed
- [ ] UX-2: Volume lưu vào PlayerPrefs, restore khi khởi động
- [ ] UX-2: GraphicsSettings hoạt động trên WebGL build
- [ ] UX-2: Nút "Cài đặt" trên MainMenu mở SettingsHub
- [ ] UX-3: Save thất bại hiện thông báo màu đỏ
- [ ] UX-3: Save thành công hiện thông báo màu xanh, tự ẩn sau 2 giây
- [ ] UX-4: Nhập Transfer Code sai không ghi đè ID cũ
- [ ] UX-4: Hiện số slot tìm thấy khi validate thành công
- [ ] UX-5: Không encounter trong 30 bước đầu
- [ ] UX-5: Chắc chắn encounter trong 250 bước

### Mobile / APK
- [ ] MOB-1: Tắt `androidRenderOutsideSafeArea` trong ProjectSettings
- [ ] MOB-1: `SafeAreaFitter` gắn vào MapPanel, BattlePanel, SkillMenuPanel
- [ ] MOB-1: Test trên thiết bị có notch (hoặc dùng Device Simulator trong Unity)
- [ ] MOB-2: DynamicJoystick xuất hiện tại điểm chạm, ẩn khi nhả
- [ ] MOB-2: Joystick cố định cũ vẫn hoạt động khi `useDynamicJoystick = false`
- [ ] MOB-3: Nút ☰ Pause hiển thị trên cả MapScene và BattleScene
- [ ] MOB-3: Khi vào InputMode.Pause, joystick và nút battle ẩn hoàn toàn
- [ ] MOB-4: Android Back button trong Map → mở Pause
- [ ] MOB-4: Android Back button trong Pause → đóng Pause (không thoát app)
- [ ] MOB-5: Tất cả nút action ≥ 88×88 px
- [ ] MOB-5: Haptic nhẹ khi tấn công, trung khi nhận hit
- [ ] MOB-6: Game chỉ chạy Landscape, không auto-rotate
- [ ] MOB-6: Canvas Scaler dùng Scale with Screen Size 1920×1080, Match 0.5
