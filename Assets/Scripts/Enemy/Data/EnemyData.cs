using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string entityName = "New Enemy";
    public int baseHP = 20;
    public int baseAtk = 6;
    public int baseDef = 1;
    public int baseSpd = 2;

    public EnemyStatus CreateStatus()
    {
        var e = new EnemyStatus(entityName, baseHP, baseAtk, baseDef, baseSpd);
        return e;
    }
}
