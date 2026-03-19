using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public abstract class Status
{
    [NonSerialized] public int LastTargetSlot = 0;
    [NonSerialized] public float NextTurnTime;
    [NonSerialized] public int BattleSlotId = -1;
    [NonSerialized] public PartyType PartyType;
    [NonSerialized] public GameObject SpawnedModel;

    public event Action<Status> OnDeath;
    public event Action<Status> OnRevive;

    public string entityName;
    public int level = 1;

    protected int baseHP;
    protected int baseAtk;
    protected int baseDef;
    protected int baseSpd;

    protected int hpGrowth;
    protected int atkGrowth;
    protected int defGrowth;
    protected int spdGrowth;

    public int MaxHP => baseHP + (hpGrowth * (level - 1));
    public int Atk   => baseAtk + (atkGrowth * (level - 1));
    public int Def   => baseDef + (defGrowth * (level - 1));
    public int Spd   => baseSpd + (spdGrowth * (level - 1));

    public int currentHP;
    public bool IsAlive => currentHP > 0;

    protected Status(string name, int hp, int atk, int def, int spd)
    {
        entityName = name;
        baseHP  = hp;
        baseAtk = atk;
        baseDef = def;
        baseSpd = spd;

        hpGrowth  = Mathf.RoundToInt(hp  * 0.2f);
        atkGrowth = Mathf.Max(1, Mathf.RoundToInt(atk * 0.15f));
        defGrowth = Mathf.Max(1, Mathf.RoundToInt(def * 0.1f));
        spdGrowth = 1;

        level     = 1;
        currentHP = MaxHP;
    }

    /// <summary>
    /// Gọi khi bắt đầu mỗi trận. Reset timeline + clear tất cả buff/debuff cũ.
    /// HP của player KHÔNG được reset ở đây — đã được load từ save data trước đó.
    /// EnemyStatus override để tự reset HP về Max.
    /// </summary>
    public virtual void ResetForBattle(float baseDelay)
    {
        NextTurnTime = baseDelay / Mathf.Max(1, Spd);
        ClearAllEffects();
    }

    public virtual void SetLevel(int targetLevel)
    {
        if (targetLevel < 1) return;
        if (level == targetLevel) return;
        level = targetLevel;
        HealFull();
        Debug.Log($"{entityName} set to level {level}, HP restored to {MaxHP}");
    }

    public void TakeDamage(Status attacker, int rawDamage)
    {
        if (!IsAlive) return;
        if (attacker != null && !attacker.IsAlive) return;

        int finalDmg = Mathf.Max(1, rawDamage - Def);
        currentHP -= finalDmg;

        if (SpawnedModel != null)
        {
            var visual = SpawnedModel.GetComponent<UnitVisual>();
            if (visual != null) visual.PlayHit();
        }

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    /// <summary>
    /// Poison damage bỏ qua DEF (true damage per turn).
    /// </summary>
    public void TakePoisonDamage(int damage)
    {
        if (!IsAlive) return;
        currentHP -= Mathf.Max(1, damage);
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"{entityName} died");
        if (SpawnedModel != null)
        {
            var visual = SpawnedModel.GetComponent<UnitVisual>();
            if (visual != null) visual.PlayDie();
        }
        OnDeath?.Invoke(this);
        EventManager.Publish(GameEvent.UnitDied, this);
    }

    private readonly List<StatusEffect> activeEffects = new List<StatusEffect>();

    /// <summary>Expose để BattleManager xử lý Poison/Stun mỗi đầu lượt.</summary>
    public IReadOnlyList<StatusEffect> GetActiveEffects() => activeEffects;

    public virtual void ApplyStatusEffect(StatusEffect effect)
    {
        if (effect == null) return;

        // Clone để tránh ghi đè duration lên ScriptableObject gốc
        // khi UpdateEffectDurations() giảm duration-- trên bản sao.
        StatusEffect clone = effect.Clone();
        activeEffects.Add(clone);

        switch (clone.effectType)
        {
            case StatusEffectType.BuffHeal:
                Heal(clone.value);
                break;
            case StatusEffectType.BuffAtk:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(baseAtk * (clone.value / 100f)));
                baseAtk += clone.appliedValue;
                break;
            case StatusEffectType.BuffDef:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(baseDef * (clone.value / 100f)));
                baseDef += clone.appliedValue;
                break;
            case StatusEffectType.BuffSpd:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(baseSpd * (clone.value / 100f)));
                baseSpd += clone.appliedValue;
                break;
            case StatusEffectType.BuffHP:
                clone.appliedValue = clone.value;
                baseHP += clone.appliedValue;
                break;
            case StatusEffectType.DebuffAtk:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(baseAtk * (clone.value / 100f)));
                baseAtk -= clone.appliedValue;
                break;
            case StatusEffectType.DebuffDef:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(baseDef * (clone.value / 100f)));
                baseDef -= clone.appliedValue;
                break;
            case StatusEffectType.DebuffSpd:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(baseSpd * (clone.value / 100f)));
                baseSpd -= clone.appliedValue;
                break;
            case StatusEffectType.Poison:
            case StatusEffectType.Stun:
                // Xử lý mỗi lượt bởi BattleManager.ProcessTurnEffects.
                break;
        }

        Debug.Log($"[EFFECT] {entityName} gained {clone.effectName} (dur:{clone.duration}, val:{clone.value})");
    }

    public void UpdateEffectDurations()
    {
        foreach (var effect in activeEffects)
            if (effect.duration > 0)
                effect.duration--;

        RemoveExpiredEffects();
    }

    private void RemoveExpiredEffects()
    {
        var toRemove = new List<StatusEffect>();
        foreach (var effect in activeEffects)
            if (effect.duration <= 0) { toRemove.Add(effect); UndoStatusEffect(effect); }

        foreach (var effect in toRemove)
        {
            activeEffects.Remove(effect);
            Debug.Log($"[EFFECT EXPIRED] {entityName} lost {effect.effectName}");
        }
    }

    private void UndoStatusEffect(StatusEffect effect)
    {
        switch (effect.effectType)
        {
            // Buff đã cộng vào khi Apply → undo phải TRỪ đi.
            case StatusEffectType.BuffAtk:   baseAtk -= effect.appliedValue; break;
            case StatusEffectType.BuffDef:   baseDef -= effect.appliedValue; break;
            case StatusEffectType.BuffSpd:   baseSpd -= effect.appliedValue; break;
            case StatusEffectType.BuffHP:    baseHP  -= effect.appliedValue; break;
            // Debuff đã trừ khi Apply → undo phải CỘNG lại.
            case StatusEffectType.DebuffAtk: baseAtk += effect.appliedValue; break;
            case StatusEffectType.DebuffDef: baseDef += effect.appliedValue; break;
            case StatusEffectType.DebuffSpd: baseSpd += effect.appliedValue; break;
            case StatusEffectType.Poison:
            case StatusEffectType.Stun:
            case StatusEffectType.BuffHeal:
                // Không cần undo: BuffHeal là immediate, Poison/Stun không thay đổi stat.
                break;
        }
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;
        currentHP = Mathf.Min(MaxHP, currentHP + amount);
    }

    public void HealFull()
    {
        if (!IsAlive) return; // Không hồi sinh unit đã chết — dùng Revive() thay thế
        currentHP = MaxHP;
    }

    /// <summary>
    /// Xóa toàn bộ status effects đang active và undo stat changes.
    /// Gọi khi kết thúc trận hoặc reset state.
    /// </summary>
    public void ClearAllEffects()
    {
        foreach (var effect in activeEffects)
            UndoStatusEffect(effect);
        activeEffects.Clear();
    }

    public virtual void Revive(int hpAmount)
    {
        if (IsAlive) return;
        currentHP = Mathf.Clamp(hpAmount, 1, MaxHP);
        OnRevive?.Invoke(this);
        EventManager.Publish(GameEvent.UnitRevived, this);
        Debug.Log($"{entityName} revived with {currentHP} HP");
    }
}
