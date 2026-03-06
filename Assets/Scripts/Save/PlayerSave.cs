using System.Collections.Generic;

[System.Serializable]
public class PlayerSave
{
    public string _id;

    public List<UnitSave> party;

    // Vị trí Save Point đã lưu gần nhất
    public string lastSaveScene;
    public string lastSavePointId;
}