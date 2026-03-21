public enum InputMode
{
    Map,
    Battle,
    BattleSkillMenu,
    BattleItemMenu,
    UI,         // Tắt toàn bộ input khi đang mở menu UI (Save Point, Shop, ...)
    Cutscene    // Chỉ bật Map.Interact để advance dialogue
}
