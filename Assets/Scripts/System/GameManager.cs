using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Backend")]
    // Editor: tự động dùng "http://localhost:3000" | WebGL build: dùng URL tương đối ""
    [HideInInspector] public string backendBaseURL = "";
    public string loadURL = "/player/player001";
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

    [Header("Database")]
    public List<PlayerData> playerDatabase = new();

    // ================= INIT =================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

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
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        StartCoroutine(LoadPlayerParty());
    }

    // ================= LOAD =================

    IEnumerator LoadPlayerParty()
    {
        string url = FullURL(loadURL);
        Debug.Log("[LOAD] Requesting: " + url);

        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[LOAD FAILED] " + req.error + " | URL: " + url);
            yield break;
        }

        string json = req.downloadHandler.text;
        Debug.Log("[LOAD] Raw JSON: " + json);

        CreatePartyFromJson(json);

        isLoaded = true;
        Debug.Log("PLAYER READY");

        // Khởi động quest đầu tiên CHỈ KHI chưa có save data (từ server hoặc PlayerPrefs)
        var qm = questManager != null ? questManager : QuestManager.Instance;
        if (qm != null)
        {
            // Nếu server không trả về quest data → thử load từ PlayerPrefs
            if (qm.ActiveQuests.Count == 0 && qm.CompletedQuests.Count == 0)
            {
                qm.LoadProgress(); // PlayerPrefs fallback
            }

            // Nếu vẫn không có gì → bắt đầu quest đầu tiên
            if (qm.ActiveQuests.Count == 0 && qm.CompletedQuests.Count == 0)
            {
                qm.StartQuest("Q001");
            }
        }
    }

    void CreatePartyFromJson(string json)
    {
        PlayerSave save = JsonUtility.FromJson<PlayerSave>(json);

        if (save == null)
        {
            Debug.LogError("[LOAD] JsonUtility parse thất bại. JSON: " + json);
            return;
        }

        if (save.party == null || save.party.Count == 0)
        {
            Debug.LogError("[LOAD] save.party rỗng hoặc null. JSON: " + json);
            return;
        }

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

        // Tải tiến trình quest từ server
        var qm = questManager != null ? questManager : QuestManager.Instance;
        if (qm != null && save.questProgress != null)
        {
            qm.LoadProgressFromData(save.questProgress);
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

    public void StartGame()
    {
        if (!isLoaded)
        {
            Debug.Log("Player not loaded yet");
            return;
        }

        // Nếu có save point đã lưu, load scene đó
        if (!string.IsNullOrEmpty(pendingSaveScene))
            SceneManager.LoadScene(pendingSaveScene);
        else
            SceneManager.LoadScene("MapScene");
    }

    // ================= SAVE AT POINT =================

    /// <summary>Hồi máu + lưu vị trí save point rồi gửi lên server.</summary>
    public void SaveAtPoint(string pointId, string sceneName)
    {
        // Hồi máu toàn bộ party
        if (playerParty != null)
        {
            foreach (var member in playerParty.Members)
                member.HealFull();
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

        // Hồi máu toàn bộ party
        if (playerParty != null)
        {
            foreach (var member in playerParty.Members)
                member.HealFull();
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
            Debug.Log("[GM] No save point found, respawn at MapScene default.");
            SceneManager.LoadScene("MapScene");
        }
    }

    // ================= SAVE =================

    public void SavePlayerParty()
    {
        StartCoroutine(SaveRoutine());
    }

    IEnumerator SaveRoutine(string savePointId = null, string saveScene = null)
    {
        PlayerSave save = new PlayerSave();
        save._id = "player001";
        save.party = new List<UnitSave>();

        // Giữ lại save point hiện tại nếu không truyền tham số
        save.lastSavePointId = savePointId ?? pendingSavePointId;
        save.lastSaveScene   = saveScene   ?? pendingSaveScene;

        foreach (Status s in playerParty.Members)
        {
            UnitSave unit = new UnitSave();
            unit.entityName = s.entityName;
            unit.level = s.level;
            unit.currentHP = s.currentHP;
            unit.currentExp = (s is PlayerStatus ps) ? ps.currentExp : 0;
            save.party.Add(unit);
        }

        // Gắn tiến trình quest vào payload
        var qm = questManager != null ? questManager : QuestManager.Instance;
        if (qm != null)
        {
            save.questProgress = qm.BuildSaveData();
            qm.SaveProgress(); // Lưu PlayerPrefs làm fallback
        }

        string json = JsonUtility.ToJson(save);

        string url = FullURL(saveURL);
        UnityWebRequest req = new UnityWebRequest(url, "POST");

        byte[] body = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("SAVE SUCCESS");
        }
        else
        {
            Debug.LogError("SAVE FAILED: " + req.error);
        }
    }
}