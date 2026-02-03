using UnityEngine;

[System.Serializable]
public class PlayerStatus : Status
{
    // ===== PROGRESSION =====
    public int currentExp = 0;
    public int MaxExpRequired => 100 * level;

    // ===== ACTION POINT =====
    public int currentAP = 0;
    public int maxAP = 5;

    public PlayerStatus(string name)
        : base(name, hp: 100, atk: 15, def: 5, spd: 10)
    {
        level = 1;
        currentExp = 0;
        currentAP = 0;
    }

    // ===== EXP =====
    public void GainExp(int amount)
    {
        currentExp += amount;
        while (currentExp >= MaxExpRequired)
            LevelUp();
    }

    private void LevelUp()
    {
        currentExp -= MaxExpRequired;
        level++;
        HealFull();
    }

    // ===== AP =====
    public void GainAP(int amount)
    {
        currentAP = Mathf.Min(currentAP + amount, maxAP);
    }

    public bool CanUseAP(int cost)
    {
        return currentAP >= cost;
    }

    public void UseAP(int cost)
    {
        currentAP -= cost;
        if (currentAP < 0) currentAP = 0;
    }
}