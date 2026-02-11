using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý liên kết giữa PlayerData và PlayerAttackData trên cùng một GameObject
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private List<PlayerAttackData> playerAttackDataList = new List<PlayerAttackData>();
    
    private PlayerAttackData basicAttack; // Skill tốn 0 AP (tự động nhận diện)

    public PlayerData GetPlayerData() => playerData;
    
    public List<PlayerAttackData> GetPlayerAttackDataList() => playerAttackDataList;
    
    public PlayerAttackData GetBasicAttack() => basicAttack;
    
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

    private void Awake()
    {
        // Tự động nhận diện basic attack (skill tốn 0 AP)
        AutoDetectBasicAttack();
    }

    private void AutoDetectBasicAttack()
    {
        basicAttack = null;
        
        foreach (var attackData in playerAttackDataList)
        {
            if (attackData.apCost == 0)
            {
                basicAttack = attackData;
                Debug.Log($"[PlayerDataManager] Đã xác định Basic Attack: {attackData.attackName}", gameObject);
                return;
            }
        }
        
        if (basicAttack == null)
        {
            Debug.LogWarning($"[PlayerDataManager] Không tìm thấy skill tốn 0 AP cho {playerData?.entityName ?? gameObject.name}", gameObject);
        }
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
        
        // Kiểm tra xem có skill tốn 0 AP hay không
        bool hasZeroAPSkill = playerAttackDataList.Exists(a => a.apCost == 0);
        if (!hasZeroAPSkill && playerAttackDataList.Count > 0)
        {
            Debug.LogWarning($"GameObject '{gameObject.name}' không có skill nào tốn 0 AP (Basic Attack)!", gameObject);
        }
    }
}
