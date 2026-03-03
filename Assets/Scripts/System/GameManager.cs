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
    public string loadURL = "/player/player001";
    public string saveURL = "/player/save";

    [Header("Runtime")]
    public Party playerParty;
    public bool isLoaded = false;

    [Header("Database")]
    public List<PlayerData> playerDatabase = new();

    // ================= INIT =================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        Debug.Log("Loading Player Party...");

        UnityWebRequest req = UnityWebRequest.Get(loadURL);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("LOAD FAILED: " + req.error);
            yield break;
        }

        CreatePartyFromJson(req.downloadHandler.text);

        isLoaded = true;
        Debug.Log("PLAYER READY");
    }

    void CreatePartyFromJson(string json)
    {
        PlayerSave save = JsonUtility.FromJson<PlayerSave>(json);

        playerParty = new Party(PartyType.Player);

        foreach (UnitSave unit in save.party)
        {
            PlayerData data = FindPlayerData(unit.entityName);

            if (data == null)
            {
                Debug.LogError("Missing PlayerData: " + unit.entityName);
                continue;
            }

            PlayerStatus status = data.CreateStatus();

            if (status == null)
            {
                Debug.LogError("CreateStatus FAILED");
                continue;
            }

            status.SetLevel(unit.level);

            status.currentHP =
                Mathf.Clamp(unit.currentHP, 1, status.MaxHP);

            playerParty.AddMember(status);

            Debug.Log(
                $"Loaded {status.entityName} | " +
                $"HP:{status.currentHP}/{status.MaxHP} | " +
                $"Skills:{status.SkillCount} | " +
                $"Basic:{status.BasicAttack}"
            );
        }

        Debug.Log("Party Loaded SUCCESS: " + playerParty.Members.Count);
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

        UnityWebRequest req = new UnityWebRequest(saveURL, "POST");

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