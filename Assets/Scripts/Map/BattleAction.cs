using UnityEngine;

[CreateAssetMenu(menuName = "MapAction/Battle")]
public class BattleAction : MapAction
{
    public EnemyData enemyData;
    public int mapLevel = 1;

    public override void Execute()
    {
        MapManager.Instance.StartBattle(enemyData, mapLevel);
    }
}
