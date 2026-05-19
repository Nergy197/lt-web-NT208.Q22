using UnityEngine;
using TMPro;

public class BattleInfoDialogUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI buffText;
    [SerializeField] private TextMeshProUGUI debuffText;
    [SerializeField] private TextMeshProUGUI enemyComboText;

    private void OnEnable()
    {
        BattleEvents.OnEnemyAttackAnnounced += HandleAttackAnnounced;
        BattleEvents.OnEnemyHitIncoming     += HandleHitIncoming;
        BattleEvents.OnParryWindowOpened    += HandleParryWindowOpened;
        BattleEvents.OnAttackFinished       += HandleAttackFinished;
    }

    private void OnDisable()
    {
        BattleEvents.OnEnemyAttackAnnounced -= HandleAttackAnnounced;
        BattleEvents.OnEnemyHitIncoming     -= HandleHitIncoming;
        BattleEvents.OnParryWindowOpened    -= HandleParryWindowOpened;
        BattleEvents.OnAttackFinished       -= HandleAttackFinished;
    }

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

    private void HandleAttackAnnounced(EnemyAttackData attack, EnemyStatus enemy, PlayerStatus target)
    {
        if (enemyComboText == null) return;
        int totalHits = attack.hits.Count;
        enemyComboText.text = $"Dang danh: <color=#FF4444>{attack.attackName}</color> ({totalHits} Hits)";
    }

    private void HandleHitIncoming(EnemyAttackHit hit, int hitIndex, EnemyStatus enemy)
    {
        if (enemyComboText == null) return;
        if (hit.canBeParried)
            enemyComboText.text = $"Don {hitIndex + 1} sap toi...";
        else
            enemyComboText.text = $"Don {hitIndex + 1} sap toi (khong parry)";
    }

    private void HandleParryWindowOpened(PlayerStatus player)
    {
        if (enemyComboText == null) return;
        enemyComboText.text = "<color=#FFE040>>>> PARRY! <<<</color>";
    }

    private void HandleAttackFinished()
    {
        // BattleManager.OnAttackFinished se goi UpdateEnemyCombo ngay sau — reset tam thoi
        if (enemyComboText != null) enemyComboText.text = "Sap toi: ???";
    }

    public void UpdateEnemyCombo(EnemyAttackData attack)
    {
        if (enemyComboText == null) return;

        if (attack == null)
        {
            enemyComboText.text = "Sắp tới: ???";
            return;
        }

        int totalHits = attack.hits.Count;
        enemyComboText.text = $"Sắp tới: <color=orange>{attack.attackName}</color> ({totalHits} Hits)";
    }
}
