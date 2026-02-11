using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerStatus : Status
{
    // ================= AP SYSTEM =================
    public int MaxAP { get; private set; }
    public int currentAP;

    public bool CanUseAP(int cost) => currentAP >= cost;
    public void UseAP(int cost) => currentAP -= cost;
    public void RestoreAP(int amount) =>
        currentAP = Mathf.Min(MaxAP, currentAP + amount);
    public void RestoreFullAP() => currentAP = MaxAP;

    // ================= SKILL SYSTEM =================
    private readonly List<PlayerAttackData> skills = new();

    public IReadOnlyList<PlayerAttackData> Skills => skills;
    public PlayerAttackData BasicAttack { get; private set; }

    /// <summary>
    /// Add skill cho player.
    /// Skill có apCost == 0 sẽ tự động làm BasicAttack (chỉ 1 skill).
    /// </summary>
    public void AddSkill(PlayerAttackData attack)
    {
        if (attack == null) return;
        if (skills.Contains(attack)) return;

        skills.Add(attack);

        if (attack.apCost == 0 && BasicAttack == null)
        {
            BasicAttack = attack;
            Debug.Log($"[PlayerStatus] BasicAttack set to {attack.attackName}");
        }
    }

    public PlayerAttackData GetSkillByIndex(int index)
    {
        if (index < 0 || index >= skills.Count)
            return null;

        return skills[index];
    }

    // ================= PARRY SYSTEM =================
    private bool canParry = false;
    public void EnableParry() => canParry = true;

    public bool ConsumeParry()
    {
        if (!canParry) return false;
        canParry = false;
        return true;
    }

    // ================= EXP SYSTEM =================
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

    // ================= CONSTRUCTOR =================
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
