using System.Collections.Generic;

[System.Serializable]
public class PlayerSave
{
    public string _id;
    public int slotId = 0;
    public string saveTime;

    public List<UnitSave> party;

    // Vị trí Save Point đã lưu gần nhất
    public string lastSaveScene;
    public string lastSavePointId;

    // Tiến trình quest (lưu lên server)
    public AllQuestsSaveData questProgress;
}