using System.Collections.Generic;
using UnityEngine;

public static class DataFactory
{
    public static PlayerStatus CreatePlayerFromData(PlayerData data)
    {
        if (data == null) return null;
        return data.CreateStatus();
    }

    public static EnemyStatus CreateEnemyFromData(EnemyData data)
    {
        if (data == null) return null;
        return data.CreateStatus();
    }

    public static EnemyAttack CreateEnemyAttackFromData(EnemyAttackData data)
    {
        if (data == null) return null;
        return data.CreateInstance();
    }

    public static PlayerAttack CreatePlayerAttackFromData(PlayerAttackData data)
    {
        if (data == null) return null;
        return data.CreateInstance();
    }
}
