using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("Current Map")]
    public Mapdata currentMap;


    [Header("Battle Data")]

    // DANH SÁCH ENEMY thay vì 1 enemy
    public List<EnemyData> currentEnemies =
        new List<EnemyData>();

    public int currentMapLevel;

    public bool isInBattle = false;


    [Header("Random Encounter")]
    public float encounterRate = 0.02f;

    private int stepsSinceLastEncounter = 0;



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



    public void CheckForEncounter()
    {
        if (isInBattle)
            return;


        if (currentMap == null ||
            currentMap.possibleEnemies.Count == 0)
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


            currentEnemies.Add(enemy);
        }


        currentMapLevel =
            currentMap.enemyLevel;


        Debug.Log("Encounter:");

        foreach (var e in currentEnemies)
        {
            Debug.Log("- " + e.name);
        }


        StartBattle();
    }



    // ================= START BATTLE =================

    public void StartBattle()
    {
        if (isInBattle)
            return;


        isInBattle = true;


        SceneManager
        .LoadScene("BattleScene");
    }



    public void EndBattle()
    {
        isInBattle = false;


        SceneManager
        .LoadScene("MapScene");
    }



    // ================= APPLY EFFECT =================

    public void ApplyPlayerEffects(
        PlayerStatus player)
    {
        currentMap?.ApplyPlayerEffects(player);
    }



    public void ApplyEnemyEffects(
        EnemyStatus enemy)
    {
        currentMap?.ApplyEnemyEffects(enemy);
    }

}