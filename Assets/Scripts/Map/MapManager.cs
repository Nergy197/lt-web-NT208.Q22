using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;



    [Header("Current Map")]
    public Mapdata currentMap;



    [Header("Battle Data")]

    // ScriptableObject EnemyData
    public List<EnemyData> currentEnemies =
        new List<EnemyData>();

    public int currentMapLevel;

    public bool isInBattle = false;



    [Header("Random Encounter")]

    public float encounterRate = 0.02f;

    private int stepsSinceLastEncounter = 0;



    // ================= INIT =================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }



    // ================= SET MAP =================

    public void SetMap(Mapdata map)
    {
        currentMap = map;

        Debug.Log(
        $"[MapManager] Loaded Map: {map.mapName}");


        currentMap.GenerateRandomEnemyEffects();

        currentMap.GenerateRandomPlayerEffects();


        stepsSinceLastEncounter = 0;

        isInBattle = false;
    }



    // ================= CHECK ENCOUNTER =================

    public void CheckForEncounter()
    {
        if (isInBattle)
            return;


        if (currentMap == null)
            return;


        if (currentMap.possibleEnemies.Count == 0)
            return;


        stepsSinceLastEncounter++;


        if (Random.value < encounterRate)
        {
            TriggerRandomEncounter();
        }
    }



    // ================= RANDOM ENCOUNTER =================

    private void TriggerRandomEncounter()
    {
        currentEnemies.Clear();


        int count =
            Random.Range(1, 4);



        for (int i = 0; i < count; i++)
        {
            EnemyData enemy =
                currentMap.GetRandomEnemy();


            if (enemy == null)
                continue;


            currentEnemies.Add(enemy);
        }



        currentMapLevel =
            currentMap.enemyLevel;



        Debug.Log("===== ENCOUNTER =====");


        foreach (var e in currentEnemies)
        {
            Debug.Log(
            $"Enemy: {e.entityName} | Attacks: {e.attacks.Count}");
        }



        StartBattle();
    }



    // ================= START BATTLE =================

    public void StartBattle()
    {
        if (isInBattle)
            return;


        if (currentEnemies.Count == 0)
        {
            Debug.LogError("No enemies to battle");
            return;
        }


        isInBattle = true;


        // Lưu vị trí nhân vật trước khi chuyển sang BattleScene
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null && GameManager.Instance != null)
        {
            GameManager.Instance.SetLastMapPosition(playerObj.transform.position);
            Debug.Log($"[MapManager] Saved player position: {playerObj.transform.position}");
        }


        SceneManager.LoadScene("BattleScene");
    }



    // ================= END BATTLE =================

    public void EndBattle()
    {
        isInBattle = false;


        currentEnemies.Clear();


        SceneManager.LoadScene("MapScene");
    }



    // ================= APPLY EFFECT =================

    public void ApplyPlayerEffects(
        PlayerStatus player)
    {
        if (currentMap == null)
            return;


        currentMap.ApplyPlayerEffects(player);
    }



    public void ApplyEnemyEffects(
        EnemyStatus enemy)
    {
        if (currentMap == null)
            return;


        currentMap.ApplyEnemyEffects(enemy);
    }



    // ================= CREATE ENEMY STATUS =================

    public List<EnemyStatus> CreateEnemyStatuses()
    {
        List<EnemyStatus> list =
            new List<EnemyStatus>();


        foreach (var data in currentEnemies)
        {
            if (data == null)
                continue;


            EnemyStatus enemy =
                data.CreateStatus();


            enemy.SetLevel(currentMapLevel);


            ApplyEnemyEffects(enemy);


            list.Add(enemy);
        }


        return list;
    }
}