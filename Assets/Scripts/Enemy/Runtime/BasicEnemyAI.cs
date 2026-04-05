using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AI co ban cho enemy: Uu tien tan cong player co HP thap nhat.
/// Neu co nhieu attack, chon attack co damage cao khi can ha guc,
/// hoac chon attack co effects khi doi phuong dang khoe.
/// </summary>
[CreateAssetMenu(fileName = "BasicEnemyAI", menuName = "Game/AI/BasicEnemyAI")]
public class BasicEnemyAI : EnemyAI
{
    public override void CalculateAction(BattleManager bm, EnemyStatus self, Party playerParty, Party enemyParty)
    {
        // --- CHON TARGET ---
        PlayerStatus target = PickTarget(playerParty);

        if (target == null)
        {
            Debug.Log($"[AI] {self.entityName}: No alive player target");
            return;
        }

        // --- CHON ATTACK ---
        EnemyAttackData chosenAttack = PickAttack(self, target);

        if (chosenAttack == null)
        {
            Debug.Log($"[AI] {self.entityName}: No attacks available");
            return;
        }

        Debug.Log($"[AI] {self.entityName} -> {target.entityName} using {chosenAttack.attackName}");

        // Set target cho BattleManager
        bm.SetEnemyTarget(target);

        // Thuc hien attack
        chosenAttack.CreateInstance().Use(self, target);
    }

    /// <summary>
    /// Chon player co HP thap nhat (de ha guc).
    /// </summary>
    private PlayerStatus PickTarget(Party playerParty)
    {
        PlayerStatus bestTarget = null;
        int lowestHP = int.MaxValue;

        foreach (var member in playerParty.Members)
        {
            if (!member.IsAlive) continue;

            var ps = member as PlayerStatus;
            if (ps == null) continue;

            if (ps.currentHP < lowestHP)
            {
                lowestHP = ps.currentHP;
                bestTarget = ps;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Chon attack phu hop:
    /// - Neu target co HP thap (duoi 30% MaxHP) -> chon don damage cao nhat de ha guc.
    /// - Neu khong -> chon ngau nhien (co trong so cho don co effects).
    /// </summary>
    private EnemyAttackData PickAttack(EnemyStatus self, PlayerStatus target)
    {
        var attack = self.GetRandomAttack();
        if (attack == null) return null;

        // Neu chi co 1 attack thi return luon
        var allAttacks = self.GetAllAttacks();
        if (allAttacks == null || allAttacks.Count <= 1) return attack;

        float hpRatio = (float)target.currentHP / target.MaxHP;

        if (hpRatio < 0.3f)
        {
            // Target sap chet -> chon don damage cao nhat
            EnemyAttackData bestDmg = null;
            float bestMultiplier = 0f;

            foreach (var atk in allAttacks)
            {
                float totalDmg = 0f;
                foreach (var hit in atk.hits)
                    totalDmg += hit.damageMultiplier * Mathf.Max(1, hit.repeat);

                if (totalDmg > bestMultiplier)
                {
                    bestMultiplier = totalDmg;
                    bestDmg = atk;
                }
            }

            return bestDmg ?? attack;
        }

        // Chon ngau nhien nhung uu tien don co effects (60% chance chon don co effect neu co)
        var attacksWithEffects = allAttacks.Where(a => a.effects != null && a.effects.Count > 0).ToList();

        if (attacksWithEffects.Count > 0 && Random.value < 0.6f)
            return attacksWithEffects[Random.Range(0, attacksWithEffects.Count)];

        return allAttacks[Random.Range(0, allAttacks.Count)];
    }
}
