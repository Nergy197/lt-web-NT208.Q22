using UnityEngine;

public class BattleRunner : MonoBehaviour
{
    public static BattleRunner Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
