using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [Header("Current Map")]
    public Mapdata currentMap;
    [Tooltip("Map mặc định khi bắt đầu game (chưa teleport lần nào). Gán Mapdata của vùng xuất phát.")]
    public Mapdata defaultMap;

    [Header("Battle Data")]
    public List<EnemyData> currentEnemies = new List<EnemyData>();
    public bool isInBattle = false;

    // Level thực tế cho trận hiện tại: ưu tiên override (từ trigger/action),
    // fallback về currentMap.enemyLevel khi dùng random encounter
    private int _levelOverride;
    private int EffectiveBattleLevel => _levelOverride > 0 ? _levelOverride : (currentMap?.enemyLevel ?? 1);
    private bool _loggedNoMap;

    [Header("Random Encounter")]
    [Tooltip("Xác suất mỗi lần CheckForEncounter() (0–1). Nên gọi CheckForEncounter tối đa ~1 lần/FixedUpdate khi đang di chuyển — ví dụ 0.001 ≈ trung bình vài chục giây đi liên tục (50Hz).")]
    [Range(0f, 1f)]
    public float encounterRate = 0.001f;
    private int stepsSinceLastEncounter = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (currentMap == null && defaultMap != null)
            SetMap(defaultMap);
    }

    public void SetMap(Mapdata map)
    {
        currentMap = map;
        _loggedNoMap = false;
        Debug.Log($"[MapManager] Loaded Map: {map.mapName} | lv={map.enemyLevel} | {map.possibleEnemies.Count} enemies");

        currentMap.GenerateRandomEnemyEffects();
        currentMap.GenerateRandomPlayerEffects();

        stepsSinceLastEncounter = 0;
        isInBattle = false;

        // Cập nhật tên khu vực trên Minimap (nếu có)
        if (Minimap.Instance != null)
            Minimap.Instance.UpdateMapName();
    }

    public void CheckForEncounter()
    {
        if (isInBattle) return;
        if (currentMap == null)
        {
            if (!_loggedNoMap) { Debug.LogWarning("[Encounter] currentMap null — gán defaultMap vào MapManager hoặc gọi SetMap()"); _loggedNoMap = true; }
            return;
        }
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
            if (enemy == null) { Debug.LogWarning("[Encounter] GetRandomEnemy() trả về null — kiểm tra possibleEnemies"); continue; }
            currentEnemies.Add(enemy);
        }

        if (currentEnemies.Count == 0)
        {
            Debug.LogWarning("[Encounter] Huỷ — tất cả enemy data null");
            return;
        }

        _levelOverride = 0;
        Debug.Log($"[Encounter] {currentEnemies.Count} enemy | map='{currentMap.mapName}' | lv={EffectiveBattleLevel}");
        foreach (var e in currentEnemies)
            Debug.Log($"  → {e.entityName} ({e.attacks.Count} attacks)");

        StartBattle();
    }

    /// <summary>Thiết lập enemy và level cho trận scripted (EnemyTrigger, BattleAction, Bootstrap).</summary>
    public void SetupBattle(IEnumerable<EnemyData> enemies, int level)
    {
        currentEnemies.Clear();
        foreach (var e in enemies)
            if (e != null) currentEnemies.Add(e);
        _levelOverride = Mathf.Max(1, level);
        Debug.Log($"[MapManager] SetupBattle: {currentEnemies.Count} enemy | lv={_levelOverride}");
    }

    public void StartBattle()
    {
        if (isInBattle)
        {
            Debug.LogWarning("[MapManager] StartBattle bị block — isInBattle=true. EndBattle() chưa được gọi?");
            return;
        }

        if (currentEnemies == null || currentEnemies.Count == 0)
        {
            Debug.LogError("[MapManager] StartBattle thất bại — currentEnemies rỗng");
            return;
        }

        currentEnemies.RemoveAll(e => e == null);
        if (currentEnemies.Count == 0)
        {
            Debug.LogError("[MapManager] StartBattle thất bại — tất cả enemy data null sau khi clean");
            return;
        }

        isInBattle = true;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null && GameManager.Instance != null)
            GameManager.Instance.SetLastMapPosition(playerObj.transform.position);

        Debug.Log($"[MapManager] → BattleScene | {currentEnemies.Count} enemy | lv={EffectiveBattleLevel} | map='{currentMap?.mapName ?? "none"}'");
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
            enemy.SetLevel(EffectiveBattleLevel);
            ApplyEnemyEffects(enemy);
            list.Add(enemy);
        }
        return list;
    }
}