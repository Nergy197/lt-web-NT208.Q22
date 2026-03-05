using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public static InputController Instance;

    public GameInput Input;

    public InputMode Mode;

    private BattleManager battle;

    // ================= INIT =================

    void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("[INPUT] Destroy duplicate");
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

        Debug.Log("[INPUT] Awake complete");
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

    // ================= BIND =================

    public void BindBattleManager(BattleManager bm)
    {
        battle = bm;

        SetMode(InputMode.Battle);

        Debug.Log("[INPUT] Battle bound");
    }

    public void UnbindBattleManager()
    {
        battle = null;

        SetMode(InputMode.Map);

        Debug.Log("[INPUT] Battle unbound");
    }

    // ================= BATTLE INPUT =================

    void BindBattleInput()
    {
        Input.Battle.BasicAttack.performed += ctx =>
        {
            Debug.Log("[INPUT] BasicAttack");

            battle?.SelectBasicAttack();
        };

        Input.Battle.NextTarget.performed += ctx =>
        {
            Debug.Log("[INPUT] NextTarget");

            battle?.ChangeTargetInput(1);
        };

        Input.Battle.PrevTarget.performed += ctx =>
        {
            Debug.Log("[INPUT] PrevTarget");

            battle?.ChangeTargetInput(-1);
        };

        Input.Battle.Parry.performed += ctx =>
        {
            Debug.Log("[INPUT] Parry");

            battle?.RequestParry();
        };

        Input.Battle.OpenSkillMenu.performed += ctx =>
        {
            Debug.Log("[INPUT] OpenSkillMenu");

            SetMode(InputMode.BattleSkillMenu);
        };

        Input.Battle.OpenItemMenu.performed += ctx =>
        {
            Debug.Log("[INPUT] HealAlly (Q)");

            battle?.HealAlly();
        };
    }

    // ================= SKILL INPUT =================

    void BindSkillMenuInput()
    {
        Input.SkillMenu.Skill1.performed += ctx =>
        {
            Debug.Log("[INPUT] Skill1");

            battle?.UseSkill(0);

            SetMode(InputMode.Battle);
        };

        Input.SkillMenu.Skill2.performed += ctx =>
        {
            Debug.Log("[INPUT] Skill2");

            battle?.UseSkill(1);

            SetMode(InputMode.Battle);
        };

        Input.SkillMenu.Cancel.performed += ctx =>
        {
            Debug.Log("[INPUT] CancelSkillMenu");

            SetMode(InputMode.Battle);
        };
    }

}