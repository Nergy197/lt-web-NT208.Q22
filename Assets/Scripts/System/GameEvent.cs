public enum GameEvent
{
    // ======= BATTLE =======
    BattleStart,        // payload: null
    BattleWin,          // payload: null
    BattleLose,         // payload: null
    BattleFlee,         // payload: null
    AttackFinished,     // payload: null

    // ======= UNIT =======
    UnitDied,           // payload: Status (unit đã chết)
    UnitRevived,        // payload: Status (unit hồi sinh)
    UnitLevelUp,        // payload: PlayerStatus (unit vừa lên cấp)

    // ======= STATUS EFFECT =======
    EffectApplied,      // payload: StatusEffect
    EffectExpired,      // payload: StatusEffect

    // ======= MAP =======
    MapEntered,         // payload: Mapdata
    EncounterTriggered, // payload: null
}
