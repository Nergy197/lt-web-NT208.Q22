using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Backend")]
    public string loadURL =
        "http://127.0.0.1:3000/player/player001";

    public string saveURL =
        "http://127.0.0.1:3000/player/save";


    [Header("Runtime")]
    public Party playerParty;


    [Header("Database")]
    public List<PlayerData> playerDatabase =
        new List<PlayerData>();



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

        UnityWebRequest req =
            UnityWebRequest.Get(loadURL);

        yield return req.SendWebRequest();


        if (req.result !=
            UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);

            yield break;
        }


        CreatePartyFromJson(
            req.downloadHandler.text);
    }



    void CreatePartyFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("JSON is empty");

            return;
        }


        PlayerSave save =
            JsonUtility.FromJson<PlayerSave>(json);


        if (save == null)
        {
            Debug.LogError("Save is NULL");

            return;
        }


        playerParty =
            new Party(PartyType.Player);



        foreach (UnitSave unit in save.party)
        {
            PlayerData data =
                FindPlayerData(unit.entityName);


            if (data == null)
            {
                Debug.LogError(
                "Missing PlayerData: "
                + unit.entityName);

                continue;
            }



            PlayerStatus status =
                data.CreateStatus();



            status.SetLevel(unit.level);


            status.currentHP =
                Mathf.Clamp(
                    unit.currentHP,
                    1,
                    status.MaxHP);



            playerParty.AddMember(status);
        }



        Debug.Log(
        "Party Loaded SUCCESS: "
        + playerParty.Members.Count);
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



    // ================= SAVE =================

    public void SavePlayerParty()
    {
        StartCoroutine(SaveRoutine());
    }



    IEnumerator SaveRoutine()
    {
        if (playerParty == null)
        {
            Debug.LogError("Party NULL");

            yield break;
        }



        PlayerSave save =
            new PlayerSave();


        save._id = "player001";


        save.party =
            new List<UnitSave>();



        foreach (Status s in playerParty.Members)
        {
            UnitSave unit =
                new UnitSave();


            unit.entityName =
                s.entityName;


            unit.level =
                s.level;


            unit.currentHP =
                s.currentHP;


            save.party.Add(unit);
        }



        string json =
            JsonUtility.ToJson(save);



        UnityWebRequest req =
            new UnityWebRequest(
                saveURL,
                "POST");



        byte[] body =
            Encoding.UTF8.GetBytes(json);



        req.uploadHandler =
            new UploadHandlerRaw(body);



        req.downloadHandler =
            new DownloadHandlerBuffer();



        req.SetRequestHeader(
            "Content-Type",
            "application/json");



        yield return req.SendWebRequest();



        if (req.result ==
            UnityWebRequest.Result.Success)
        {
            Debug.Log("SAVE SUCCESS");
        }
        else
        {
            Debug.LogError("SAVE FAILED");
        }
    }

}