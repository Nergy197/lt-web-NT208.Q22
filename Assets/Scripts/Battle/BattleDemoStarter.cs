using UnityEngine;

public class BattleDemoStarter : MonoBehaviour
{
    private BattleManager battle;

    private void Awake()
    {
        battle = GetComponent<BattleManager>();
        if (battle == null)
        {
            Debug.LogError("❌ BattleManager missing on Battle System");
        }
    }

    private void Start()
    {
        // ===== HERO =====
        PlayerDataManager heroManager =
            FindFirstObjectByType<PlayerDataManager>();

        if (heroManager == null)
        {
            Debug.LogError("❌ Không tìm thấy PlayerDataManager (Hero)");
            return;
        }

        if (heroManager.GetPlayerData() == null)
        {
            Debug.LogError("❌ Hero chưa gán PlayerData");
            return;
        }

        PlayerStatus heroStatus =
            heroManager.GetPlayerData().CreateStatus();

        // ===== SLIME =====
        EnemyDataManager slimeManager =
            FindFirstObjectByType<EnemyDataManager>();

        if (slimeManager == null)
        {
            Debug.LogError("❌ Không tìm thấy EnemyDataManager (Slime)");
            return;
        }

        if (slimeManager.GetEnemyData() == null)
        {
            Debug.LogError("❌ Slime chưa gán EnemyData");
            return;
        }

        EnemyStatus slimeStatus =
            slimeManager.GetEnemyData().CreateStatus();

        // ===== PARTY =====
        Party playerParty = new Party(PartyType.Player);
        playerParty.AddMember(heroStatus);

        Party enemyParty = new Party(PartyType.Enemy);
        enemyParty.AddMember(slimeStatus);

        // ===== INIT BATTLE =====
        battle.InitBattle(playerParty, enemyParty, 1);

        Debug.Log("=== DEMO START: HERO vs SLIME ===");
    }
}
