[System.Serializable]
public class UnitSave
{
    public string entityName;

    public int level;

    public int currentHP;

    /// <summary>EXP hiện tại (dùng để không mất progress khi load game).</summary>
    public int currentExp;
}