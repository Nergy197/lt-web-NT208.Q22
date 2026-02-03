using System.Collections.Generic;
using System.Linq;

public class TurnManager
{
    //Trả về danh sách lượt hiện tại (đã sort, đã loại người chết)
    public List<Character> BuildTurnOrder(Party playerParty, Party enemyParty)
    {
        List<Character> result = new List<Character>();

        result.AddRange(playerParty.Members);
        result.AddRange(enemyParty.Members);

        return result
            .Where(c => c.IsAlive)
            .OrderByDescending(c => c.spd)
            .ToList();
    }

    //Lấy nhân vật đi sau current
    public Character GetNext(Character current, List<Character> turnOrder)
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
