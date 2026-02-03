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
        if (turnOrder == null || turnOrder.Count == 0) return null;

        int startIndex = turnOrder.IndexOf(current);
        // Nếu không tìm thấy current (ví dụ mới bắt đầu), bắt đầu từ -1 để loop check từ 0
        if (startIndex < 0) startIndex = -1;

        int count = turnOrder.Count;
        // Loop để tìm người còn sống tiếp theo
        for (int i = 1; i <= count; i++)
        {
            int nextIndex = (startIndex + i) % count;
            Status candidate = turnOrder[nextIndex];

            // Chỉ trả về nếu còn sống
            if (candidate.IsAlive) 
                return candidate;
        }

        // Nếu tất cả đều chết (trừ người hiện tại?), return null hoặc xử lý Endgame
        return null;
    }
}
