using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerStatus : Status
{
    // ================= AP =================
    public int MaxAP { get; private set; }
    public int currentAP;

    public bool CanUseAP(int cost) => currentAP >= cost;
    public void UseAP(int cost) => currentAP -= cost;
    public void RestoreFullAP() => currentAP = MaxAP;

    // ================= SKILL =================
    [SerializeField] private List<PlayerAttackData> skills = new();
    public IReadOnlyList<PlayerAttackData> Skills => skills;

    public PlayerAttackData GetSkillByIndex(int index)
    {
        if (index < 0 || index >= skills.Count)
            return null;
        return skills[index];
    }

    public void AddSkill(PlayerAttackData skill)
    {
        if (skill != null && !skills.Contains(skill))
            skills.Add(skill);
    }

    // ================= EXP =================
    public int currentExp = 0;
    public int expToNextLevel => level * 100;

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

    // ================= CTOR =================
    public PlayerStatus(
        string name,
        int baseHP,
        int baseAtk,
        int baseDef,
        int baseSpd,
        int maxAP = 100
    ) : base(name, baseHP, baseAtk, baseDef, baseSpd)
    {
        MaxAP = maxAP;
        currentAP = MaxAP;
    }
}
