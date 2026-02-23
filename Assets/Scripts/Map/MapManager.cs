using UnityEngine;
using UnityEngine.SceneManagement;
public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("Current Map")]
    public Mapdata currentMap;

    public EnemyData currentEnemy;
    public int currentMapLevel;

    [Header("Random Encounter")]
    public float encounterRate = 0.05f; // 5% chance per step
    private int stepsSinceLastEncounter = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void SetMap(Mapdata map)
    {
        currentMap = map;
        Debug.Log($"[MapManager] Loaded Map: {map.mapName}");
        currentMap.GenerateRandomEnemyEffects();
        currentMap.GenerateRandomPlayerEffects();
        stepsSinceLastEncounter = 0;
    }

    public int GetMapLevel()
    {
        if (currentMap == null)
        {
            Debug.LogWarning("MapManager: currentMap is NULL");
            return 1;
        }
        return currentMap.enemyLevel;
    }

    public void ApplyPlayerEffects(PlayerStatus player)
    {
        currentMap?.ApplyPlayerEffects(player);
    }

    public void ApplyEnemyEffects(EnemyStatus enemy)
    {
        currentMap?.ApplyEnemyEffects(enemy);
    }

    public void CheckForEncounter()
    {
        if (currentMap == null || currentMap.possibleEnemies.Count == 0) return;

        stepsSinceLastEncounter++;
        if (Random.value < encounterRate)
        {
            TriggerRandomEncounter();
            stepsSinceLastEncounter = 0;
        }
    }

    private void TriggerRandomEncounter()
    {
        EnemyData randomEnemy = currentMap.GetRandomEnemy();
        if (randomEnemy != null)
        {
            Debug.Log($"[MapManager] Random encounter with {randomEnemy.name}");
            StartBattle(randomEnemy, currentMap.enemyLevel);
        }
    }

    public void StartBattle(EnemyData enemy, int mapLevel)
    {
        currentEnemy = enemy;
        currentMapLevel = mapLevel;
        SceneManager.LoadScene("BattleScene");
    }
}
