using System.Collections.Generic;
using System.Linq;

public class TurnManager
{
    public List<Status> BuildTurnOrder(Party playerParty, Party enemyParty)
    {
        List<Status> result = new List<Status>();

        result.AddRange(playerParty.Members);
        result.AddRange(enemyParty.Members);

        return result
            .Where(s => s.IsAlive)
            .OrderByDescending(s => s.Spd)
            .ToList();
    }

    public Status GetNext(Status current, List<Status> turnOrder)
    {
        if (turnOrder == null || turnOrder.Count == 0)
            return null;

        int index = turnOrder.IndexOf(current);

        if (index < 0)
            return turnOrder[0];

        int nextIndex = (index + 1) % turnOrder.Count;
        return turnOrder[nextIndex];
    }
}
