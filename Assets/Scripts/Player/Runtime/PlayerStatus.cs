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

    public int SkillCount => skills.Count;

    public PlayerAttackData BasicAttack { get; private set; }



    public void AddSkill(PlayerAttackData attack)
    {
        if (attack == null)
        {
            Debug.LogError("AddSkill NULL");
            return;
        }

        if (skills.Contains(attack))
            return;


        skills.Add(attack);


        Debug.Log(
        $"AddSkill: {attack.attackName} | AP:{attack.apCost}");


        // Detect Basic Attack
        if (attack.apCost == 0)
        {
            if (BasicAttack == null)
            {
                BasicAttack = attack;

                Debug.Log(
                $"BasicAttack SET: {attack.attackName}");
            }
        }
    }



    public PlayerAttackData GetSkillByIndex(int index)
    {
        if (index < 0 || index >= skills.Count)
            return null;

        return skills[index];
    }



    // ================= INIT =================

    public void InitializeSkills(List<PlayerAttackData> list)
    {
        skills.Clear();

        BasicAttack = null;


        foreach (var atk in list)
        {
            AddSkill(atk);
        }


        if (BasicAttack == null)
        {
            Debug.LogError(
            $"{entityName} has NO BASIC ATTACK");
        }
    }



    // ================= PARRY SYSTEM =================

    private bool parryWindowOpen = false;

    private bool parryRequested = false;

    public bool WasParried { get; private set; }



    public void OpenParryWindow()
    {
        parryWindowOpen = true;

        parryRequested = false;

        WasParried = false;
    }



    public void CloseParryWindow()
    {
        parryWindowOpen = false;
    }



    public void RequestParry()
    {
        if (!parryWindowOpen)
            return;

        parryRequested = true;
    }



    public bool ConsumeParry()
    {
        if (!parryWindowOpen)
            return false;

        if (!parryRequested)
            return false;

        parryWindowOpen = false;

        parryRequested = false;

        WasParried = true;

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

        Debug.Log(
        $"{entityName} level up {level}");
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