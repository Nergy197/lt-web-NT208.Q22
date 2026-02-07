using UnityEngine;

public class BattleInputController : MonoBehaviour
{
    private BattleManager battle;

    private void Awake()
    {
        battle = GetComponent<BattleManager>();
        if (battle == null)
        {
            Debug.LogError("Missing BattleManager");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!battle.CanAcceptInput)
            return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            battle.SelectBasicAttack();
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            battle.ChangeTargetInput(+1);
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            battle.ChangeTargetInput(-1);
        }
    }
}
