using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/MapData")]
public class Mapdata : ScriptableObject
{
    [Header("=== MAP INFORMATION ===")]
    public string mapName;
    public int mapId;

    [Header("=== DIFFICULTY / ENEMY LEVEL ===")]
    [Tooltip("Common level applied to every enemy in this map")]
    public int enemyLevel = 1;

    [Header("=== ENEMY CONFIGURATION ===")]
    public List<StatusEffect> enemyBuffs = new List<StatusEffect>();
    public List<StatusEffect> enemyDebuffs = new List<StatusEffect>();

    [Header("=== PLAYER EFFECTS (APPLIED WHEN BATTLE STARTS) ===")]
    public List<StatusEffect> playerBuffs = new List<StatusEffect>();
    public List<StatusEffect> playerDebuffs = new List<StatusEffect>();

    // ---------------- APPLY ----------------
    public void ApplyPlayerEffects(PlayerStatus player)
    {
        if (player == null) return;

        foreach (var buff in playerBuffs)
            player.ApplyStatusEffect(buff);

        foreach (var deb in playerDebuffs)
            player.ApplyStatusEffect(deb);

        Debug.Log($"[Mapdata] Applied {playerBuffs.Count} buffs and {playerDebuffs.Count} debuffs to {player.entityName}");
    }

    public void ApplyEnemyEffects(EnemyStatus enemy)
    {
        if (enemy == null) return;

        if (enemyLevel > 0) enemy.SetLevel(enemyLevel);

        foreach (var buff in enemyBuffs)
            enemy.ApplyStatusEffect(buff);

        foreach (var deb in enemyDebuffs)
            enemy.ApplyStatusEffect(deb);

        Debug.Log($"[Mapdata] Set {enemy.entityName} to level {enemyLevel}, applied {enemyBuffs.Count} buffs and {enemyDebuffs.Count} debuffs");
    }

    // ---------------- RANDOM GENERATOR ----------------
    // Generate player buffs/debuffs heuristically depending on map difficulty (enemyLevel)
    public void GenerateRandomPlayerEffects(int seed = 0)
    {
        playerBuffs.Clear();
        playerDebuffs.Clear();

        System.Random rnd = (seed == 0) ? new System.Random() : new System.Random(seed);

        // simple heuristic: enemyLevel low -> more buffs, high -> more debuffs
        int lvl = Mathf.Max(1, enemyLevel);

        if (lvl <= 3)
        {
            // easy map: small buffs
            playerBuffs.Add(new StatusEffect("Blessing of Vitality", StatusEffectType.BuffHP, 10, 3));
            playerBuffs.Add(new StatusEffect("Swift Foot", StatusEffectType.BuffSpd, 2, 3));
            // occasional heal
            if (rnd.NextDouble() < 0.6)
                playerBuffs.Add(new StatusEffect("Start Heal", StatusEffectType.BuffHeal, 15, 1));
        }
        else if (lvl <= 6)
        {
            // medium map: mixed
            playerBuffs.Add(new StatusEffect("Harden", StatusEffectType.BuffDef, 3, 3));
            if (rnd.NextDouble() < 0.5)
                playerBuffs.Add(new StatusEffect("Rage", StatusEffectType.BuffAtk, 3, 3));

            // small chance of minor poison hazard placed on map
            if (rnd.NextDouble() < 0.4)
                playerDebuffs.Add(new StatusEffect("Miasma", StatusEffectType.Poison, Mathf.Max(1, lvl / 2), 3));
        }
        else
        {
            // hard map: stronger debuffs and fewer buffs
            playerDebuffs.Add(new StatusEffect("Plague Cloud", StatusEffectType.Poison, lvl / 2 + 1, 4));
            playerDebuffs.Add(new StatusEffect("Fatigue", StatusEffectType.DebuffSpd, 2, 4));

            // a small buff to compensate
            if (rnd.NextDouble() < 0.3)
                playerBuffs.Add(new StatusEffect("Adrenaline", StatusEffectType.BuffAtk, 5, 2));
        }

        // normalize durations: 0 duration means immediate only; we prefer -1 (perm) or positive turns.
        playerBuffs.ForEach(e => { if (e.duration == 0) e.duration = 1; });
        playerDebuffs.ForEach(e => { if (e.duration == 0) e.duration = 1; });

        Debug.Log($"[Mapdata] Generated {playerBuffs.Count} player buffs and {playerDebuffs.Count} player debuffs for map '{mapName}' (level {enemyLevel})");
    }

    // optional: generate enemy effects similarly
    public void GenerateRandomEnemyEffects(int seed = 0)
    {
        enemyBuffs.Clear();
        enemyDebuffs.Clear();

        System.Random rnd = (seed == 0) ? new System.Random() : new System.Random(seed);

        int lvl = Mathf.Max(1, enemyLevel);
        if (lvl <= 3)
        {
            enemyBuffs.Add(new StatusEffect("Minor Shield", StatusEffectType.BuffDef, 2, -1));
        }
        else if (lvl <= 6)
        {
            enemyBuffs.Add(new StatusEffect("Ferocity", StatusEffectType.BuffAtk, 3, 3));
            if (rnd.NextDouble() < 0.4)
                enemyDebuffs.Add(new StatusEffect("Weaken Aura", StatusEffectType.DebuffAtk, 2, 3));
        }
        else
        {
            enemyBuffs.Add(new StatusEffect("Bloodlust", StatusEffectType.BuffAtk, 5, 4));
            enemyBuffs.Add(new StatusEffect("Toxic Armor", StatusEffectType.BuffDef, 4, 4));
            if (rnd.NextDouble() < 0.5)
                enemyDebuffs.Add(new StatusEffect("Corrosive Spit", StatusEffectType.Poison, lvl / 2 + 1, 3));
        }

        // normalize durations: 0 duration means immediate only; we prefer -1 (perm) or positive turns.
        enemyBuffs.ForEach(e => { if (e.duration == 0) e.duration = 1; });
        enemyDebuffs.ForEach(e => { if (e.duration == 0) e.duration = 1; });

        Debug.Log($"[Mapdata] Generated {enemyBuffs.Count} enemy buffs and {enemyDebuffs.Count} enemy debuffs for map '{mapName}'");
    }

    // ---------------- UTIL ----------------
    public void ClearAllEffects()
    {
        playerBuffs.Clear();
        playerDebuffs.Clear();
        enemyBuffs.Clear();
        enemyDebuffs.Clear();
    }
}
