using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "MapAction/Battle")]
public class BattleAction : MapAction
{
    public List<EnemyData> enemies =
        new List<EnemyData>();

    public int mapLevel = 1;


    public override void Execute()
    {
        if (enemies == null ||
            enemies.Count == 0)
        {
            Debug.LogError(
            "BattleAction: No enemies!");

            return;
        }

        if (MapManager.Instance == null)
        {
            Debug.LogError("BattleAction: MapManager.Instance == null, không thể bắt đầu battle.");
            return;
        }

        MapManager.Instance
            .currentEnemies.Clear();


        foreach (var e in enemies)
        {
            if (e == null) continue;
            MapManager.Instance
                .currentEnemies.Add(e);
        }

        if (MapManager.Instance.currentEnemies.Count == 0)
        {
            Debug.LogError("BattleAction: Tất cả enemy data null, bỏ qua.");
            return;
        }

        MapManager.Instance
            .currentMapLevel = mapLevel;


        MapManager.Instance
            .StartBattle();
    }
}