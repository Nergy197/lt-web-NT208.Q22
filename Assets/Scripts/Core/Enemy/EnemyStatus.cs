using UnityEngine;

[System.Serializable]
public class EnemyStatus : Status
{
    // ===== EXP REWARD =====
    // EXP cơ bản ở level 1
    [SerializeField] private int baseExpReward = 20;

    // ===== CONSTRUCTOR =====
    public EnemyStatus(string name, int baseHP, int baseAtk, int baseDef, int baseSpd)
        : base(name, baseHP, baseAtk, baseDef, baseSpd)
    {
    }

    // ===== SYNC LEVEL THEO MAP =====
    public void SyncToLevel(int targetLevel)
    {
        // dùng logic chuẩn của Status (set level + full HP)
        SetLevel(targetLevel);

        Debug.Log($"Enemy [{entityName}] synced to Map Level {level}");
    }

    // ===== EXP REWARD =====
    public int GetExpReward()
    {
        // EXP scale theo level map
        // Lv1: 20
        // Lv5: ~40
        // Lv10: ~60
        return Mathf.RoundToInt(baseExpReward * (1f + level * 0.2f));
    }
}
