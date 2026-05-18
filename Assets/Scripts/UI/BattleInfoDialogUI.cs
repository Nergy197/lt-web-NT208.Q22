using UnityEngine;
using TMPro;

public class BattleInfoDialogUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI buffText;
    [SerializeField] private TextMeshProUGUI debuffText;
    [SerializeField] private TextMeshProUGUI enemyComboText;

    private void Start()
    {
        Clear();
    }

    public void Clear()
    {
        if (buffText != null) buffText.text = "Buffs: None";
        if (debuffText != null) debuffText.text = "Debuffs: None";
        if (enemyComboText != null) enemyComboText.text = "Sắp tới: ???";
    }

    public void UpdateBuffDebuff(Status target)
    {
        if (target == null)
        {
            Clear();
            return;
        }

        string buffs = "<b><color=#55FF55>BUFFS:</color></b>\n";
        string debuffs = "<b><color=#FF5555>DEBUFFS:</color></b>\n";
        bool hasBuff = false;
        bool hasDebuff = false;

        var effects = target.GetActiveEffects();
        foreach (var effect in effects)
        {
            bool isDebuff = effect.effectType == StatusEffectType.DebuffAtk || 
                            effect.effectType == StatusEffectType.DebuffDef || 
                            effect.effectType == StatusEffectType.DebuffSpd || 
                            effect.effectType == StatusEffectType.Poison || 
                            effect.effectType == StatusEffectType.Stun;

            string typeDesc = "";
            switch (effect.effectType)
            {
                case StatusEffectType.BuffAtk: typeDesc = $"+{effect.value}% Atk"; break;
                case StatusEffectType.BuffDef: typeDesc = $"+{effect.value}% Def"; break;
                case StatusEffectType.BuffSpd: typeDesc = $"+{effect.value}% Spd"; break;
                case StatusEffectType.BuffHP: typeDesc = $"+{effect.value} MaxHP"; break;
                case StatusEffectType.BuffHeal: typeDesc = $"+{effect.value} HP/turn"; break;
                case StatusEffectType.DebuffAtk: typeDesc = $"-{effect.value}% Atk"; break;
                case StatusEffectType.DebuffDef: typeDesc = $"-{effect.value}% Def"; break;
                case StatusEffectType.DebuffSpd: typeDesc = $"-{effect.value}% Spd"; break;
                case StatusEffectType.Poison: typeDesc = $"-{effect.value} HP/turn"; break;
                case StatusEffectType.Stun: typeDesc = "Bỏ lượt"; break;
            }

            string turnText = effect.duration < 0 ? "Vĩnh viễn" : $"{effect.duration} lượt";
            string effectInfo = $"• {effect.effectName} ({typeDesc}) - Còn: {turnText}\n";

            if (isDebuff)
            {
                debuffs += effectInfo;
                hasDebuff = true;
            }
            else
            {
                buffs += effectInfo;
                hasBuff = true;
            }
        }

        if (buffText != null) buffText.text = hasBuff ? buffs.TrimEnd() : "<b><color=#55FF55>BUFFS:</color></b> Trống";
        if (debuffText != null) debuffText.text = hasDebuff ? debuffs.TrimEnd() : "<b><color=#FF5555>DEBUFFS:</color></b> Trống";
    }

    public void UpdateEnemyCombo(EnemyAttackData attack)
    {
        if (enemyComboText == null) return;

        if (attack == null)
        {
            enemyComboText.text = "Sắp tới: ???";
            return;
        }

        int totalHits = 0;
        foreach (var hit in attack.hits)
        {
            totalHits += Mathf.Max(1, hit.repeat);
        }

        // Show attack name and number of hits
        enemyComboText.text = $"Sắp tới: <color=orange>{attack.attackName}</color> ({totalHits} Hits)";
    }
}
