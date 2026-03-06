using UnityEngine;

[System.Serializable]
public class StatusEffect
{
    public string effectName;
    public StatusEffectType effectType;
    public int value; // % đối với Atk, Def, Spd, flat value đối với HP, Heal, Poison
    [System.NonSerialized] public int appliedValue; // dùng để nhớ số stat thực tế đã cộng/trừ để undo
    public int duration; // số turn (-1 = vĩnh viễn)

    public StatusEffect(string name, StatusEffectType type, int val, int dur = -1)
    {
        effectName = name;
        effectType = type;
        value = val;
        duration = dur;
    }

    /// <summary>
    /// Tạo bản sao độc lập — bắt buộc dùng trước khi thêm vào activeEffects
    /// để tránh ghi đè lên dữ liệu ScriptableObject gốc.
    /// </summary>
    public StatusEffect Clone()
    {
        return new StatusEffect(effectName, effectType, value, duration);
    }
}

public enum StatusEffectType
{
    BuffAtk,
    BuffDef,
    BuffSpd,
    BuffHP,
    BuffHeal,
    DebuffAtk,
    DebuffDef,
    DebuffSpd,
    Poison,
    Stun
}
