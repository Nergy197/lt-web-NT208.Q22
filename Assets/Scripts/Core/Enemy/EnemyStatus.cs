using UnityEngine;

[System.Serializable]
public class EnemyStatus : Status
{
    // Phần thưởng EXP cơ bản (sẽ nhân với level)
    private int baseExpReward = 20;

    // Base stats của Enemy
    public EnemyStatus(string name, int baseHP, int baseAtk, int baseDef, int baseSpd)
        // SỬA LỖI: Chỉ truyền 5 tham số khớp với constructor của Status
        : base(name, baseHP, baseAtk, baseDef, baseSpd) 
    {
    }

    // === HÀM ĐỒNG BỘ LEVEL ===
    public void SyncToLevel(int targetLevel)
    {
        // Gọi hàm của lớp cha để set level và hồi máu
        base.SetLevel(targetLevel);
        
        UnityEngine.Debug.Log($"Enemy {entityName} initialized at Map Level {level}.");
    }
    
    // === TÍNH TOÁN EXP THƯỞNG ===
    public int GetExpReward()
    {
        // Level map càng cao, exp càng nhiều
        return (int)(baseExpReward * (1 + (level * 0.2f)));
    }
}