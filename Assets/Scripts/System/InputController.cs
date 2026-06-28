using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InputController : MonoBehaviour
{
    public static InputController Instance;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    public GameInput Input;
    public InputMode Mode;

    private BattleManager battle;
    private bool mobileInteractQueued;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[INPUT] Destroy duplicate");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Đảm bảo InputController nằm ở root, nếu không DontDestroyOnLoad sẽ không hoạt động!
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        Input = new GameInput();
        Input.Enable();

        BindBattleInput();
        BindSkillMenuInput();
        BindSavePointMenuInput();
        BindMapInput();

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
            case InputMode.BattleItemMenu: Input.Battle.Enable();       break;
            case InputMode.UI:             Input.SavePointMenu.Enable(); break;
            case InputMode.Cutscene:       Input.Map.Enable();          break;
            // Pause: vẫn bật Map map để ESC (Open Menu) đóng được pause.
            // Moves bị đóng băng do Time.timeScale=0; Interact bị SavePoint chặn (Mode != Map).
            case InputMode.Pause:          Input.Map.Enable();          break;
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
        Input.Battle.BasicAttack.performed   += _ => { if (ShouldIgnoreBattleGameplayInput() || Mode == InputMode.BattleItemMenu) return; battle?.SelectBasicAttack(); };
        Input.Battle.NextTarget.performed    += _ => { if (ShouldIgnoreBattleGameplayInput()) return; battle?.ChangeTargetInput(1); };
        Input.Battle.PrevTarget.performed    += _ => { if (ShouldIgnoreBattleGameplayInput()) return; battle?.ChangeTargetInput(-1); };
        Input.Battle.Parry.performed         += _ => { if (ShouldIgnoreBattleGameplayInput() || Mode == InputMode.BattleItemMenu) return; battle?.RequestParry(); };
        Input.Battle.OpenSkillMenu.performed += _ => { if (ShouldIgnoreBattleGameplayInput() || Mode == InputMode.BattleItemMenu) return; battle?.RequestOpenSkillMenu(); };
        Input.Battle.OpenItemMenu.performed  += _ => { if (ShouldIgnoreBattleGameplayInput()) return; battle?.RequestOpenItemMenu(); };
        Input.Battle.Flee.performed          += _ => { if (ShouldIgnoreBattleGameplayInput() || Mode == InputMode.BattleItemMenu) return; battle?.TryFlee(); };
        Input.Battle.Confirm.performed       += _ => { if (ShouldIgnoreBattleGameplayInput()) return; battle?.ConfirmAction(); };
        Input.Battle.Cancel.performed        += _ => { if (ShouldIgnoreBattleGameplayInput()) return; battle?.BackToActionMenu(); };
    }

    void BindSkillMenuInput()
    {
        Input.SkillMenu.Skill1.performed += _ => { battle?.UseSkill(0); };
        Input.SkillMenu.Skill2.performed += _ => { battle?.UseSkill(1); };
        Input.SkillMenu.Skill3.performed += _ => { battle?.UseSkill(2); };
        Input.SkillMenu.Cancel.performed += _ => battle?.BackToActionMenu();
    }

    /// <summary>Gọi từ nút "Tương tác" trên mobile UI để kích hoạt Interact trong 1 frame.</summary>
    public void QueueMobileInteract() => mobileInteractQueued = true;

    /// <summary>Kiểm tra Interact từ cả bàn phím lẫn nút mobile (tự reset sau khi đọc).</summary>
    public bool IsInteractPressed()
    {
        bool pressed = Input.Map.Interact.WasPressedThisFrame() || mobileInteractQueued;
        mobileInteractQueued = false;
        return pressed;
    }

    void BindSavePointMenuInput()
    {
        Input.SavePointMenu.Swap.performed  += _ => SavePointUI.Instance?.OnSwapOrder();
        Input.SavePointMenu.Close.performed += _ => SavePointUI.Instance?.OnClose();
        Input.SavePointMenu.Heal.performed  += _ => SavePointUI.Instance?.OnHeal();
        Input.SavePointMenu.Save.performed  += _ => SavePointUI.Instance?.OnSave();
    }

    void BindMapInput()
    {
        // ESC (Open Menu) mở/đóng Pause Menu. Chạy trên cả web desktop (bàn phím),
        // không phụ thuộc nút pause của mobile. Map map được bật cả ở mode Pause
        // (xem SetMode) để ESC có thể đóng lại pause.
        Input.Map.OpenMenu.performed += _ => PauseMenuUI.Instance?.Toggle();
    }

    bool ShouldIgnoreBattleGameplayInput()
    {
        if (EventSystem.current == null) return false;

        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed) continue;
                if (EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue()))
                    return true;
            }
        }

        // Mouse click lên UI button không được trigger gameplay battle input.
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
            return EventSystem.current.IsPointerOverGameObject();

        return false;
    }
}