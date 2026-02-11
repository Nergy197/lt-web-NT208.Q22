using UnityEngine;
using System.Linq;

public class BattleDemoStarter : MonoBehaviour
{
    private BattleManager battle;

    [Header("Demo Options")]
    public bool killFirstPlayerAtStart = true; // test skip dead unit
    public bool showInputHint = true;

    private void Awake()
    {
        battle = GetComponent<BattleManager>();
        if (battle == null)
            Debug.LogError("[ERROR] BattleManager missing on Battle System");
    }

    private void Start()
    {
        // ================= CREATE PARTIES =================
        Party playerParty = new Party(PartyType.Player);
        Party enemyParty  = new Party(PartyType.Enemy);

        // ================= FIND PLAYERS =================
        var players = FindObjectsOfType<PlayerDataManager>();
        if (players.Length == 0)
        {
            Debug.LogError("[ERROR] PlayerDataManager not found");
            return;
        }

        foreach (var p in players)
        {
            var data = p.GetPlayerData();
            if (data == null) continue;

            playerParty.AddMember(data.CreateStatus());
        }

        // ================= FIND ENEMIES =================
        var enemies = FindObjectsOfType<EnemyDataManager>();
        if (enemies.Length == 0)
        {
            Debug.LogError("[ERROR] EnemyDataManager not found");
            return;
        }

        foreach (var e in enemies)
        {
            var data = e.GetEnemyData();
            if (data == null) continue;

            enemyParty.AddMember(data.CreateStatus());
        }

        // ================= INIT BATTLE =================
        Debug.Log("=====================================");
        Debug.Log(" DEMO BATTLE START ");
        Debug.Log("=====================================");
        Debug.Log($"Init Battle: Player({playerParty.Members.Count}) vs Enemy({enemyParty.Members.Count})");

        if (showInputHint)
        {
            Debug.Log("INPUT:");
            Debug.Log("  SPACE  : Player basic attack");
            Debug.Log("  A / D  : Change target");
        }

        battle.InitBattle(playerParty, enemyParty, 1);
    }

}
