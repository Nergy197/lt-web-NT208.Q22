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


        MapManager.Instance
            .currentEnemies.Clear();


        foreach (var e in enemies)
        {
            MapManager.Instance
                .currentEnemies.Add(e);
        }


        MapManager.Instance
            .currentMapLevel = mapLevel;


        MapManager.Instance
            .StartBattle();
    }
}