using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerStatus : Status
{
    public GameObject battlePrefab;

    public int MaxAP { get; private set; }
    public int currentAP;

    public bool CanUseAP(int cost) => currentAP >= cost;
    public void UseAP(int cost)    => currentAP -= cost;
    public void RestoreAP(int amount) => currentAP = Mathf.Min(MaxAP, currentAP + amount);
    public void RestoreFullAP()    => currentAP = MaxAP;

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

        // apCost == 0 → Basic Attack: hành động luôn khả dụng, không hiển thị trong skill menu.
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
            return;
        }

        if (skills.Contains(attack)) return;
        skills.Add(attack);
        Debug.Log($"AddSkill: {attack.attackName} | AP:{attack.apCost} | Index:{skills.Count - 1}");
    }

    public PlayerAttackData GetSkillByIndex(int index)
    {
        if (index < 0 || index >= skills.Count) return null;
        return skills[index];
    }

    public void InitializeSkills(List<PlayerAttackData> list)
    {
        skills.Clear();
        BasicAttack = null;
        foreach (var atk in list) AddSkill(atk);
        if (BasicAttack == null)
            Debug.LogError($"{entityName} has NO BASIC ATTACK");
    }

    private bool parryWindowOpen    = false;
    private bool parryRequested     = false;
    private float parryCooldownEndTime = 0f;
    public bool WasParried { get; private set; }

    public void OpenParryWindow()
    {
        parryWindowOpen = true;
        parryRequested  = false;
        WasParried      = false;
    }

    public void CloseParryWindow()
    {
        parryWindowOpen = false;
    }

    public void RequestParry()
    {
        if (Time.time < parryCooldownEndTime) return;

        if (!parryWindowOpen)
        {
            // Bấm parry khi cửa sổ chưa mở → bị phạt không được parry trong 1 giây.
            parryCooldownEndTime = Time.time + 1.0f;
            Debug.Log($"[PARRY PENALTY] {entityName} bấm hụt! Bị khóa parry 1 giây.");
            return;
        }

        parryRequested = true;
    }

    /// <summary>
    /// Tiêu thụ yêu cầu parry nếu hợp lệ. Trả về true nếu parry thành công.
    /// Tự đóng parry window và set WasParried để trigger animation.
    /// </summary>
    public bool ConsumeParry()
    {
        if (!parryWindowOpen || !parryRequested) return false;

        parryWindowOpen = false;
        parryRequested  = false;
        WasParried      = true;

        if (SpawnedModel != null)
        {
            var visual = SpawnedModel.GetComponent<UnitVisual>();
            if (visual != null) visual.PlayParry();
        }

        return true;
    }

    public int currentExp = 0;

    /// <summary>
    /// EXP cần để lên cấp tiếp theo. Dùng công thức cong (level^1.5) để tránh grind nhàm chán.
    /// Ví dụ: Lv1=100, Lv5=559, Lv10=1581, Lv20=4472.
    /// </summary>
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
        Debug.Log($"{entityName} level up → {level}");
        EventManager.Publish(GameEvent.UnitLevelUp, this);
    }

    public PlayerStatus(
        string name,
        int baseHP,
        int baseAtk,
        int baseDef,
        int baseSpd,
        int maxAP = 100
    ) : base(name, baseHP, baseAtk, baseDef, baseSpd)
    {
        MaxAP     = maxAP;
        currentAP = MaxAP;
    }
}