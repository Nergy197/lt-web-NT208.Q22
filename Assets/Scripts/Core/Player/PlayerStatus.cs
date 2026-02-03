using UnityEngine;

[System.Serializable]
public class PlayerStatus : Status
{
    [Header("Progression")]
    public int currentExp = 0;

    // Lv1 cần 100, Lv2 cần 200...
    public int MaxExpRequired => 100 * level;

    public PlayerStatus(string name) 
        // Base Stats cố định cho Player
        : base(name, hp: 100, atk: 15, def: 5, spd: 10)
    {
        this.level = 1;
        this.currentExp = 0;
    }

    public void GainExp(int amount)
    {
        currentExp += amount;
        Debug.Log($"Player gained {amount} EXP. Total: {currentExp}/{MaxExpRequired}");

        while (currentExp >= MaxExpRequired)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentExp -= MaxExpRequired;
        level++;
        HealFull();
        Debug.Log($"<color=yellow>LEVEL UP!</color> Player Lv.{level}. MaxHP: {MaxHP}");
    }

    public float GetExpRatio()
    {
        if (MaxExpRequired == 0) return 0;
        return (float)currentExp / MaxExpRequired;
    }

    public void LoadData(int savedLevel, int savedExp)
    {
        // Sử dụng hàm SetLevel của cha để đảm bảo đồng bộ máu
        base.SetLevel(savedLevel);
        this.currentExp = savedExp;
    }
}