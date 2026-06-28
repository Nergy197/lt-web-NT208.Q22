using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý lựa chọn mục tiêu trong trận: chỉ số target địch/đồng minh và việc resolve
/// chúng thành Status còn sống. Tách khỏi BattleManager để gom toàn bộ logic chọn mục
/// tiêu (thuần, không đụng UI) vào một chỗ — dễ đọc và unit-test được.
/// </summary>
public class BattleTargeting
{
    private Party playerParty;
    private Party enemyParty;

    /// <summary>Index địch đang nhắm (trong danh sách địch còn sống).</summary>
    public int EnemyIndex;
    /// <summary>Index đồng minh đang nhắm (cho skill heal/buff).</summary>
    public int AllyIndex;
    /// <summary>Index player mà enemy AI đang nhắm — TÁCH RIÊNG khỏi AllyIndex.</summary>
    public int EnemyChosenPlayerIndex;

    public void SetParties(Party player, Party enemy)
    {
        playerParty = player;
        enemyParty = enemy;
        EnemyIndex = 0;
        AllyIndex = 0;
        EnemyChosenPlayerIndex = 0;
    }

    List<EnemyStatus> AliveEnemies()
    {
        var list = new List<EnemyStatus>();
        if (enemyParty != null)
            foreach (var e in enemyParty.Members) if (e.IsAlive) list.Add(e as EnemyStatus);
        return list;
    }

    List<PlayerStatus> AliveAllies()
    {
        var list = new List<PlayerStatus>();
        if (playerParty != null)
            foreach (var p in playerParty.Members) if (p.IsAlive) list.Add(p as PlayerStatus);
        return list;
    }

    /// <summary>Địch đang nhắm. Auto-clamp index để không trả null khi còn địch.</summary>
    public EnemyStatus GetEnemyTarget()
    {
        var alive = AliveEnemies();
        if (alive.Count == 0) return null;
        EnemyIndex = Mathf.Clamp(EnemyIndex, 0, alive.Count - 1);
        return alive[EnemyIndex];
    }

    /// <summary>Đồng minh đang nhắm. Auto-clamp index.</summary>
    public PlayerStatus GetAllyTarget()
    {
        var alive = AliveAllies();
        if (alive.Count == 0) return null;
        AllyIndex = Mathf.Clamp(AllyIndex, 0, alive.Count - 1);
        return alive[AllyIndex];
    }

    /// <summary>Đổi mục tiêu địch theo hướng dir (±1, vòng tròn). Trả target mới hoặc null.</summary>
    public EnemyStatus CycleEnemy(int dir)
    {
        var alive = AliveEnemies();
        if (alive.Count == 0) return null;
        EnemyIndex = (EnemyIndex + dir + alive.Count) % alive.Count;
        return alive[EnemyIndex];
    }

    /// <summary>Đổi mục tiêu đồng minh theo hướng dir (±1, vòng tròn). Trả target mới hoặc null.</summary>
    public PlayerStatus CycleAlly(int dir)
    {
        var alive = AliveAllies();
        if (alive.Count == 0) return null;
        AllyIndex = (AllyIndex + dir + alive.Count) % alive.Count;
        return alive[AllyIndex];
    }

    /// <summary>Index (trong danh sách ally còn sống) của đồng minh HP thấp nhất theo tỉ lệ.</summary>
    public int IndexOfLowestHpAlly()
    {
        var alive = AliveAllies();
        if (alive.Count == 0) return 0;

        int best = 0;
        float bestRatio = float.MaxValue;
        for (int i = 0; i < alive.Count; i++)
        {
            float ratio = alive[i].MaxHP > 0 ? (float)alive[i].currentHP / alive[i].MaxHP : 1f;
            if (ratio < bestRatio) { bestRatio = ratio; best = i; }
        }
        return best;
    }

    /// <summary>Ghi EnemyChosenPlayerIndex theo player mà enemy AI chọn nhắm.</summary>
    public void SetEnemyChosenPlayer(PlayerStatus target)
    {
        if (target == null) return;
        var alive = AliveAllies();
        for (int i = 0; i < alive.Count; i++)
            if (alive[i] == target) { EnemyChosenPlayerIndex = i; break; }
    }

    /// <summary>
    /// Tìm index địch còn sống gần điểm tap trên màn hình (mobile). Trả -1 nếu không thấy.
    /// Pass 1: trúng sprite bounds. Pass 2: gần nhất trong 1.5 world unit.
    /// </summary>
    public int FindAliveEnemyIndexNearScreenPoint(Camera cam, Vector2 screenPos)
    {
        var alive = AliveEnemies();
        if (alive.Count == 0 || cam == null) return -1;

        float depth = Mathf.Abs(cam.transform.position.z);
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));

        for (int i = 0; i < alive.Count; i++)
        {
            var model = alive[i]?.SpawnedModel;
            if (model == null) continue;
            foreach (var sr in model.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr == null || !sr.enabled) continue;
                Bounds b = sr.bounds;
                if (worldPos.x >= b.min.x && worldPos.x <= b.max.x &&
                    worldPos.y >= b.min.y && worldPos.y <= b.max.y)
                    return i;
            }
        }

        float bestDist = 1.5f;
        int bestIdx = -1;
        for (int i = 0; i < alive.Count; i++)
        {
            var model = alive[i]?.SpawnedModel;
            if (model == null) continue;
            float dist = Vector2.Distance(
                new Vector2(worldPos.x, worldPos.y),
                new Vector2(model.transform.position.x, model.transform.position.y));
            if (dist < bestDist) { bestDist = dist; bestIdx = i; }
        }
        return bestIdx;
    }
}
