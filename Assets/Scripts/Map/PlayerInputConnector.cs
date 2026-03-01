using UnityEngine;

public class PlayerInputConnector : MonoBehaviour
{
    private void Start()
    {
        Connect();
    }

    private void Connect()
    {
        if (InputController.Instance == null)
        {
            Debug.LogError("[PLAYER] InputController NULL");
            return;
        }

        InputController.Instance.SetMode(InputMode.Map);

        Debug.Log("[PLAYER] Input connected → MAP mode");
    }
}