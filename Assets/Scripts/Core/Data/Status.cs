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

    // ── Buff/Debuff layer ──────────────────────────────────────────────
    // Bonus tách RIÊNG khỏi base stat: buff/debuff chỉ cộng/trừ vào bonus*,
    // không bao giờ làm hỏng base. Mặc định 0 (battle-only, không persist).
    [NonSerialized] private int bonusMaxHP;
    [NonSerialized] private int bonusAtk;
    [NonSerialized] private int bonusDef;
    [NonSerialized] private int bonusSpd;

    // Giá trị "nền" theo level, CHƯA tính buff — dùng làm gốc % cho buff/debuff.
    private int RawMaxHP => baseHP  + (hpGrowth  * (level - 1));
    private int RawAtk   => baseAtk + (atkGrowth * (level - 1));
    private int RawDef   => baseDef + (defGrowth * (level - 1));
    private int RawSpd   => baseSpd + (spdGrowth * (level - 1));

    public int MaxHP => Mathf.Max(1, RawMaxHP + bonusMaxHP);
    public int Atk   => Mathf.Max(0, RawAtk   + bonusAtk);
    public int Def   => Mathf.Max(0, RawDef   + bonusDef);
    public int Spd   => Mathf.Max(1, RawSpd   + bonusSpd);

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
            if (visual != null)
            {
                visual.SetAnimatorSpeed(1f);
                visual.PlayHit();
            }
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

        // Undo tất cả buff/debuff để base stat không bị corrupt vĩnh viễn
        ClearAllEffects();

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

        // % của buff/debuff luôn tính trên giá trị NỀN theo level (Raw*), KHÔNG trên giá trị
        // đã buff → tránh cộng dồn lệch khi stack nhiều buff cùng loại.
        switch (clone.effectType)
        {
            case StatusEffectType.BuffHeal:
                Heal(clone.value);
                break;
            case StatusEffectType.BuffAtk:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(RawAtk * (clone.value / 100f)));
                bonusAtk += clone.appliedValue;
                break;
            case StatusEffectType.BuffDef:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(RawDef * (clone.value / 100f)));
                bonusDef += clone.appliedValue;
                break;
            case StatusEffectType.BuffSpd:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(RawSpd * (clone.value / 100f)));
                bonusSpd += clone.appliedValue;
                break;
            case StatusEffectType.BuffHP:
                clone.appliedValue = clone.value;
                bonusMaxHP += clone.appliedValue;
                break;
            case StatusEffectType.DebuffAtk:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(RawAtk * (clone.value / 100f)));
                bonusAtk -= clone.appliedValue;
                break;
            case StatusEffectType.DebuffDef:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(RawDef * (clone.value / 100f)));
                bonusDef -= clone.appliedValue;
                break;
            case StatusEffectType.DebuffSpd:
                clone.appliedValue = Mathf.Max(1, Mathf.RoundToInt(RawSpd * (clone.value / 100f)));
                bonusSpd -= clone.appliedValue;
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
            if (effect.duration == 0) { toRemove.Add(effect); UndoStatusEffect(effect); }

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
            // Buff đã cộng vào bonus khi Apply → undo phải TRỪ đi.
            case StatusEffectType.BuffAtk:   bonusAtk -= effect.appliedValue; break;
            case StatusEffectType.BuffDef:   bonusDef -= effect.appliedValue; break;
            case StatusEffectType.BuffSpd:   bonusSpd -= effect.appliedValue; break;
            case StatusEffectType.BuffHP:
                bonusMaxHP -= effect.appliedValue;
                // MaxHP vừa giảm → kẹp currentHP để không vượt quá.
                if (currentHP > MaxHP) currentHP = MaxHP;
                break;
            // Debuff đã trừ khỏi bonus khi Apply → undo phải CỘNG lại.
            case StatusEffectType.DebuffAtk: bonusAtk += effect.appliedValue; break;
            case StatusEffectType.DebuffDef: bonusDef += effect.appliedValue; break;
            case StatusEffectType.DebuffSpd: bonusSpd += effect.appliedValue; break;
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

        // An toàn: sau khi gỡ hết buff, ép bonus về 0 và kẹp currentHP.
        bonusMaxHP = bonusAtk = bonusDef = bonusSpd = 0;
        if (currentHP > MaxHP) currentHP = MaxHP;
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
