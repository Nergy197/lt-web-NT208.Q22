using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public abstract class Status
{
    // ================= BATTLE RUNTIME (DO NOT SAVE) =================
    [NonSerialized] public int LastTargetSlot = 0; // nhớ vị trí target cuối cùng
    [NonSerialized] public float NextTurnTime;      // speed-based timeline
    [NonSerialized] public int BattleSlotId = -1;   // slot cố định trong battle
    [NonSerialized] public PartyType PartyType;     // Player / Enemy
    [NonSerialized] public GameObject SpawnedModel; // Model đã được spawn ra scene

    // ================= EVENTS =================

    public event Action<Status> OnDeath;
    public event Action<Status> OnRevive;

    // ================= IDENTITY =================

    public string entityName;
    public int level = 1;

    // ================= BASE STATS =================

    protected int baseHP;
    protected int baseAtk;
    protected int baseDef;
    protected int baseSpd;

    // ================= GROWTH STATS =================

    protected int hpGrowth;
    protected int atkGrowth;
    protected int defGrowth;
    protected int spdGrowth;

    // ================= REAL-TIME CALCULATION =================

    public int MaxHP => baseHP + (hpGrowth * (level - 1));
    public int Atk => baseAtk + (atkGrowth * (level - 1));
    public int Def => baseDef + (defGrowth * (level - 1));
    public int Spd => baseSpd + (spdGrowth * (level - 1));

    // ================= DYNAMIC STATE =================

    public int currentHP;
    public bool IsAlive => currentHP > 0;

    // ================= CONSTRUCTOR =================

    protected Status(string name, int hp, int atk, int def, int spd)
    {
        entityName = name;

        baseHP = hp;
        baseAtk = atk;
        baseDef = def;
        baseSpd = spd;

        // Growth auto-calc
        hpGrowth = Mathf.RoundToInt(hp * 0.2f);
        atkGrowth = Mathf.Max(1, Mathf.RoundToInt(atk * 0.15f));
        defGrowth = Mathf.Max(1, Mathf.RoundToInt(def * 0.1f));
        spdGrowth = 1;

        level = 1;
        currentHP = MaxHP;
    }

    // ================= BATTLE LIFECYCLE =================

    /// <summary>
    /// Gọi khi bắt đầu mỗi trận battle.
    /// BUG FIX: Không reset currentHP ở đây — HP của Player đã được load từ DB.
    /// Chỉ reset NextTurnTime phục vụ timeline.
    /// EnemyStatus sẽ override để reset HP về Max.
    /// </summary>
    public virtual void ResetForBattle(float baseDelay)
    {
        NextTurnTime = baseDelay / Mathf.Max(1, Spd);
    }

    // ================= LEVEL =================

    public virtual void SetLevel(int targetLevel)
    {
        if (targetLevel < 1) return;
        if (level == targetLevel) return;

        level = targetLevel;
        HealFull();

        Debug.Log($"{entityName} set to level {level}, HP restored to {MaxHP}");
    }

    // ================= COMBAT =================

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
    /// BUG FIX: Poison damage bất qua Def (true damage per turn).
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

    // ================= STATUS EFFECT =================

    private readonly List<StatusEffect> activeEffects = new List<StatusEffect>();

    /// <summary>
    /// BUG FIX: Expose active effects để BattleManager xử lý Poison/Stun mỗi lượt.
    /// </summary>
    public IReadOnlyList<StatusEffect> GetActiveEffects() => activeEffects;

    public virtual void ApplyStatusEffect(StatusEffect effect)
    {
        if (effect == null) return;

        // Clone để tránh ghi đè duration lên ScriptableObject gốc
        // (duration-- trong UpdateEffectDurations sẽ chỉ ảnh hưởng bản sao)
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
                // xử lý theo turn trong BattleManager.ProcessTurnEffects
                break;

            case StatusEffectType.Stun:
                // xử lý skip turn trong BattleManager.ProcessTurnEffects
                break;
        }

        Debug.Log($"[EFFECT] {entityName} gained {clone.effectName} " +
                  $"(dur:{clone.duration}, val:{clone.value})");
    }

    public void UpdateEffectDurations()
    {
        // Decrease duration for all active effects
        foreach (var effect in activeEffects)
        {
            if (effect.duration > 0) // Only decrease positive durations (-1 is permanent)
                effect.duration--;
        }

        // Remove expired effects and undo their stat changes
        RemoveExpiredEffects();
    }

    private void RemoveExpiredEffects()
    {
        List<StatusEffect> toRemove = new List<StatusEffect>();

        foreach (var effect in activeEffects)
        {
            if (effect.duration <= 0)
            {
                toRemove.Add(effect);
                UndoStatusEffect(effect);
            }
        }

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
            case StatusEffectType.BuffAtk:
                baseAtk -= effect.appliedValue;
                break;

            case StatusEffectType.BuffDef:
                baseDef -= effect.appliedValue;
                break;

            case StatusEffectType.BuffSpd:
                baseSpd -= effect.appliedValue;
                break;

            case StatusEffectType.BuffHP:
                baseHP -= effect.appliedValue;
                break;

            case StatusEffectType.DebuffAtk:
                baseAtk += effect.appliedValue;
                break;

            case StatusEffectType.DebuffDef:
                baseDef += effect.appliedValue;
                break;

            case StatusEffectType.DebuffSpd:
                baseSpd += effect.appliedValue;
                break;

            case StatusEffectType.Poison:
            case StatusEffectType.Stun:
            case StatusEffectType.BuffHeal:
                // BuffHeal is immediate, no undo needed
                // Poison and Stun handled by BattleManager
                break;
        }
    }

    // ================= HEAL / REVIVE =================

    public void Heal(int amount)
    {
        if (!IsAlive) return;

        currentHP = Mathf.Min(MaxHP, currentHP + amount);
    }

    public void HealFull()
    {
        currentHP = MaxHP;
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
