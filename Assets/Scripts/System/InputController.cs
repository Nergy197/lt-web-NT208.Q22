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
        BindSkillMenuInput();

        SetMode(InputMode.Map);
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

        Debug.Log($"[INPUT MODE] {mode}");
    }

    // ================= BATTLE INPUT =================
    private void BindBattleInput()
    {
        // BASIC ATTACK (E)
        Input.Battle.BasicAttack.performed += _ =>
        {
            if (!CanBattleInput()) return;
            Debug.Log("[INPUT] Basic Attack");
            battle.SelectBasicAttack();
        };

        // OPEN SKILL MENU (W)
        Input.Battle.OpenSkillMenu.performed += _ =>
        {
            if (!CanBattleInput()) return;
            Debug.Log("[INPUT] Open Skill Menu");
            SetMode(InputMode.BattleSkillMenu);
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

    // ================= SKILL MENU INPUT =================
    private void BindSkillMenuInput()
    {
        // SKILL 1 (E)
        Input.SkillMenu.Skill1.performed += _ =>
        {
            if (Mode != InputMode.BattleSkillMenu) return;
            Debug.Log("[SKILL MENU] Skill 1");
            battle.UseSkill(0);
            SetMode(InputMode.Battle);
        };

        // SKILL 2 (W)
        Input.SkillMenu.Skill2.performed += _ =>
        {
            if (Mode != InputMode.BattleSkillMenu) return;
            Debug.Log("[SKILL MENU] Skill 2");
            battle.UseSkill(1);
            SetMode(InputMode.Battle);
        };

        // CANCEL (ESC)
        Input.SkillMenu.Cancel.performed += _ =>
        {
            if (Mode != InputMode.BattleSkillMenu) return;
            Debug.Log("[SKILL MENU] Cancel");
            SetMode(InputMode.Battle);
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
