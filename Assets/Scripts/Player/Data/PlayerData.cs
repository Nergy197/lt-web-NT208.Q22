using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/PlayerData")]
public class PlayerData : ScriptableObject
{
    public string entityName = "New Player";
    public int baseHP = 30;
    public int baseAtk = 8;
    public int baseDef = 2;
    public int baseSpd = 3;
    public int maxAP = 100;

    public PlayerStatus CreateStatus()
    {
        var p = new PlayerStatus(entityName, baseHP, baseAtk, baseDef, baseSpd, maxAP);
        return p;
    }
}
