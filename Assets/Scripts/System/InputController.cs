using UnityEngine;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    public GameInput Input { get; private set; }

    public InputMode Mode { get; private set; }

    private BattleManager battle;



    // ================= INIT =================

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        Input = new GameInput();

        Input.Enable();

        BindBattleInput();

        BindSkillMenuInput();

        SetMode(InputMode.Map);
    }



    private void OnDestroy()
    {
        if (Input != null)
            Input.Disable();
    }



    // ================= MODE =================

    public void SetMode(InputMode mode)
    {
        Mode = mode;

        Input.Map.Disable();
        Input.Battle.Disable();
        Input.SkillMenu.Disable();

        switch (mode)
        {
            case InputMode.Map:
                Input.Map.Enable();
                break;

            case InputMode.Battle:
                Input.Battle.Enable();
                break;

            case InputMode.BattleSkillMenu:
                Input.SkillMenu.Enable();
                break;
        }

        Debug.Log("[INPUT MODE] " + mode);
    }



    // ================= BATTLE INPUT =================

    private void BindBattleInput()
    {

        // BASIC ATTACK

        Input.Battle.BasicAttack.performed += ctx =>
        {
            if (!CanBattleInput()) return;

            battle.SelectBasicAttack();
        };



        // SKILL MENU

        Input.Battle.OpenSkillMenu.performed += ctx =>
        {
            if (!CanBattleInput()) return;

            SetMode(InputMode.BattleSkillMenu);
        };



        // TARGET

        Input.Battle.PrevTarget.performed += ctx =>
        {
            if (!CanBattleInput()) return;

            battle.ChangeTargetInput(-1);
        };


        Input.Battle.NextTarget.performed += ctx =>
        {
            if (!CanBattleInput()) return;

            battle.ChangeTargetInput(+1);
        };



        // PARRY INPUT

        Input.Battle.Parry.performed += ctx =>
        {
            if (battle == null)
                return;


            PlayerStatus player = battle.GetAlivePlayer();


            if (player == null)
                return;


            player.RequestParry();
        };

    }



    // ================= SKILL MENU =================

    private void BindSkillMenuInput()
    {
        Input.SkillMenu.Skill1.performed += ctx =>
        {
            if (Mode != InputMode.BattleSkillMenu) return;

            battle.UseSkill(0);

            SetMode(InputMode.Battle);
        };


        Input.SkillMenu.Skill2.performed += ctx =>
        {
            if (Mode != InputMode.BattleSkillMenu) return;

            battle.UseSkill(1);

            SetMode(InputMode.Battle);
        };


        Input.SkillMenu.Cancel.performed += ctx =>
        {
            if (Mode != InputMode.BattleSkillMenu) return;

            SetMode(InputMode.Battle);
        };
    }



    private bool CanBattleInput()
    {
        return Mode == InputMode.Battle
            && battle != null;
    }



    // ================= BIND =================

    public void BindBattleManager(BattleManager bm)
    {
        battle = bm;

        SetMode(InputMode.Battle);
    }



    public void UnbindBattleManager()
    {
        battle = null;

        SetMode(InputMode.Map);
    }

}