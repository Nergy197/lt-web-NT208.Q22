using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public static InputController Instance;

    public GameInput Input;
    public InputMode Mode;

    private BattleManager battle;

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
        BindSavePointMenuInput();

        SetMode(InputMode.Map);
        Debug.Log("[INPUT] Awake complete");
    }

    void OnDisable()  => Input?.Disable();
    void OnDestroy()  => Input?.Dispose();

    public void SetMode(InputMode mode)
    {
        Mode = mode;

        Input.Map.Disable();
        Input.Battle.Disable();
        Input.SkillMenu.Disable();
        Input.SavePointMenu.Disable();

        switch (mode)
        {
            case InputMode.Map:            Input.Map.Enable();          break;
            case InputMode.Battle:         Input.Battle.Enable();       break;
            case InputMode.BattleSkillMenu: Input.SkillMenu.Enable();  break;
            case InputMode.UI:             Input.SavePointMenu.Enable(); break;
            case InputMode.Cutscene:       Input.Map.Enable();          break;
        }

        Debug.Log("[INPUT MODE] " + mode);
    }

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

    void BindBattleInput()
    {
        Input.Battle.BasicAttack.performed  += _ => battle?.SelectBasicAttack();
        Input.Battle.NextTarget.performed   += _ => battle?.ChangeTargetInput(1);
        Input.Battle.PrevTarget.performed   += _ => battle?.ChangeTargetInput(-1);
        Input.Battle.Parry.performed        += _ => battle?.RequestParry();
        Input.Battle.OpenSkillMenu.performed += _ => SetMode(InputMode.BattleSkillMenu);
        Input.Battle.OpenItemMenu.performed  += _ => Debug.Log("[INPUT] OpenItemMenu (chưa implement)");
        Input.Battle.Flee.performed         += _ => battle?.TryFlee();
    }

    void BindSkillMenuInput()
    {
        Input.SkillMenu.Skill1.performed += _ => { battle?.UseSkill(0); SetMode(InputMode.Battle); };
        Input.SkillMenu.Skill2.performed += _ => { battle?.UseSkill(1); SetMode(InputMode.Battle); };
        Input.SkillMenu.Cancel.performed += _ => SetMode(InputMode.Battle);
    }

    void BindSavePointMenuInput()
    {
        Input.SavePointMenu.Swap.performed  += _ => SavePointUI.Instance?.OnSwapOrder();
        Input.SavePointMenu.Close.performed += _ => SavePointUI.Instance?.OnClose();
        Input.SavePointMenu.Heal.performed  += _ => SavePointUI.Instance?.OnHeal();
        Input.SavePointMenu.Save.performed  += _ => SavePointUI.Instance?.OnSave();
    }
}