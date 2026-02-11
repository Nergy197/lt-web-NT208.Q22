using UnityEngine;
using System.Collections.Generic;

public class PlayerDataManager : MonoBehaviour
{
    [Header("Base Data")]
    [SerializeField] private PlayerData playerData;

    [Header("Attack Data")]
    [SerializeField] private List<PlayerAttackData> playerAttackDataList = new();

    private PlayerAttackData basicAttack;

    // ================= CREATE RUNTIME STATUS =================
    public PlayerStatus CreateStatus()
    {
        if (playerData == null)
        {
            Debug.LogError("[PlayerDataManager] Missing PlayerData");
            return null;
        }

        PlayerStatus status = playerData.CreateStatus();

        // Gán toàn bộ skill data
        foreach (var atkData in playerAttackDataList)
        {
            status.AddSkill(atkData);
        }

        // KHÔNG cần SetBasicAttack
        // BasicAttack sẽ tự nhận skill có apCost == 0

        return status;
    }


    // ================= BASIC ATTACK =================
    private void DetectBasicAttack()
    {
        basicAttack = null;

        foreach (var atk in playerAttackDataList)
        {
            if (atk != null && atk.apCost == 0)
            {
                basicAttack = atk;
                Debug.Log($"[PlayerDataManager] BasicAttack = {atk.attackName}", gameObject);
                return;
            }
        }

        Debug.LogWarning(
            $"[PlayerDataManager] {name} has no AP=0 skill (Basic Attack)",
            gameObject
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (playerData == null)
            Debug.LogWarning($"[{name}] PlayerData not assigned", gameObject);

        if (playerAttackDataList.Count == 0)
            Debug.LogWarning($"[{name}] No PlayerAttackData assigned", gameObject);
    }
#endif
}
