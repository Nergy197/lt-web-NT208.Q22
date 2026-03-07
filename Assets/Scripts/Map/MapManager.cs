using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("Current Map")]
    public Mapdata currentMap;

    [Header("Battle Data")]
    public List<EnemyData> currentEnemies = new List<EnemyData>();
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
        Debug.Log($"[MapManager] Loaded Map: {map.mapName}");

        currentMap.GenerateRandomEnemyEffects();
        currentMap.GenerateRandomPlayerEffects();

        stepsSinceLastEncounter = 0;
        isInBattle = false;
    }

    public void CheckForEncounter()
    {
        if (isInBattle) return;
        if (currentMap == null) return;
        if (currentMap.possibleEnemies.Count == 0) return;

        stepsSinceLastEncounter++;

        if (Random.value < encounterRate)
            TriggerRandomEncounter();
    }

    private void TriggerRandomEncounter()
    {
        currentEnemies.Clear();
        int count = Random.Range(1, 4);

        for (int i = 0; i < count; i++)
        {
            EnemyData enemy = currentMap.GetRandomEnemy();
            if (enemy == null) continue;
            currentEnemies.Add(enemy);
        }

        currentMapLevel = currentMap.enemyLevel;

        Debug.Log("===== ENCOUNTER =====");
        foreach (var e in currentEnemies)
            Debug.Log($"Enemy: {e.entityName} | Attacks: {e.attacks.Count}");

        StartBattle();
    }

    public void StartBattle()
    {
        if (isInBattle) return;

        if (currentEnemies.Count == 0)
        {
            Debug.LogError("No enemies to battle");
            return;
        }

        isInBattle = true;

        // Lưu vị trí nhân vật để restore sau khi quay về MapScene.
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null && GameManager.Instance != null)
        {
            GameManager.Instance.SetLastMapPosition(playerObj.transform.position);
            Debug.Log($"[MapManager] Saved player position: {playerObj.transform.position}");
        }

        SceneManager.LoadScene("BattleScene");
    }

    public void EndBattle(bool playerWon = true)
    {
        isInBattle = false;
        currentEnemies.Clear();

        if (!playerWon && GameManager.Instance != null)
        {
            // Thua trận → hồi máu và respawn về Save Point gần nhất.
            Debug.Log("[MapManager] Thua trận → Respawn tại Save Point");
            GameManager.Instance.RespawnAtSavePoint();
        }
        else
        {
            SceneManager.LoadScene("MapScene");
        }
    }

    public void ApplyPlayerEffects(PlayerStatus player)
    {
        if (currentMap == null) return;
        currentMap.ApplyPlayerEffects(player);
    }

    public void ApplyEnemyEffects(EnemyStatus enemy)
    {
        if (currentMap == null) return;
        currentMap.ApplyEnemyEffects(enemy);
    }

    public List<EnemyStatus> CreateEnemyStatuses()
    {
        var list = new List<EnemyStatus>();
        foreach (var data in currentEnemies)
        {
            if (data == null) continue;
            EnemyStatus enemy = data.CreateStatus();
            enemy.SetLevel(currentMapLevel);
            ApplyEnemyEffects(enemy);
            list.Add(enemy);
        }
        return list;
    }
}