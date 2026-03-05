using UnityEngine;
using System.Collections;

/// <summary>
/// BattleDemo: Attach to a GameObject in a test scene.
/// Simulates a minimal battle loop in the Console (no full GameManager needed).
/// Usage: Press Play → check Console/BattleDebugUI for logs.
/// </summary>
public class BattleDemo : MonoBehaviour
{
    [Header("Test Skills (assign in Inspector)")]
    public PlayerAttackData basicAttack;
    public PlayerAttackData skill1;
    public PlayerAttackData skill2;

    [Header("Test Enemies (assign in Inspector)")]
    public EnemyData enemyData;

    // =========================================================

    private PlayerStatus player;
    private EnemyStatus enemy;

    void Log(string msg)
    {
        Debug.Log(msg);
        if (BattleDebugUI.Instance != null)
            BattleDebugUI.Instance.Log(msg);
    }

    // =========================================================

    IEnumerator Start()
    {
        yield return null; // wait one frame for BattleDebugUI to init

        Log("=== BATTLE DEMO START ===");

        SetupPlayer();
        SetupEnemy();

        Log("--- Test 1: Basic Attack ---");
        TestBasicAttack();

        yield return new WaitForSeconds(0.5f);

        Log("--- Test 2: Skill1 ---");
        TestSkill(0);

        yield return new WaitForSeconds(0.5f);

        Log("--- Test 3: Skill2 ---");
        TestSkill(1);

        yield return new WaitForSeconds(0.5f);

        Log("--- Test 4: Use Skill when AP = 0 ---");
        TestSkillNoAP();

        yield return new WaitForSeconds(0.5f);

        Log("--- Test 5: Skill index out of range ---");
        TestSkillOutOfRange();

        Log("=== BATTLE DEMO END ===");
    }

    // =========================================================

    void SetupPlayer()
    {
        player = new PlayerStatus("Hero", 100, 20, 10, 15, maxAP: 100);

        var skills = new System.Collections.Generic.List<PlayerAttackData>();

        if (basicAttack != null) skills.Add(basicAttack);
        if (skill1 != null)      skills.Add(skill1);
        if (skill2 != null)      skills.Add(skill2);

        if (skills.Count == 0)
        {
            Log("[DEMO] No skills assigned → using fallback");
            return;
        }

        player.InitializeSkills(skills);

        Log($"[DEMO] Player: HP={player.HP}, AP={player.currentAP}, Skills={player.SkillCount}");
    }

    void SetupEnemy()
    {
        if (enemyData != null)
        {
            enemy = enemyData.CreateStatus() as EnemyStatus;
        }
        else
        {
            // Fallback: minimal enemy
            enemy = new EnemyStatus("Goblin", 50, 10, 5, 10);
            Log("[DEMO] No enemyData assigned → using fallback Goblin");
        }

        Log($"[DEMO] Enemy: {enemy.entityName} HP={enemy.HP}");
    }

    // =========================================================

    void TestBasicAttack()
    {
        if (player.BasicAttack == null)
        {
            Log("[DEMO] BasicAttack is NULL – assign a skill with apCost=0");
            return;
        }

        int hpBefore = enemy.HP;
        player.BasicAttack.CreateInstance().Use(player, enemy);
        Log($"[DEMO] BasicAttack done. Enemy HP: {hpBefore} → {enemy.HP}");
    }

    void TestSkill(int index)
    {
        var skill = player.GetSkillByIndex(index);
        if (skill == null)
        {
            Log($"[DEMO] Skill[{index}] not found (player has {player.SkillCount} skills)");
            return;
        }

        if (!player.CanUseAP(skill.apCost))
        {
            Log($"[DEMO] Not enough AP for Skill[{index}]: need {skill.apCost}, have {player.currentAP}");
            return;
        }

        int hpBefore = enemy.HP;
        int apBefore = player.currentAP;
        skill.CreateInstance().Use(player, enemy);
        Log($"[DEMO] Skill[{index}] '{skill.attackName}' done. AP: {apBefore}→{player.currentAP}, EnemyHP: {hpBefore}→{enemy.HP}");
    }

    void TestSkillNoAP()
    {
        // Drain all AP
        player.UseAP(player.currentAP);
        Log($"[DEMO] AP drained to {player.currentAP}");
        TestSkill(1); // should fail with "Not enough AP"
    }

    void TestSkillOutOfRange()
    {
        var skill = player.GetSkillByIndex(99);
        if (skill == null)
            Log("[DEMO] Skill[99] correctly returned null ✓");
        else
            Log("[DEMO] UNEXPECTED: Skill[99] found!");
    }
}
