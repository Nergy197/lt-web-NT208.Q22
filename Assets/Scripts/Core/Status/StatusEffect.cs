using UnityEngine;

[System.Serializable]
public class StatusEffect
{
    public string effectName;
    public StatusEffectType effectType;
    public int value;
    public int duration; // số turn (-1 = vĩnh viễn)

    public StatusEffect(string name, StatusEffectType type, int val, int dur = -1)
    {
        effectName = name;
        effectType = type;
        value = val;
        duration = dur;
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
