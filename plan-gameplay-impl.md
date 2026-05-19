# Kế hoạch triển khai Gameplay — Chapter 1 (Q001–Q008)

Tài liệu này mô tả chi tiết **cách làm trong Unity Editor** cho từng quest, quyết định về scene, cách fix race condition đã phân tích, và thứ tự triển khai an toàn.

---

## 1. Sơ đồ scene & flow

```
StartScene
  └─ New Game → Chapter1_CutScene
                  Q001 hoàn thành → auto open Q002

Chapter1_CutScene → (cutscene kết thúc) → MapScene
MapScene                                      [Q002: O1–O4]
  └─ portal/trigger "ra trận" → BattleScene   [Q003: O1–O4, thua bắt buộc]
BattleScene (thua) → Chapter1_Aftermath       [Q004–Q007 pre-battle]
Chapter1_Aftermath → BattleScene              [Q007: O3, thắng]
BattleScene (thắng) → Chapter1_Aftermath      [Q007: O4–O5]
Chapter1_Aftermath → Chapter1_Ending          [Q008: O1–O5]
```

### Scenes cần tạo mới

| Scene | Mục đích |
|---|---|
| `Chapter1_Aftermath` | Hậu trận + đường rút + vườn không (Q004–Q007 pre/post) |
| `Chapter1_Ending` | Kết chương (Q008) |

Thêm cả hai vào **File → Build Settings → Scenes In Build** ngay khi tạo.

---

## 2. Fix race condition Q003 (PHẢI làm trước)

### Vấn đề
`BattleManager.EndBattle()` (khi thua) gọi theo thứ tự:
1. `EventManager.Publish(BattleLose)` → `BattleQuestTrigger.onLoseActions` → `LoadScene("Chapter1_Aftermath")`
2. `MapManager.EndBattle(false)` → `GameManager.RespawnAtSavePoint()` → `LoadScene(pendingSaveScene)`

Hai `LoadScene` trong cùng frame → Unity load scene **cuối cùng** (`pendingSaveScene`), Chapter1_Aftermath bị bỏ qua.

### Giải pháp: Dùng `pendingSaveScene` thay `LoadScene` trong onLoseActions

**Bước 1** — Trước khi vào trận Q003 (ở MapScene, trigger dẫn vào BattleScene), gọi:
```csharp
// Trong script trigger lên BattleScene cho Q003
GameManager.Instance.SetSavePoint("Chapter1_Aftermath", "aftermath_start");
MapManager.Instance.StartBattle(enemies);
```

`SetSavePoint` lưu `pendingSaveScene = "Chapter1_Aftermath"` và `pendingSavePointId = "aftermath_start"`. Khi thua, `RespawnAtSavePoint()` sẽ tự load đúng scene.

**Bước 2** — `BattleQuestTrigger.onLoseActions` cho Q003 chỉ có `CompleteObjective`, **không** có `LoadScene`:

| Order | TriggerOn | Action | Quest | ObjectiveId |
|---|---|---|---|---|
| 1 | OnBattleLoss | CompleteObjective | Q003 | O4 |

`LoadScene` không cần — `RespawnAtSavePoint()` lo.

**Bước 3** — Trong `Chapter1_Aftermath`, đặt một `SavePoint` với `pointId = "aftermath_start"` tại vị trí bắt đầu của Q004 (nơi Tuấn "hồi tỉnh"). `PlayerMovement.Start()` tự đọc `pendingSavePointId` và teleport đúng chỗ.

---

## 3. Components mới cần tạo

### 3.1 `ScriptedBattleController.cs`

Dùng cho Q003 (forced loss). Gắn vào một GameObject trong BattleScene.

```
Fields (Inspector):
  int forceLossAfterPlayerTurns = 3   // sau bao nhiêu lượt player thì force thua
  List<QuestAction> midBattleActions  // optional: complete O1/O2/O3 giữa trận
```

Logic:
- `Start()`: subscribe vào event lượt player (hoặc đếm trong Update theo cờ từ BattleManager)
- Sau `forceLossAfterPlayerTurns` lượt: gọi `ForceScriptedLoss()` (xem 3.2)
- `midBattleActions` dùng `QuestAction.Execute(..., When.Manual)` tại các mốc turn

### 3.2 Thêm `ForceScriptedLoss()` vào `BattleManager`

```csharp
/// <summary>
/// Kết thúc trận theo kịch bản thua — dùng cho chiến sự scripted (Q003).
/// Gọi từ ScriptedBattleController.
/// </summary>
public void ForceScriptedLoss()
{
    if (!waitingForPlayerAction && !waitingForAttackFinish) return;
    playerWon  = false;
    playerFled = false;
    StopAllCoroutines();
    EndBattle();
}
```

Thêm method này vào `BattleManager.cs` (public, không thay đổi flow khác).

### 3.3 `Q003BattlePhase.cs`

Script nhỏ trong BattleScene hoàn thành O1–O3 theo mốc thời gian / lượt:

```
Start()         → StartCoroutine(TrackPhases())
TrackPhases():
  yield 0 frames         → CompleteObjective("Q003", "O1")   // vào trận
  WaitForTurn(1)         → CompleteObjective("Q003", "O2")   // sau lượt 1
  WaitForTurn(2)         → CompleteObjective("Q003", "O3")   // sau lượt 2
  (O4 do BattleQuestTrigger.onLoseActions lo)
```

Không phụ thuộc event — chỉ cần BattleManager.Instance tồn tại.

---

## 4. Triển khai từng quest

### Q001 — Di nguyện của cha (`Chapter1_CutScene`)

**ObjectiveId đặc biệt**: `"01"` (không phải `"O1"` — asset dùng số không).

**Hook thoại** — mở `Assets/Scripts/CutScene/Chapter1/QuanLyHoiThoai.cs`, tìm hàm kết thúc hội thoại (`KetThucThoai` hoặc chỗ set `daXongHetKichBan = true`), thêm vào:

```csharp
QuestManager.Instance?.CompleteObjective("Q001", "01");
```

Sau khi Q001 complete, `NextQuests` tự mở Q002. Scene tiếp theo cần có `QuestUI` để hiển thị.

**Scene transition**: Ở cuối Chapter1_CutScene, load `MapScene` (đã có sẵn flow).

---

### Q002 — Tiếng trống biên ải (`MapScene`)

Tạo một zone/khu "Trại quân" riêng trong MapScene. Bố trí theo thứ tự:

#### O1 — Đến điểm họp quân
- `QuestZoneTrigger`, `triggerOnce = true`
- `onEnterZoneActions`:

| TriggerOn | Action | Quest | ObjectiveId |
|---|---|---|---|
| OnEnterZone | CompleteObjective | Q002 | O1 |

#### O2 — Nghe báo cáo biên ải
- NPC "Lính thám báo", gắn `NpcTrigger` + list `questActions`
- Cuối thoại (NpcTrigger tự gọi `QuestAction.Execute(..., When.OnDialogueEnd)` ở `EndDialogue()`) → cần hook:
  - Mở `NpcTrigger.cs`, trong `EndDialogue()` thêm `QuestAction.Execute(questActions, QuestAction.When.OnDialogueEnd);`
  - Inspector action: `TriggerOn=OnDialogueEnd, CompleteObjective, Q002, O2`

> **Lưu ý**: `NpcTrigger.EndDialogue()` hiện **chưa** gọi `QuestAction.Execute`. Cần thêm 1 dòng này — tương tự cho tất cả NPC dùng quest objectives.

#### O3 — Gặp tướng đồng minh
- NPC "Tướng đồng minh", cùng pattern O2
- Inspector: `Q002 / O3`

#### O4 — Nhận lệnh xuất phát
- NPC "Chủ soái" hoặc vật thể "Lệnh bài"
- Cuối thoại: `Q002 / O4`
- Sau O4, Q002 complete → Q003 active

#### Trigger vào BattleScene (Q003)
- Tạo `EnemyTrigger` hoặc portal riêng label "Ra trận"
- Trước khi gọi `MapManager.StartBattle()`, gọi:
  ```csharp
  GameManager.Instance.SetSavePoint("Chapter1_Aftermath", "aftermath_start");
  ```
- Đây là bước bắt buộc để fix race condition Q003

---

### Q003 — Trận chiến đầu tiên (`BattleScene`)

#### Setup BattleScene cho Q003

Tạo GameObject `Q003_BattleController` trong BattleScene, gắn 3 component:

**Component 1: `BattleQuestTrigger`**

`onLoseActions`:

| Order | TriggerOn | Action | Quest | ObjectiveId |
|---|---|---|---|---|
| 1 | OnBattleLoss | CompleteObjective | Q003 | O4 |

`onWinActions` và `onAnyEndActions`: để trống (trận này không có win path).

**Component 2: `Q003BattlePhase`** (script mới — xem 3.3)
- Tự complete O1, O2, O3 theo mốc turn

**Component 3: `ScriptedBattleController`** (script mới — xem 3.1)
- `forceLossAfterPlayerTurns = 3` (hoặc 4 tùy cảm giác chiến đấu)
- Sau đủ lượt: gọi `BattleManager.Instance.ForceScriptedLoss()`

#### Cơ chế thua bắt buộc — lý do dùng ScriptedBattleController thay buff tĩnh

Buff địch cứng dễ bị player hạ nếu level cao (testing, debug). `ScriptedBattleController` đảm bảo trận luôn kết thúc đúng lúc bất kể stats, giữ nhịp narrative.

#### Lưu ý EXP
`BattleManager.EndBattle()` chỉ tính EXP khi `playerWon`. Khi `ForceScriptedLoss()` set `playerWon = false`, player không nhận EXP từ trận này — đúng về narrative (thất bại, không có gì để mừng).

---

### Q004 — Lệnh rút lui (`Chapter1_Aftermath`)

#### Setup scene `Chapter1_Aftermath`

Scene có 1 bản đồ lớn chia zone rõ ràng. Đặt `PlayerInputConnector` ở root để set `InputMode.Map` khi load.

**SavePoint bắt buộc**: `pointId = "aftermath_start"` — đặt ở vị trí Tuấn "hồi tỉnh" sau trận. Đây là điểm respawn khi Q003 kết thúc.

#### O1 — Hồi tỉnh / đứng dậy
- `QuestZoneTrigger` tại điểm SavePoint `aftermath_start`
- OnEnterZone: `Q004 / O1`
- Có thể kết hợp cutscene ngắn (fade in, âm thanh chiến trường)

#### O2 — Điểm quan sát A (dân bị thương)
- `QuestZoneTrigger` ở khu dân bị thương (cách start ~20–30 units)
- OnEnterZone: `Q004 / O2`

#### O3 — Điểm quan sát B (binh đã tử trận)
- `QuestZoneTrigger` ở khu chiến trường đổ nát
- OnEnterZone: `Q004 / O3`

#### O4 — Cứu binh lính còn sống (nhóm 1)
- NPC "Binh sĩ bị thương A" — `NpcTrigger` + questActions
- Cuối thoại: `Q004 / O4`

#### O5 — Cứu binh lính còn sống (nhóm 2)
- NPC "Binh sĩ bị thương B" — cùng pattern
- Cuối thoại: `Q004 / O5`

#### O6 — Truyền lệnh rút lui
- NPC "Tướng lĩnh cận vệ" hoặc object "Trống hiệu lệnh"
- Interact: `Q004 / O6`

#### O7 — Dẫn tàn quân tới điểm tập kết
- `QuestZoneTrigger` ở cuối bản đồ (exit zone)
- OnEnterZone: `Q004 / O7`
- Q004 complete → Q005 tự active (NextQuests chain)

---

### Q005 — Dấu tro trên đường rút (`Chapter1_Aftermath`, tiếp theo Q004)

Cùng scene, mở khóa zone tiếp theo khi Q005 active (dùng cổng/collider ẩn enable theo quest state, hoặc đơn giản là bố trí tuyến tính không cần khóa).

#### O1 — Rời trận địa, bước vào tuyến đường rút
- `QuestZoneTrigger` ở cổng ra khỏi khu Q004
- OnEnterZone: `Q005 / O1`

#### O2 — Đi qua đoạn đường cháy / đổ nát
- `QuestZoneTrigger` ở giữa đường (landscape cháy nát)
- OnEnterZone: `Q005 / O2`

#### O3 — Gặp dân sống sót (nhóm 1)
- NPC "Người dân A", thoại ngắn
- Cuối thoại: `Q005 / O3`

#### O4 — Gặp dân sống sót (nhóm 2)
- NPC "Người dân B"
- Cuối thoại: `Q005 / O4`

#### O5 — Đi qua làng vắng
- `QuestZoneTrigger` ở làng bỏ hoang (không có NPC)
- OnEnterZone: `Q005 / O5`

#### O6 — Đến điểm dừng chân / trại tạm
- `QuestZoneTrigger` ở cuối zone Q005
- OnEnterZone: `Q005 / O6`
- Q005 complete → Q006 active

---

### Q006 — Vườn không nhà trống (`Chapter1_Aftermath`, tiếp theo Q005)

#### O1 — Gặp chỉ huy, nhận kế hoạch vườn không
- NPC "Chỉ huy chiến lược"
- Cuối thoại: `Q006 / O1`

#### O2 — Làng A — hoàn tất sơ tán
- `QuestZoneTrigger` tại Làng A
- OnEnterZone: `Q006 / O2` (hoặc NPC làng + thoại xác nhận)

#### O3 — Kho lương B — phá/đốt/bỏ
- Object tương tác "Kho lương" (`MapTrigger` hoặc `NpcTrigger` cấu hình như vật thể)
- requireInteract = true
- Interact: `Q006 / O3`

#### O4 — Tránh giao chiến lớn / đi an toàn
- `QuestZoneTrigger` waypoint an toàn
- OnEnterZone: `Q006 / O4`

#### O5 — Báo cáo hoàn tất cho chỉ huy
- NPC "Chỉ huy" (cùng NPC O1 hoặc NPC khác ở cuối zone)
- Cuối thoại: `Q006 / O5`
- Q006 complete → Q007 active

---

### Q007 — Đông Bộ Đầu (`Chapter1_Aftermath` + `BattleScene`)

#### O1 — Đến điểm hội quân trước trận
- `QuestZoneTrigger` tại vị trí hội quân (zone mới trong Chapter1_Aftermath)
- OnEnterZone: `Q007 / O1`

#### O2 — Gặp chỉ huy cánh quân / nhận vai trò
- NPC "Chỉ huy Đông Bộ Đầu"
- Cuối thoại: `Q007 / O2`

#### O3 — Trận Đông Bộ Đầu (thắng)
- Trigger "Vào trận" → `MapManager.StartBattle(enemies)`
- **Không cần** `SetSavePoint` đặc biệt (trận này thắng → `MapManager.EndBattle(true)` load `MapScene` hoặc return về Chapter1_Aftermath)
- Cần override về Chapter1_Aftermath sau thắng: thêm `BattleQuestTrigger.onWinActions`:

| Order | TriggerOn | Action | Quest | ObjectiveId / SceneName |
|---|---|---|---|---|
| 1 | OnBattleWin | CompleteObjective | Q007 | O3 |
| 2 | OnBattleWin | LoadScene | — | Chapter1_Aftermath |

Vì `MapManager.EndBattle(true)` load `MapScene`, cần đè bằng `LoadScene` trong `onWinActions`. Đây là trường hợp LoadScene hợp lệ vì `MapManager.EndBattle(true)` dùng `SceneManager.LoadScene("MapScene")` — cũng là 1 LoadScene. Cả hai cùng frame → `BattleQuestTrigger` fire trước (synchronous event) → rồi MapManager load MapScene. Scene cuối cùng load là MapScene. **Vấn đề tương tự Q003 nhưng chiều ngược.**

**Giải pháp Q007 win**: Thêm flag `skipMapManagerLoad` vào `BattleQuestTrigger` (bool Inspector), khi true thì `BattleManager.EndBattle()` bỏ qua `MapManager.EndBattle()`. Implement bằng cách thêm:

```csharp
// BattleQuestTrigger.cs — thêm field:
[Tooltip("Nếu true, bỏ qua MapManager.EndBattle() — dùng khi BattleQuestTrigger tự load scene")]
public bool overrideSceneTransition = false;

void OnEnable() {
    // ... subscribe như cũ
    if (overrideSceneTransition)
        BattleManager.SetSceneOverride(true);
}
```

```csharp
// BattleManager.cs — thêm:
public static bool SceneOverride { get; private set; }
public static void SetSceneOverride(bool v) => SceneOverride = v;

// Trong EndBattle(), thay:
if (MapManager.Instance != null)
    MapManager.Instance.EndBattle(playerWon);
// Thành:
if (!SceneOverride && MapManager.Instance != null)
    MapManager.Instance.EndBattle(playerWon);
SceneOverride = false; // reset
```

Set `overrideSceneTransition = true` trên BattleQuestTrigger của Q007. Q003 không cần (đã dùng RespawnAtSavePoint).

#### O4 — Điểm quan sát chứng kiến giặc rút
- `QuestZoneTrigger` trong Chapter1_Aftermath (sau khi load lại từ BattleScene)
- OnEnterZone: `Q007 / O4`

#### O5 — Báo cáo tóm tắt
- NPC "Cận vệ" hoặc object "Bản đồ chiến sự"
- Cuối thoại: `Q007 / O5`
- Q007 complete → Q008 active
- Tại đây: thêm `LoadScene` action: `Chapter1_Ending`

---

### Q008 — Hai con đường (`Chapter1_Ending`)

Scene tĩnh, ít di chuyển, thiên về atmosphere. Không có `BattleScene`.

`BranchChoices` hiện trống — **chưa thiết kế branching mechanic**. Triển khai O1–O5 tuyến tính; O5 kết thúc chương.

#### O1 — Về địa điểm an toàn
- `QuestZoneTrigger` tại nơi Tuấn về nghỉ
- OnEnterZone: `Q008 / O1`

#### O2 — Gặp người thân cận
- NPC (Yết Kiêu, Dã Tượng, hoặc gia nhân)
- Cuối thoại: `Q008 / O2`

#### O3 — Điểm một mình — hồi tưởng
- `QuestZoneTrigger` ở góc vắng (đỉnh đồi, bờ sông)
- OnEnterZone: `Q008 / O3`
- Có thể trigger cutscene flash-back ngắn

#### O4 — Nhật ký / suy ngẫm (di nguyện vs non sông)
- Object tương tác "Cuốn nhật ký" hoặc "Bức thư cha"
- requireInteract = true
- Interact: `Q008 / O4`

#### O5 — Kết chương
- `QuestZoneTrigger` ở exit của scene (fade out + title card)
- OnEnterZone: `Q008 / O5`
- Q008 complete → **Chapter 1 kết thúc**
- Tại đây: transition về `StartScene` hoặc credits (chưa xác định)

---

## 5. Sửa `NpcTrigger.cs` — hook QuestAction

`NpcTrigger.EndDialogue()` hiện chưa gọi `QuestAction.Execute`. Thêm vào:

```csharp
void EndDialogue()
{
    _isTalking = false;
    HideDialogue();

    if (!_triggeredOnce)
    {
        _triggeredOnce = true;
        QuestAction.Execute(questActions, QuestAction.When.OnDialogueEnd); // ← thêm dòng này
    }
}
```

**Quan trọng**: Hiện tại `questActions` chỉ fire `When.OnDialogueEnd` trong nhánh `!_triggeredOnce`. Nếu NPC có `triggerOnce = false` và cần complete quest ở lần thoại đầu, logic này đúng. Nếu muốn fire mỗi lần thoại (quest repeatable), điều chỉnh thêm.

---

## 6. Thứ tự triển khai an toàn

```
Bước 1  Thêm ForceScriptedLoss() vào BattleManager
Bước 2  Thêm SceneOverride flag vào BattleManager
Bước 3  Sửa NpcTrigger.EndDialogue() (1 dòng)
Bước 4  Tạo ScriptedBattleController.cs + Q003BattlePhase.cs
Bước 5  Q001: hook CompleteObjective trong QuanLyHoiThoai
Bước 6  MapScene: tạo zone Q002 + NPC O1–O4 + trigger Q003 (với SetSavePoint)
Bước 7  BattleScene: tạo Q003_BattleController GameObject + 3 components
Bước 8  Tạo Chapter1_Aftermath, đặt SavePoint aftermath_start
Bước 9  Bố trí zones + NPC cho Q004, Q005, Q006 trong Chapter1_Aftermath
Bước 10 Q007: trigger vào BattleScene + BattleQuestTrigger với overrideSceneTransition=true
Bước 11 Tạo Chapter1_Ending, bố trí Q008
Bước 12 Build Settings: thêm Chapter1_Aftermath, Chapter1_Ending
Bước 13 Tools → Quest System → 4. Kiểm tra lỗi
```

---

## 7. Bảng kiểm tra nhanh

| # | Việc | Pass? |
|---|---|---|
| 1 | StartScene → New Game → Q001 active | |
| 2 | Cuối CutScene → Q001 complete ("01"), Q002 hiện trên QuestUI | |
| 3 | MapScene → đến zone O1 → Q002/O1 check | |
| 4 | Nói NPC O2, O3, O4 lần lượt → Q002 complete, Q003 active | |
| 5 | SetSavePoint("Chapter1_Aftermath","aftermath_start") được gọi trước StartBattle | |
| 6 | Vào BattleScene → Q003/O1–O3 check dần | |
| 7 | Sau 3 lượt → ForceScriptedLoss() fire → Q003/O4 check, Q004 active | |
| 8 | Scene load: Chapter1_Aftermath tại aftermath_start (không phải MapScene) | |
| 9 | Q004/O1–O7 check đúng thứ tự khi đi theo tuyến | |
| 10 | Q005/O1–O6 và Q006/O1–O5 chain đúng | |
| 11 | Q007: vào BattleScene → thắng → Q007/O3 check → load Chapter1_Aftermath | |
| 12 | Q007/O4–O5 check → Q008 active → load Chapter1_Ending | |
| 13 | Q008/O1–O5 check lần lượt → Q008 complete | |

---

## 8. Tham chiếu file

| File | Việc cần sửa |
|---|---|
| `Assets/Scripts/Battle/Runtime/BattleManager.cs` | Thêm `ForceScriptedLoss()`, `SceneOverride` flag |
| `Assets/Scripts/Map/NpcTrigger.cs` | Thêm `QuestAction.Execute` trong `EndDialogue()` |
| `Assets/Scripts/Quest/BattleQuestTrigger.cs` | Thêm `overrideSceneTransition` bool |
| `Assets/Scripts/CutScene/Chapter1/QuanLyHoiThoai.cs` | Hook `CompleteObjective("Q001","01")` |
| *(mới)* `Assets/Scripts/Battle/Runtime/ScriptedBattleController.cs` | Forced loss controller |
| *(mới)* `Assets/Scripts/Battle/Runtime/Q003BattlePhase.cs` | Complete O1–O3 theo turn |
