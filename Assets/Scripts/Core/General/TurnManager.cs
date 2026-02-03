using System.Collections.Generic;
using System.Linq;

public class TurnManager
{
    public Queue<Status> BuildTurnOrder(Party playerParty, Party enemyParty)
    {
        List<Status> allUnits = new List<Status>();

        allUnits.AddRange(playerParty.Members);
        allUnits.AddRange(enemyParty.Members);

        var sortedUnits = allUnits
            .Where(s => s.IsAlive)
            .OrderByDescending(s => s.Spd)
            .ToList();

        return new Queue<Status>(sortedUnits);
    }
}
