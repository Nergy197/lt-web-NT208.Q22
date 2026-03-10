using UnityEngine;

[CreateAssetMenu(menuName = "MapAction/ChangeMap")]
public class ChangeMapAction : MapAction
{
    [Header("Next Map Data")]
    public Mapdata targetMap;

    public override void Execute()
    {
        if (targetMap == null)
        {
            Debug.LogError("[ChangeMapAction] MapData is null!");
            return;
        }

        if (MapManager.Instance != null && MapManager.Instance.currentMap != targetMap)
        {
            MapManager.Instance.SetMap(targetMap);
            Debug.Log($"[ChangeMapAction] Changed MapData to: {targetMap.mapName}");
        }
    }
}
