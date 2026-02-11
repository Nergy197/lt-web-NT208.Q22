using System.Collections.Generic;

public class Reward
{
    public int expAmount = 0;
    public List<string> itemRewards = new List<string>();

    public Reward(int exp = 0)
    {
        expAmount = exp;
    }

    public void AddItem(string itemName)
    {
        if (!itemRewards.Contains(itemName))
            itemRewards.Add(itemName);
    }
}
