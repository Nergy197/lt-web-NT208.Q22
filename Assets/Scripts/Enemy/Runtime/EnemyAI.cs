using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Tạo thành menu để dễ tạo file mới trong Project
public abstract class EnemyAI : ScriptableObject
{
    public abstract void CalculateAction(BattleManager bm, EnemyStatus self, Party playerParty, Party enemyParty);
}