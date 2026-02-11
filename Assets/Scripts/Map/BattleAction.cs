using UnityEngine;

[CreateAssetMenu(menuName = "MapAction/Battle")]
public class BattleAction : MapAction
{
    public EnemyData enemyData;
    public int mapLevel = 1;

    public override void Execute()
    {
        GameManager.Instance.StartBattle(enemyData, mapLevel);
    }
}
