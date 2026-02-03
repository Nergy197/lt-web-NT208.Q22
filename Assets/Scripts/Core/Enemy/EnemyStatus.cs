using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class EnemyStatus : Status
{
    [SerializeField] private int baseExpReward = 20;

    [SerializeField] private bool isAggressive = true; 

    public EnemyStatus(string name, int baseHP, int baseAtk, int baseDef, int baseSpd)
        : base(name, baseHP, baseAtk, baseDef, baseSpd)
    {
    }

    public void SyncToLevel(int targetLevel)
    {
        SetLevel(targetLevel);
        Debug.Log($"Enemy [{entityName}] synced to Map Level {level}");
    }

    public int GetExpReward()
    {
        return Mathf.RoundToInt(baseExpReward * (1f + level * 0.2f));
    }

    // ===== CHỌN MỤC TIÊU =====
    // Hàm này nhận vào danh sách tất cả Hero (Status) đang có trong trận
    public Status PickTarget(List<Status> heroParty)
    {
        // 1. Lọc ra những Hero còn sống (HP > 0)
        var aliveHeroes = heroParty.Where(h => h.currentHP > 0).ToList();

        // Nếu không còn ai sống -> trả về null (Thắng trận)
        if (aliveHeroes.Count == 0) return null;

        // 2. Logic chọn mục tiêu
        if (isAggressive)
        {
            // AGGRESSIVE: Sắp xếp theo HP tăng dần -> Lấy người đầu tiên (Thấp máu nhất)
            return aliveHeroes.OrderBy(h => h.currentHP).First();
        }
        else
        {
            // NORMAL: Chọn ngẫu nhiên (nếu muốn quái khác đánh lung tung)
            int randomIndex = Random.Range(0, aliveHeroes.Count);
            return aliveHeroes[randomIndex];
        }
    }
}