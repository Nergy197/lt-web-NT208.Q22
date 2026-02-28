using UnityEngine;

public class BattleDemoStarter : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private BattleManager battle;

    [SerializeField]
    private InputController inputController;



    [Header("Demo Options")]

    public bool autoStartBattle = true;



    // ================= INIT =================

    private void Awake()
    {
        if (battle == null)
            battle = FindObjectOfType<BattleManager>();


        if (inputController == null)
            inputController = FindObjectOfType<InputController>();
    }



    private void Start()
    {
        if (autoStartBattle)
            StartDemo();
    }



    // ================= DEMO START =================

    public void StartDemo()
    {

        if (battle == null)
        {
            Debug.LogError("BattleManager not found");

            return;
        }



        Party playerParty = CreatePlayerParty();

        Party enemyParty = CreateEnemyParty();



        if (playerParty == null || enemyParty == null)
            return;



        Debug.Log("=== DEMO START ===");



        battle.InitBattle(playerParty, enemyParty, 1);



        if (inputController != null)
        {
            inputController.BindBattleManager(battle);

            inputController.SetMode(InputMode.Battle);
        }

    }



    // ================= CREATE PLAYER =================

    private Party CreatePlayerParty()
    {

        PlayerDataManager[] managers =
            FindObjectsOfType<PlayerDataManager>();



        if (managers.Length == 0)
        {
            Debug.LogError("No PlayerDataManager found");

            return null;
        }



        Party party = new Party(PartyType.Player);



        foreach (PlayerDataManager m in managers)
        {

            PlayerStatus status = m.CreateStatus();


            if (status != null)
            {
                party.AddMember(status);

                Debug.Log("Add Player: " + status.entityName);
            }

        }



        return party;
    }



    // ================= CREATE ENEMY =================

    private Party CreateEnemyParty()
    {

        EnemyDataManager[] managers =
            FindObjectsOfType<EnemyDataManager>();



        if (managers.Length == 0)
        {
            Debug.LogError("No EnemyDataManager found");

            return null;
        }



        Party party = new Party(PartyType.Enemy);



        foreach (EnemyDataManager m in managers)
        {

            EnemyStatus status = m.CreateStatus();


            if (status != null)
            {
                party.AddMember(status);

                Debug.Log("Add Enemy: " + status.entityName);
            }

        }



        return party;
    }

}