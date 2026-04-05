using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Info")]
    public string entityName = "New Enemy";

    [Header("Battle Visual")]
    public GameObject battlePrefab; // Model/Sprite duoc spawn khi vao battle

    [Header("Base Stats")]
    public int baseHP = 20;
    public int baseAtk = 6;
    public int baseDef = 1;
    public int baseSpd = 2;

    [Header("Attack List")]
    public List<EnemyAttackData> attacks = new();

    [Header("AI")]
    [Tooltip("Enemy AI strategy. If null, fallback to random attack.")]
    public EnemyAI ai;

    public EnemyStatus CreateStatus()
    {
        var e = new EnemyStatus(
            entityName, baseHP, baseAtk, baseDef, baseSpd
        );

        e.battlePrefab = battlePrefab;
        e.ai = ai;

        // ADD ATTACK
        foreach (var atk in attacks)
        {
            if (atk == null)
                continue;

            e.AddAttack(atk);
        }

        Debug.Log(
        $"{entityName} loaded | Attacks: {attacks.Count} | AI: {(ai != null ? ai.name : "Random")}");

        return e;
    }
}