using UnityEngine;
using System.Linq;

public class BattleDemoStarter : MonoBehaviour
{
    private BattleManager battle;

    [Header("Demo Options")]
    public bool killFirstPlayerAtStart = false;
    public bool showInputHint = true;

    private void Awake()
    {
        battle = FindFirstObjectByType<BattleManager>();
        if (battle == null)
        {
            Debug.LogError("[DEMO] BattleManager not found in scene");
        }
    }

    private void Start()
    {
        // ================= CREATE PARTIES =================
        Party playerParty = new Party(PartyType.Player);
        Party enemyParty  = new Party(PartyType.Enemy);

        // ================= FIND PLAYER OBJECTS =================
        var playerManagers = FindObjectsOfType<PlayerDataManager>();
        if (playerManagers.Length == 0)
        {
            Debug.LogError("[DEMO] No PlayerDataManager found in scene");
            return;
        }

        foreach (var pm in playerManagers)
        {
            var status = pm.CreateStatus();
            if (status == null) continue;

            playerParty.AddMember(status);
        }

        // ================= FIND ENEMY OBJECTS =================
        var enemyManagers = FindObjectsOfType<EnemyDataManager>();
        if (enemyManagers.Length == 0)
        {
            Debug.LogError("[DEMO] No EnemyDataManager found in scene");
            return;
        }

        foreach (var em in enemyManagers)
        {
            var status = em.CreateStatus();
            if (status == null) continue;

            enemyParty.AddMember(status);
        }

        // ================= OPTIONAL TEST =================
        if (killFirstPlayerAtStart)
        {
            var p = playerParty.Members.OfType<PlayerStatus>().FirstOrDefault();
            if (p != null)
            {
                p.TakeDamage(p, 99999);
                Debug.Log("[DEMO] First player killed to test skip dead unit");
            }
        }

        // ================= INIT BATTLE =================
        Debug.Log("=====================================");
        Debug.Log(" DEMO BATTLE START (SCENE OBJECTS) ");
        Debug.Log("=====================================");
        Debug.Log($"Players: {playerParty.Members.Count}");
        Debug.Log($"Enemies: {enemyParty.Members.Count}");

        if (showInputHint)
        {
            Debug.Log("INPUT:");
            Debug.Log(" SPACE : Basic attack");
            Debug.Log(" W     : Open skill menu");
            Debug.Log(" Q/E   : Use skill");
            Debug.Log(" A/D   : Change target");
        }

        battle.InitBattle(playerParty, enemyParty, mapLevel: 1);
        var input = FindObjectOfType<InputController>();
        if (input != null)
        {
            input.SetMode(InputMode.Battle);
            input.BindBattleManager(battle);
            Debug.Log("[INPUT MODE] Battle");
        }
        else
        {
            Debug.LogError("InputController not found!");
        }

    }
}
