using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý liên kết giữa PlayerData và PlayerAttackData trên cùng một GameObject
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private List<PlayerAttackData> playerAttackDataList = new List<PlayerAttackData>();

    public PlayerData GetPlayerData() => playerData;
    
    public List<PlayerAttackData> GetPlayerAttackDataList() => playerAttackDataList;
    
    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
    }
    
    public void AddAttackData(PlayerAttackData attackData)
    {
        if (!playerAttackDataList.Contains(attackData))
        {
            playerAttackDataList.Add(attackData);
        }
    }
    
    public void RemoveAttackData(PlayerAttackData attackData)
    {
        playerAttackDataList.Remove(attackData);
    }

    private void OnValidate()
    {
        // Kiểm tra trong Editor để đảm bảo có dữ liệu
        if (playerData == null)
        {
            Debug.LogWarning($"GameObject '{gameObject.name}' chưa có PlayerData được gán!", gameObject);
        }
        if (playerAttackDataList.Count == 0)
        {
            Debug.LogWarning($"GameObject '{gameObject.name}' chưa có PlayerAttackData nào!", gameObject);
        }
    }
}
