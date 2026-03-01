using UnityEngine;

public class InputController : MonoBehaviour
{
    public static InputController Instance;

    public GameInput Input;

    public InputMode Mode;

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



    // ================= SET MODE =================

    public void SetMode(InputMode mode)
    {
        if (Input == null)
            return;


        // QUAN TRỌNG: disable ALL trước
        Input.Map.Disable();

        Input.Battle.Disable();

        Input.SkillMenu.Disable();


        Mode = mode;


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



    // ================= BIND BATTLE =================

    public void BindBattleManager(
        BattleManager bm)
    {
        battle = bm;

        SetMode(InputMode.Battle);

        Debug.Log("Battle Input Bound");
    }



    public void UnbindBattleManager()
    {
        battle = null;


        // QUAN TRỌNG
        Input.Battle.Disable();

        Input.SkillMenu.Disable();


        SetMode(InputMode.Map);

        Debug.Log("Battle Input Unbound");
    }



    // ================= DESTROY =================

    private void OnDestroy()
    {
        Cleanup();
    }


    private void OnDisable()
    {
        Cleanup();
    }



    private void Cleanup()
    {
        if (Input == null)
            return;


        Input.Map.Disable();

        Input.Battle.Disable();

        Input.SkillMenu.Disable();


        Input.Disable();


        Input.Dispose();


        Input = null;


        Debug.Log("GameInput Cleaned");
    }



    // ================= INPUT EVENTS =================

    private void BindBattleInput()
    {
        Input.Battle.BasicAttack.performed += ctx =>
        {
            if (battle != null)

                battle.SelectBasicAttack();
        };


        Input.Battle.NextTarget.performed += ctx =>
        {
            if (battle != null)

                battle.ChangeTargetInput(1);
        };


        Input.Battle.PrevTarget.performed += ctx =>
        {
            if (battle != null)

                battle.ChangeTargetInput(-1);
        };
    }



    private void BindSkillMenuInput()
    {
        Input.SkillMenu.Skill1.performed += ctx =>
        {
            if (battle != null)

                battle.UseSkill(0);
        };


        Input.SkillMenu.Skill2.performed += ctx =>
        {
            if (battle != null)

                battle.UseSkill(1);
        };
    }

}