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

Giai đoạn 3 — Polish (1–2 giờ):
└── [UX-5] CheckForEncounter() scale theo bước
```

---

## Checklist hoàn thành

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
