using UnityEngine;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    public GameInput Input { get; private set; }
    public InputMode Mode { get; private set; }

    private BattleManager battle;

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
        BindBattleInput();

        SetMode(InputMode.Map);
    }

    // ================= MODE =================
    public void SetMode(InputMode mode)
    {
        Mode = mode;

        Input.Map.Disable();
        Input.Battle.Disable();

        if (mode == InputMode.Map)
            Input.Map.Enable();
        else
            Input.Battle.Enable();

        Debug.Log($"[INPUT MODE] {mode}");
    }

    // ================= BIND BATTLE =================
    private void BindBattleInput()
    {
        // BASIC ATTACK (E)
        Input.Battle.BasicAttack.performed += _ =>
        {
            if (!CanBattleInput()) return;
            battle.SelectBasicAttack();
        };

        // OPEN SKILL MENU (W)
        Input.Battle.OpenSkillMenu.performed += _ =>
        {
            if (!CanBattleInput()) return;
            Mode = InputMode.BattleSkillMenu;
            Debug.Log("[BATTLE] Open Skill Menu");
        };

        // SKILL 1 (Q)
        Input.Battle.Skill1.performed += _ =>
        {
            if (Mode != InputMode.BattleSkillMenu) return;
            battle.UseSkill(0);
            SetMode(InputMode.Battle);
        };

        // SKILL 2 (W)
        Input.Battle.Skill2.performed += _ =>
        {
            if (Mode != InputMode.BattleSkillMenu) return;
            battle.UseSkill(1);
            SetMode(InputMode.Battle);
        };

        // CANCEL (E)
        Input.Battle.Cancel.performed += _ =>
        {
            if (Mode == InputMode.BattleSkillMenu)
            {
                SetMode(InputMode.Battle);
                Debug.Log("[BATTLE] Close Skill Menu");
            }
        };

        // TARGET
        Input.Battle.NextTarget.performed += _ =>
        {
            if (!CanBattleInput()) return;
            battle.ChangeTargetInput(+1);
        };

        Input.Battle.PrevTarget.performed += _ =>
        {
            if (!CanBattleInput()) return;
            battle.ChangeTargetInput(-1);
        };
    }

    private bool CanBattleInput()
    {
        return Mode == InputMode.Battle
            && battle != null
            && battle.CanAcceptInput;
    }

    // ================= BIND BATTLE MANAGER =================
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
