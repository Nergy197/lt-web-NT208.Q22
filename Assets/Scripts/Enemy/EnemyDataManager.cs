using UnityEngine;
using System.Collections.Generic;

public class EnemyDataManager : MonoBehaviour
{
    [Header("Base Data")]
    [SerializeField] private EnemyData enemyData;

    [Header("Attack Data")]
    [SerializeField] private List<EnemyAttackData> enemyAttackDataList = new();

    // ================= CREATE STATUS =================
    public EnemyStatus CreateStatus()
    {
        if (enemyData == null)
        {
            Debug.LogError($"[EnemyDataManager] {name} missing EnemyData", gameObject);
            return null;
        }

        var status = enemyData.CreateStatus();

        foreach (var atk in enemyAttackDataList)
        {
            status.AddAttack(atk);
        }

        return status;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (enemyData == null)
            Debug.LogWarning($"[{name}] EnemyData not assigned", gameObject);

        if (enemyAttackDataList.Count == 0)
            Debug.LogWarning($"[{name}] No EnemyAttackData assigned", gameObject);
    }
#endif
}
