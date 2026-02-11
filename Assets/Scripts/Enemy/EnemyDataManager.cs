using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý liên kết giữa EnemyData và EnemyAttackData trên cùng một GameObject
/// </summary>
public class EnemyDataManager : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private List<EnemyAttackData> enemyAttackDataList = new List<EnemyAttackData>();

    public EnemyData GetEnemyData() => enemyData;
    
    public List<EnemyAttackData> GetEnemyAttackDataList() => enemyAttackDataList;
    
    public void SetEnemyData(EnemyData data)
    {
        enemyData = data;
    }
    
    public void AddAttackData(EnemyAttackData attackData)
    {
        if (!enemyAttackDataList.Contains(attackData))
        {
            enemyAttackDataList.Add(attackData);
        }
    }
    
    public void RemoveAttackData(EnemyAttackData attackData)
    {
        enemyAttackDataList.Remove(attackData);
    }

    private void OnValidate()
    {
        // Kiểm tra trong Editor để đảm bảo có dữ liệu
        if (enemyData == null)
        {
            Debug.LogWarning($"GameObject '{gameObject.name}' chưa có EnemyData được gán!", gameObject);
        }
        if (enemyAttackDataList.Count == 0)
        {
            Debug.LogWarning($"GameObject '{gameObject.name}' chưa có EnemyAttackData nào!", gameObject);
        }
    }
}
