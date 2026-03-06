using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerStatus : Status
{
    // ================= AP SYSTEM =================

    public GameObject battlePrefab; // Set bởi PlayerData.CreateStatus()

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

        // Basic Attack (apCost == 0): chỉ lưu vào BasicAttack, KHÔNG thêm vào skills[]
        // → skill menu sẽ không hiển thị basic attack
        if (attack.apCost == 0)
        {
            if (BasicAttack == null)
            {
                BasicAttack = attack;
                Debug.Log($"BasicAttack SET: {attack.attackName}");
            }
            else
            {
                Debug.LogWarning($"BasicAttack đã có ({BasicAttack.attackName}), bỏ qua: {attack.attackName}");
            }
            return; // Không thêm vào skills[]
        }

        if (skills.Contains(attack))
            return;

        skills.Add(attack);

        Debug.Log($"AddSkill: {attack.attackName} | AP:{attack.apCost} | Index:{skills.Count - 1}");
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

    private float parryCooldownEndTime = 0f;

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
        // Đang bị phạt không cho ấn
        if (Time.time < parryCooldownEndTime)
            return;

        // Cửa sổ chưa mở mà cố ấn -> Phạt penalty 1 giây
        if (!parryWindowOpen)
        {
            parryCooldownEndTime = Time.time + 1.0f;
            Debug.Log($"[PARRY PENALTY] {entityName} bấm hụt/spam! Bị khóa parry 1 giây.");
            return;
        }

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

        if (SpawnedModel != null)
        {
            var visual = SpawnedModel.GetComponent<UnitVisual>();
            if (visual != null) visual.PlayParry();
        }

        return true;
    }



    // ================= EXP SYSTEM =================

    public int currentExp = 0;

    // Công thức EXP cần lên cấp — dạng cong để tránh grind nhàm chán
    // Level 1→10  VD: Lv1=100, Lv5=559, Lv10=1581, Lv20=4472, Lv30=8219
    public int expToNextLevel => Mathf.RoundToInt(100 * Mathf.Pow(level, 1.5f));



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

        Debug.Log($"{entityName} level up {level}");

        EventManager.Publish(GameEvent.UnitLevelUp, this);
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