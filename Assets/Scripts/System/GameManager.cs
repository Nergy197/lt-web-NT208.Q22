using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [Header("Backend")]
    // Editor: tự động dùng "http://localhost:3000" | WebGL build: dùng URL tương đối ""
    [HideInInspector] public string backendBaseURL = "";
    public string saveURL = "/player/save";

    string FullURL(string path) => backendBaseURL + path;

    [Header("Quest")]
    [Tooltip("Kéo QuestManager GameObject vào đây, hoặc để trống nếu QuestManager.Instance đã tồn tại.")]
    public QuestManager questManager;

    [Header("Runtime")]
    public Party playerParty;
    public bool isLoaded = false;

    [Header("Map Position")]
    // Vị trí nhân vật trên map trước khi vào battle (persist qua scene load)
    private Vector2 lastMapPosition = Vector2.zero;
    private bool hasMapPosition = false;

    // Save Point đã lưu: dùng khi load game để teleport player về đúng nơi
    public string pendingSavePointId { get; private set; } = null;
    private string pendingSaveScene = null;

    public void SetLastMapPosition(Vector2 pos)
    {
        lastMapPosition = pos;
        hasMapPosition = true;
    }

    public bool TryGetLastMapPosition(out Vector2 pos)
    {
        pos = lastMapPosition;
        return hasMapPosition;
    }

    public void ClearMapPosition()
    {
        hasMapPosition = false;
    }

    [Header("Save Slots")]
    public int currentSaveSlot = 0;
    private bool startAsNewGame = false; // Cờ báo hiệu bắt đầu New Game

    bool chapter1TutorialCompleted;

    [Header("Database")]
    public List<PlayerData> playerDatabase = new();

    // ================= GUEST ID =================

    /// <summary>
    /// Trả về ID duy nhất của máy/trình duyệt đang chơi.
    /// Lần đầu tiên sẽ tự sinh GUID và lưu vào PlayerPrefs.
    /// Những lần sau sẽ đọc lại ID cũ đã lưu.
    /// </summary>
    public string GetPlayerId()
    {
        if (!PlayerPrefs.HasKey("DevicePlayerId"))
        {
            string newId = "guest_" + System.Guid.NewGuid().ToString().Substring(0, 8);
            PlayerPrefs.SetString("DevicePlayerId", newId);
            PlayerPrefs.Save();
            Debug.Log("[GM] Tạo Guest ID mới: " + newId);
        }
        return PlayerPrefs.GetString("DevicePlayerId");
    }

    /// <summary>
    /// Ghi đè Guest ID bằng mã người chơi nhập vào (Transfer Code).
    /// Dùng khi người chơi muốn lấy lại file save từ máy khác.
    /// Trả về true nếu mã hợp lệ.
    /// </summary>
    public bool SetPlayerId(string transferCode)
    {
        if (string.IsNullOrWhiteSpace(transferCode))
        {
            Debug.LogWarning("[GM] Transfer Code không hợp lệ (rỗng).");
            return false;
        }

        transferCode = transferCode.Trim().ToLower();

        // Kiểm tra định dạng: phải bắt đầu bằng "guest_"
        if (!transferCode.StartsWith("guest_"))
        {
            Debug.LogWarning("[GM] Transfer Code không hợp lệ (sai định dạng): " + transferCode);
            return false;
        }

        PlayerPrefs.SetString("DevicePlayerId", transferCode);
        PlayerPrefs.Save();
        Debug.Log("[GM] Đã áp dụng Transfer Code: " + transferCode);
        return true;
    }

    // ================= INIT =================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // Fix error Destroy
        DontDestroyOnLoad(gameObject);
        EnsurePlayerDatabase();
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Tự động chọn base URL:
        // - WebGL build: URL tương đối, không cần base
        // - Unity Editor / Standalone: dùng localhost
#if UNITY_EDITOR || UNITY_STANDALONE
            backendBaseURL = "http://localhost:3000";
#else
            backendBaseURL = ""; // WebGL: relative URL
#endif
        Debug.Log("[GM] backendBaseURL = " + backendBaseURL);
    }

    // ================= WEBGL BROWSER WARNING =================

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void EnableBeforeUnloadWarning();
    [DllImport("__Internal")] private static extern void DisableBeforeUnloadWarning();
#else
    static void EnableBeforeUnloadWarning() { } // No-op outside WebGL
    static void DisableBeforeUnloadWarning() { }
#endif

    /// <summary>Bật cảnh báo trình duyệt khi đóng tab (gọi khi vào gameplay).</summary>
    public void ActivateBrowserWarning()  => EnableBeforeUnloadWarning();

    /// <summary>Tắt cảnh báo trình duyệt (gọi khi về menu chính).</summary>
    public void DeactivateBrowserWarning() => DisableBeforeUnloadWarning();

    // ================= AUTO-SAVE =================

    void OnApplicationQuit()
    {
        if (isLoaded) QuickSaveToLocal();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && isLoaded) QuickSaveToLocal();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Auto-save mỗi khi vào Chapter5_MapBattle (sau trận, sau cutscene, v.v.)
        // Đây là autosave đáng tin cậy nhất cho WebGL (OnApplicationQuit không chạy khi đóng tab)
        if (scene.name == "Chapter5_MapBattle" && isLoaded)
        {
            QuickSaveToLocal();
            Debug.Log("[GM] Auto-save khi vào Chapter5_MapBattle.");
        }
    }

    /// <summary>
    /// Lưu nhanh vào PlayerPrefs (đồng bộ, không cần coroutine).
    /// Dùng khi trình duyệt sắp đóng — không kịp gửi lên server.
    /// </summary>
    public void QuickSaveToLocal()
    {
        PlayerSave save = BuildCurrentSave(pendingSavePointId, pendingSaveScene);
        if (save == null) return;

        SaveService.SaveLocal(currentSaveSlot, save);
        Debug.Log($"[GM] QuickSave to Slot {currentSaveSlot} (on quit/pause)");
    }

    /// <summary>
    /// Dựng PlayerSave từ trạng thái gameplay hiện tại (party, quest, tutorial flag).
    /// Trả về null nếu party rỗng (caller phải hủy lưu để tránh ghi đè save trống).
    /// </summary>
    PlayerSave BuildCurrentSave(string savePointId, string saveScene)
    {
        if (playerParty == null || playerParty.Members == null || playerParty.Members.Count == 0)
            return null;

        var save = new PlayerSave
        {
            _id = GetPlayerId() + "_slot_" + currentSaveSlot,
            slotId = currentSaveSlot,
            saveTime = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
            lastSavePointId = savePointId,
            lastSaveScene = saveScene,
            party = new List<UnitSave>()
        };

        foreach (Status s in playerParty.Members)
        {
            save.party.Add(new UnitSave
            {
                entityName = s.entityName,
                level = s.level,
                currentHP = s.currentHP,
                currentExp = (s is PlayerStatus ps) ? ps.currentExp : 0
            });
        }

        var qm = questManager != null ? questManager : QuestManager.Instance;
        if (qm != null) save.questProgress = qm.BuildSaveData();
        save.chapter1TutorialCompleted = chapter1TutorialCompleted;

        return save;
    }

    void Start()
    {
        // Tránh tự động Load Slot 0 khi đang ở Menu Chính (Menu sẽ tự gọi LoadAndStartGame sau)
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Chapter0_Login")
        {
            StartCoroutine(LoadPlayerParty());
        }
    }

    // ================= LOAD =================

    IEnumerator LoadPlayerParty()
    {
        string json = "";

        if (startAsNewGame)
        {
            // 1. TẠO NEW GAME HOÀN TOÀN MỚI
            Debug.Log($"[LOAD] Bắt đầu New Game ở Slot {currentSaveSlot}...");
            PlayerSave newSave = new PlayerSave();
            newSave._id = GetPlayerId() + "_slot_" + currentSaveSlot; // Phân biệt ID theo slot trên Database
            newSave.slotId = currentSaveSlot;
            newSave.saveTime = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            newSave.party = BuildStarterParty();

            // Xóa điểm save cũ
            pendingSavePointId = null;
            pendingSaveScene = null;

            json = JsonUtility.ToJson(newSave);

            // Ép lưu ngay xuống PlayerPrefs để tạo file
            SaveService.SaveLocal(currentSaveSlot, newSave);
        }
        else if (SaveService.TryLoadLocal(currentSaveSlot, out json))
        {
            // 2. Tải dữ liệu từ Slot tương ứng trong Trình duyệt
            Debug.Log($"[LOAD] Tải thành công Slot {currentSaveSlot} từ Local Storage: " + json);
            yield return null; // Cho qua frame
        }
        else
        {
            // 3. Fallback: Cố tải từ Server nếu Local Storage mất (hoặc máy khác)
            // Tải theo ID độc nhất của thiết bị + slot
            string slotIdOnServer = GetPlayerId() + "_slot_" + currentSaveSlot;
            string url = FullURL("/player/" + slotIdOnServer);
            Debug.Log("[LOAD] Tìm kiếm file từ Server: " + url);

            string serverJson = null;
            yield return SaveService.LoadFromServer(url, r => serverJson = r);

            if (!string.IsNullOrEmpty(serverJson))
            {
                json = serverJson;
                Debug.Log("[LOAD] Lấy Save từ Server: " + json);
            }
            else
            {
                Debug.LogWarning("[LOAD FAILED] Không tìm thấy! Tự động tạo New Game Offline...");
                PlayerSave fallbackSave = new PlayerSave();
                fallbackSave._id = GetPlayerId() + "_slot_" + currentSaveSlot;
                fallbackSave.slotId = currentSaveSlot;
                fallbackSave.party = BuildStarterParty();
                json = JsonUtility.ToJson(fallbackSave);
            }
        }

        // Reset cờ New Game
        startAsNewGame = false;

        CreatePartyFromJson(json);

        isLoaded = true;
        Debug.Log("PLAYER READY");

        // Quest: chỉ load progress — Q001 start sau Chapter2_Tutorial (TryStartChapter1Quests)
        var qm = questManager != null ? questManager : QuestManager.Instance;
        if (qm != null && !qm.HasAnyProgress())
            qm.LoadProgress();
    }

    public static string Chapter1TutorialPrefsKey(int slot) => "Chapter1TutorialDone_" + slot;

    public bool IsChapter1TutorialCompleted()
    {
        if (chapter1TutorialCompleted) return true;
        return PlayerPrefs.GetInt(Chapter1TutorialPrefsKey(currentSaveSlot), 0) == 1;
    }

    public void MarkChapter1TutorialCompleted()
    {
        chapter1TutorialCompleted = true;
        PlayerPrefs.SetInt(Chapter1TutorialPrefsKey(currentSaveSlot), 1);
        PlayerPrefs.Save();
        Debug.Log("[GM] Chapter1 tutorial completed.");
    }

    void CreatePartyFromJson(string json)
    {
        EnsurePlayerDatabase();
        PlayerSave save = JsonUtility.FromJson<PlayerSave>(json);

        if (save == null)
        {
            Debug.LogError("[LOAD] JsonUtility parse thất bại. JSON: " + json);
            return;
        }

        if (save.party == null || save.party.Count == 0)
        {
            Debug.LogWarning("[LOAD] save.party rỗng hoặc null. Tự động thêm nhân vật mặc định để cứu save. JSON: " + json);
            save.party = BuildStarterParty();

            if (save.party.Count == 0)
            {
                Debug.LogError("[LOAD] Database rỗng! Không thể cứu được party.");
                return;
            }
        }

        // Party người chơi cap = Party.PlayerMaxMembers; cảnh báo nếu save chứa nhiều hơn (member dư sẽ bị bỏ).
        if (save.party.Count > Party.PlayerMaxMembers)
            Debug.LogWarning($"[LOAD] save.party có {save.party.Count} member > giới hạn {Party.PlayerMaxMembers}. " +
                             $"Các member dư sẽ bị bỏ qua khi nạp.");

        playerParty = new Party(PartyType.Player);

        foreach (UnitSave unit in save.party)
        {
            PlayerData data = FindPlayerData(unit.entityName);

            if (data == null)
            {
                Debug.LogError("[LOAD] Missing PlayerData: " + unit.entityName);
                continue;
            }

            PlayerStatus status = data.CreateStatus();

            if (status == null)
            {
                Debug.LogError("[LOAD] CreateStatus FAILED for: " + unit.entityName);
                continue;
            }

            status.SetLevel(unit.level);
            status.currentHP = Mathf.Clamp(unit.currentHP, 1, status.MaxHP);
            if (status is PlayerStatus ps2) ps2.currentExp = Mathf.Max(0, unit.currentExp);

            playerParty.AddMember(status);

            Debug.Log(
                $"[LOAD] {status.entityName} | " +
                $"Lv:{status.level} | " +
                $"HP:{status.currentHP}/{status.MaxHP} | " +
                $"Skills:{status.SkillCount}"
            );
        }

        Debug.Log("[LOAD] Party OK: " + playerParty.Members.Count + " member(s)");

        // Tải Save Point nếu có
        if (!string.IsNullOrEmpty(save.lastSavePointId))
        {
            pendingSavePointId = save.lastSavePointId;
            pendingSaveScene = save.lastSaveScene;
            Debug.Log($"[LOAD] Save Point: {pendingSaveScene} → {pendingSavePointId}");
        }

        chapter1TutorialCompleted = save.chapter1TutorialCompleted;
        int prefsVal = PlayerPrefs.GetInt(Chapter1TutorialPrefsKey(currentSaveSlot), 0);
        if (!chapter1TutorialCompleted && prefsVal == 1)
            chapter1TutorialCompleted = true;
        Debug.Log($"[GameManager] CreatePartyFromJson: chapter1TutorialCompleted(json)={save.chapter1TutorialCompleted} | PrefsKey={prefsVal} | final={chapter1TutorialCompleted}");

        // Tải tiến trình quest từ server
        var qm = questManager != null ? questManager : QuestManager.Instance;
        if (qm != null && save.questProgress != null)
        {
            qm.LoadProgressFromData(save.questProgress);
        }
    }

    /// <summary>
    /// Tạo party khởi đầu cho New Game: tối đa Party.PlayerMaxMembers nhân vật đầu tiên
    /// (tên khác nhau) lấy từ playerDatabase. Trả về list rỗng nếu database trống.
    /// </summary>
    List<UnitSave> BuildStarterParty(int maxCount = Party.PlayerMaxMembers)
    {
        EnsurePlayerDatabase();
        var list = new List<UnitSave>();
        if (playerDatabase == null) return list;

        foreach (var data in playerDatabase)
        {
            if (data == null) continue;
            if (list.Exists(u => u.entityName == data.entityName)) continue; // tránh trùng tên

            list.Add(new UnitSave
            {
                entityName = data.entityName,
                level = 1,
                currentHP = data.baseHP,
                currentExp = 0
            });

            if (list.Count >= maxCount) break;
        }

        if (list.Count == 0)
            Debug.LogError("[GM] BuildStarterParty: playerDatabase rỗng — không tạo được starter nào.");

        return list;
    }

    /// <summary>
    /// Đảm bảo playerDatabase có đủ PlayerData (Hero, v.v.).
    /// Cần khi test Chapter5_MapBattle trực tiếp — bootstrap tạo GameManager trống.
    /// </summary>
    public void EnsurePlayerDatabase()
    {
        if (playerDatabase == null)
            playerDatabase = new List<PlayerData>();

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PlayerData", new[] { "Assets/Data/Characters" });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            PlayerData data = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerData>(path);
            if (data != null && !playerDatabase.Contains(data))
                playerDatabase.Add(data);
        }
#endif

        PlayerData[] fromResources = Resources.LoadAll<PlayerData>("Characters");
        for (int i = 0; i < fromResources.Length; i++)
        {
            if (fromResources[i] != null && !playerDatabase.Contains(fromResources[i]))
                playerDatabase.Add(fromResources[i]);
        }
    }

    PlayerData FindPlayerData(string name)
    {
        foreach (var p in playerDatabase)
        {
            if (p == null)
                continue;

            if (p.entityName == name)
                return p;
        }

        return null;
    }

    public Party GetPlayerParty()
    {
        return playerParty;
    }

    // ================= START GAME =================

    bool _loadingInProgress;

    public void LoadAndStartGame()
    {
        if (_loadingInProgress)
        {
            Debug.LogWarning("[GameManager] LoadAndStartGame đang chạy, bỏ qua lần gọi thứ hai.");
            return;
        }
        StartCoroutine(LoadAndStartGameRoutine());
    }

    private IEnumerator LoadAndStartGameRoutine()
    {
        _loadingInProgress = true;
        Debug.Log($"[GameManager] Bắt đầu tiến trình tải data cho Slot {currentSaveSlot}");
        isLoaded = false;
        yield return StartCoroutine(LoadPlayerParty());
        _loadingInProgress = false;
        StartGame();
    }

    public void StartGame()
    {
        if (!isLoaded)
        {
            Debug.LogWarning("Player not loaded yet. Vui lòng gọi LoadAndStartGame() thay vì StartGame().");
            return;
        }

        // Bật cảnh báo trình duyệt khi đang chơi game
        ActivateBrowserWarning();

        // Reset về Map mode trước khi load scene — tránh mobile UI bị kẹt ở
        // InputMode.UI (set khi BackToMainMenu) và không bao giờ reset nếu
        // BattleManager khởi tạo chậm hoặc lỗi ở lần chơi thứ hai.
        InputController.Instance?.SetMode(InputMode.Map);

        bool tutorialDone = IsChapter1TutorialCompleted();
        bool hasSaveScene  = !string.IsNullOrEmpty(pendingSaveScene);

        Debug.Log($"[GameManager] StartGame → Slot={currentSaveSlot} | chapter1TutorialCompleted={chapter1TutorialCompleted} | " +
                  $"PrefsKey={PlayerPrefs.GetInt(Chapter1TutorialPrefsKey(currentSaveSlot), 0)} | " +
                  $"tutorialDone={tutorialDone} | pendingSaveScene='{pendingSaveScene}'");

        // Game mới hoàn toàn → bắt đầu từ Chapter0 (intro lore)
        if (!tutorialDone && !hasSaveScene)
        {
            Debug.Log("[GameManager] → Chapter0_Introduction (tutorial chưa hoàn thành, không có save point)");
            SceneManager.LoadScene("Chapter1_Introduction");
            return;
        }

        if (hasSaveScene)
        {
            Debug.Log($"[GameManager] → {pendingSaveScene} (load từ save point '{pendingSavePointId}')");
            SceneManager.LoadScene(pendingSaveScene);
        }
        else
        {
            Debug.Log("[GameManager] → Chapter5_MapBattle (tutorial done nhưng không có save point)");
            SceneManager.LoadScene("Chapter5_MapBattle");
        }
    }

    // ================= SAVE AT POINT =================

    /// <summary>Hồi máu + lưu vị trí save point rồi gửi lên server.</summary>
    public void SaveAtPoint(string pointId, string sceneName)
    {
        // Hồi máu toàn bộ party (bao gồm hồi sinh unit đã chết)
        if (playerParty != null)
        {
            foreach (var member in playerParty.Members)
            {
                if (!member.IsAlive) member.Revive(member.MaxHP);
                member.HealFull();
            }
            Debug.Log("[SAVE POINT] Party healed.");
        }

        // Cập nhật runtime
        pendingSavePointId = pointId;
        pendingSaveScene = sceneName;

        // Đẩy lên server
        StartCoroutine(SaveRoutine(pointId, sceneName));
    }

    /// <summary>Xóa pending save point sau khi đã dùng (được gọi từ PlayerMovement sau khi teleport).</summary>
    public void ConsumeSavePoint()
    {
        pendingSavePointId = null;
        pendingSaveScene = null;
    }

    /// <summary>Khi party chết: hồi máu full, chuyển về map của save point cuối cùng.
    /// PlayerMovement sẽ dò tìm SavePoint và teleport nhân vật về đó.</summary>
    public void RespawnAtSavePoint()
    {
        Debug.Log("[GM] Respawning at save point...");

        // Hồi máu toàn bộ party (bao gồm hồi sinh unit đã chết)
        if (playerParty != null)
        {
            foreach (var member in playerParty.Members)
            {
                if (!member.IsAlive) member.Revive(member.MaxHP);
                member.HealFull();
            }
        }

        // Xóa vị trí battle cũ
        ClearMapPosition();

        // Nếu có save point đã lưu, đặt lại pendingSavePointId để PlayerMovement dò tìm khi load scene
        if (!string.IsNullOrEmpty(pendingSaveScene))
        {
            // pendingSavePointId và pendingSaveScene đã tồn tại sẵn từ lần save trước
            Debug.Log($"[GM] Respawn → {pendingSaveScene} at {pendingSavePointId}");
            SceneManager.LoadScene(pendingSaveScene);
        }
        else
        {
            Debug.Log("[GM] No save point found, respawn at Chapter5_MapBattle default.");
            SceneManager.LoadScene("Chapter5_MapBattle");
        }
    }

    // ================= SAVE =================

    public void SavePlayerParty()
    {
        StartCoroutine(SaveRoutine());
    }

    /// <summary>Save với callback khi hoàn tất. onComplete(true) nếu server backup thành công.</summary>
    public void SavePlayerPartyWithCallback(System.Action<bool> onComplete)
    {
        StartCoroutine(SaveRoutine(onComplete: onComplete));
    }

    IEnumerator SaveRoutine(string savePointId = null, string saveScene = null, System.Action<bool> onComplete = null)
    {
        // Giữ lại save point hiện tại nếu không truyền tham số
        PlayerSave save = BuildCurrentSave(savePointId ?? pendingSavePointId, saveScene ?? pendingSaveScene);

        if (save == null)
        {
            Debug.LogError("[SAVE] LỖI CỰC NGHIÊM TRỌNG: playerParty rỗng hoặc NULL! Hủy tiến trình lưu để tránh làm hỏng file save.");
            onComplete?.Invoke(false);
            yield break; // THOÁT NGAY, không lưu đè dữ liệu rỗng lên LocalStorage/Server
        }

        // Quest: lưu PlayerPrefs làm fallback (questProgress đã được gắn trong BuildCurrentSave).
        var qm = questManager != null ? questManager : QuestManager.Instance;
        if (qm != null) qm.SaveProgress();

        // Lưu local trước (an toàn nhất kể cả khi mất mạng), rồi backup lên server.
        SaveService.SaveLocal(currentSaveSlot, save);
        Debug.Log($"==== LƯU GAME THÀNH CÔNG VÀO TRÌNH DUYỆT (SLOT {currentSaveSlot}) ====");

        yield return SaveService.BackupToServer(FullURL(saveURL), save, onComplete);
    }

    // ================= DELETE SAVE =================

    public void DeleteSaveSlot(int slotIndex)
    {
        // Xóa cả hai key dù save chính có tồn tại hay không (tránh leftover tutorial key)
        SaveService.DeleteLocal(slotIndex);
        PlayerPrefs.DeleteKey(Chapter1TutorialPrefsKey(slotIndex));
        PlayerPrefs.Save();
        Debug.Log($"[GameManager] Đã xoá Slot {slotIndex} (save + tutorial key).");
    }

    /// <summary>Debug only — xóa TOÀN BỘ PlayerPrefs để test fresh.</summary>
    [ContextMenu("DEBUG: Clear All Save Data")]
    public void DebugClearAllSaveData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.LogWarning("[GameManager] ĐÃ XÓA TOÀN BỘ PlayerPrefs!");
    }

    // ================= START MENU HELPERS =================

    /// <summary>Trả về danh sách 10 Slot Save để hiển thị lên UI Start Menu.</summary>
    public PlayerSave[] GetAllSaveSlotsMetadata()
    {
        PlayerSave[] slots = new PlayerSave[10];
        for (int i = 0; i < 10; i++)
        {
            if (SaveService.TryLoadLocal(i, out string json))
                slots[i] = JsonUtility.FromJson<PlayerSave>(json);
            else
                slots[i] = null; // Slot trống
        }
        return slots;
    }

    /// <summary>Thiết lập cờ để LoadGame bắt đầu Load lại từ đầu Party hoàn toàn mới.</summary>
    public void PrepareNewGame()
    {
        startAsNewGame = true;
        chapter1TutorialCompleted = false;
        PlayerPrefs.DeleteKey(Chapter1TutorialPrefsKey(currentSaveSlot));

        var qm = questManager != null ? questManager : QuestManager.Instance;
        if (qm != null)
        {
            qm.ActiveQuests.Clear();
            qm.CompletedQuests.Clear();
            PlayerPrefs.DeleteKey(QuestManager.SaveKey);
        }
    }
}