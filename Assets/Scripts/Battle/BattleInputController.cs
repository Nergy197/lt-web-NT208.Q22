using UnityEngine;
using UnityEngine.InputSystem;

public class BattleInputController : MonoBehaviour
{
    private CombatInputActions input;
    private BattleManager battle;

    private void OnEnable()
    {
        battle = GetComponent<BattleManager>();
        if (battle == null)
        {
            Debug.LogError("[ERROR] BattleInputController: Missing BattleManager");
            enabled = false;
            return;
        }

        // INIT INPUT HERE (NOT IN AWAKE)
        input = new CombatInputActions();

        // ENABLE CORRECT ACTION MAP
        input.Battle.Enable();

        // REGISTER CALLBACKS
        input.Battle.BasicAttack.performed += OnBasicAttack;
        //input.Battle.OpenSkillMenu.performed += OnOpenSkillMenu;
        //input.Battle.OpenItemMenu.performed += OnOpenItemMenu;
        //input.Battle.Confirm.performed += OnConfirm;
        //input.Battle.Cancel.performed += OnCancel;
        input.Battle.NextTarget.performed += OnNextTarget;
        input.Battle.PrevTarget.performed += OnPrevTarget;
        //input.Battle.Parry.performed += OnParry;

        Debug.Log("[OK] BattleInputController ENABLED");
    }

    private void OnDisable()
    {
        if (input == null) return;

        input.Battle.BasicAttack.performed -= OnBasicAttack;
        //input.Battle.OpenSkillMenu.performed -= OnOpenSkillMenu;
        //input.Battle.OpenItemMenu.performed -= OnOpenItemMenu;
        //input.Battle.Confirm.performed -= OnConfirm;
        //input.Battle.Cancel.performed -= OnCancel;
        input.Battle.NextTarget.performed -= OnNextTarget;
        input.Battle.PrevTarget.performed -= OnPrevTarget;
        //input.Battle.Parry.performed -= OnParry;

        input.Battle.Disable();
        input.Dispose();
        input = null;

        Debug.Log("[STOP] BattleInputController DISABLED");
    }

    // INPUT CALLBACKS

    private void OnBasicAttack(InputAction.CallbackContext ctx)
    {
        Debug.Log("Input: BasicAttack");
        battle.SelectBasicAttack();
    }

    private void OnNextTarget(InputAction.CallbackContext ctx)
    {
        Debug.Log("Input: NextTarget");
        battle.ChangeTargetInput(1);
    }

    private void OnPrevTarget(InputAction.CallbackContext ctx)
    {
        Debug.Log("Input: PrevTarget");
        battle.ChangeTargetInput(-1);
    }

}
