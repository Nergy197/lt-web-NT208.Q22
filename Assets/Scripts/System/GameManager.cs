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

    [Header("Runtime")]
    public Party playerParty;
    public bool isLoaded = false;

    [Header("Map Position")]
    // Vị trí nhân vật trên map trước khi vào battle (persist qua scene load)
    private Vector2 lastMapPosition = Vector2.zero;
    private bool hasMapPosition = false;

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

            playerParty.AddMember(status);

            Debug.Log(
                $"[LOAD] {status.entityName} | " +
                $"Lv:{status.level} | " +
                $"HP:{status.currentHP}/{status.MaxHP} | " +
                $"Skills:{status.SkillCount}"
            );
        }

        Debug.Log("[LOAD] Party OK: " + playerParty.Members.Count + " member(s)");
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

        SceneManager.LoadScene("MapScene");
    }

    // ================= SAVE =================

    public void SavePlayerParty()
    {
        StartCoroutine(SaveRoutine());
    }

    IEnumerator SaveRoutine()
    {
        PlayerSave save = new PlayerSave();
        save._id = "player001";
        save.party = new List<UnitSave>();

        foreach (Status s in playerParty.Members)
        {
            UnitSave unit = new UnitSave();
            unit.entityName = s.entityName;
            unit.level = s.level;
            unit.currentHP = s.currentHP;
            save.party.Add(unit);
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