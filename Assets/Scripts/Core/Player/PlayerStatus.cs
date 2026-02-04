using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerStatus : Status
{
    // --- AP SYSTEM ---
    public int MaxAP => 100; // Fixed max AP for simplicity
    public int currentAP;
    public bool CanUseAP(int cost) => currentAP >= cost;
    public void UseAP(int cost) => currentAP -= cost;
    public void RestoreAP(int amount) => currentAP = Mathf.Min(MaxAP, currentAP + amount);
    public void RestoreFullAP() => currentAP = MaxAP;

    // --- PARRY SYSTEM ---
    private bool canParry = false;
    public void EnableParry() => canParry = true;
    public bool ConsumeParry()
    {
        if (canParry)
        {
            canParry = false;
            return true;
        }
        return false;
    }

    // --- EXP SYSTEM ---
    public int currentExp = 0;
    public int expToNextLevel => level * 100; // Simple formula
    public void GainExp(int amount)
    {
        currentExp += amount;
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;
        HealFull();
        RestoreFullAP();
        Debug.Log($"{entityName} leveled up to {level}!");
    }

    // --- CONSTRUCTOR ---
    public PlayerStatus(string name, int baseHP, int baseAtk, int baseDef, int baseSpd)
        : base(name, baseHP, baseAtk, baseDef, baseSpd)
    {
        currentAP = MaxAP;
    }
}