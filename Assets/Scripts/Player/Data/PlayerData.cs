using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("Info")]
    public string entityName = "New Player";

    [Header("Battle Visual")]
    public GameObject battlePrefab; // Model/Sprite được spawn khi vào battle


    [Header("Base Stats")]
    public int baseHP = 30;
    public int baseAtk = 8;
    public int baseDef = 2;
    public int baseSpd = 3;
    public int maxAP = 100;


    [Header("Attack List")]
    public List<PlayerAttackData> attacks = new();



    public PlayerStatus CreateStatus()
    {
        var p = new PlayerStatus(
            entityName, baseHP, baseAtk, baseDef, baseSpd, maxAP
        );

        p.battlePrefab = battlePrefab;


        // ================= ADD ATTACK =================

        foreach (var atk in attacks)
        {
            if (atk == null)
                continue;

            p.AddSkill(atk);
        }


        // ================= DEBUG =================

        Debug.Log(
        $"{entityName} loaded | Skills: {p.SkillCount} | Basic: {p.BasicAttack}");


        return p;
    }
}