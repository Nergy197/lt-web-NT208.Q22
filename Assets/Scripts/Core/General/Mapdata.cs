using UnityEngine;

[CreateAssetMenu(fileName = "New Map Data", menuName = "Game/Map Data")]
public class MapData : ScriptableObject
{
    public string mapName;
    
    [Header("Difficulty Settings")]
    [Tooltip("Đây chính là Dummy Level - Level cố định của quái trong map này")]
    public int recommendedLevel = 5; 
    
    // Bạn có thể thêm: list quái xuất hiện ở map này, nhạc nền, background...
    public List<EnemyStatus> enemyPool; 
}