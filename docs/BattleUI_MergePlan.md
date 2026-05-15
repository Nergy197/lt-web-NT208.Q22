# Kế Hoạch Merge UI Mới vào BattleScene

> Ngày tạo: 2026-05-15  
> Nhánh hiện tại: `main`  
> Trạng thái: Draft

---

## 1. Tổng Quan

Mục tiêu là tích hợp UI mới vào **BattleScene** sao cho hoạt động đúng với hệ thống `BattleManager` + `BattleUI` + **hệ thống chọn mục tiêu** đang có sẵn.

### Kiến trúc hiện tại (cần giữ nguyên)

```
BattleManager (singleton)
  ├── Quản lý lượt, spawn units, win/lose
  ├── Chọn mục tiêu: currentTargetIndex / currentAllyTargetIndex
  ├── targetCursor (world-space GameObject) — Update() mỗi frame
  └── Gọi BattleUI để hiển thị

BattleUI (singleton)
  ├── Subscribe GameEvents (BattleStart, BattleWin, BattleLose, BattleFlee, UnitDied)
  ├── Nhận gọi trực tiếp từ BattleManager
  └── Button callbacks → BattleManager

InputController
  ├── NextTarget  → BattleManager.ChangeTargetInput(+1)
  └── PrevTarget  → BattleManager.ChangeTargetInput(-1)
```

### Hệ thống chọn mục tiêu hiện tại — Tóm tắt nhanh

| Thành phần | File | Mô tả |
|-----------|------|-------|
| `currentTargetIndex` | BattleManager.cs:30 | Index enemy đang chọn |
| `currentAllyTargetIndex` | BattleManager.cs:31 | Index ally đang chọn |
| `targetCursor` | BattleManager.cs:39 | GameObject con trỏ world-space |
| `isTargetingAlly` | BattleManager.cs:42 | Flag enemy/ally (hiện **luôn = false**) |
| `ChangeTargetInput(dir)` | BattleManager.cs:536 | Cycle qua danh sách còn sống |
| `GetEnemyTarget()` | BattleManager.cs:504 | Trả về enemy hiện tại (auto-clamp) |
| `GetAllyTarget()` | BattleManager.cs:524 | Trả về ally hiện tại |
| `UnitHUD.SetHighlight()` | UnitHUD.cs:102 | Highlight HUD panel player đang hành động |
| Keyboard binding | InputController.cs:80 | NextTarget / PrevTarget key |

---

## 2. Hệ Thống InputController

### Kiến trúc

`InputController` là singleton `DontDestroyOnLoad` — tồn tại xuyên suốt scene, không bị destroy khi chuyển sang BattleScene.

```
InputController (singleton, DontDestroyOnLoad)
  ├── Mode hiện tại: Map | Battle | BattleSkillMenu | BattleItemMenu | UI | Cutscene
  ├── SetMode(mode) → disable tất cả action maps, enable đúng 1 map
  ├── BindBattleManager(bm)   → gán battle reference + SetMode(Battle)
  └── UnbindBattleManager()   → xóa reference + SetMode(Map)
```

### Luồng bind/unbind

```
BattleManager.Start() (coroutine)
  └── InputController.Instance.BindBattleManager(this)   ← mode chuyển sang Battle

BattleManager.EndBattle() / FailSafeBackToMap()
  └── InputController.Instance.UnbindBattleManager()     ← mode về Map
```

### Toàn bộ key binding hiện tại

**Mode: Battle** (`Input.Battle.*`)

| Action | Gọi đến | Ghi chú |
|--------|---------|---------|
| `BasicAttack` | `BattleManager.SelectBasicAttack()` | |
| `NextTarget` | `BattleManager.ChangeTargetInput(+1)` | Cycle enemy tiếp theo |
| `PrevTarget` | `BattleManager.ChangeTargetInput(-1)` | Cycle enemy trước đó |
| `Parry` | `BattleManager.RequestParry()` | |
| `OpenSkillMenu` | `SetMode(BattleSkillMenu)` | Chuyển sang SkillMenu mode |
| `OpenItemMenu` | `Debug.Log(...)` | **Chưa implement** |
| `Flee` | `BattleManager.TryFlee()` | |

**Mode: BattleSkillMenu** (`Input.SkillMenu.*`)

| Action | Gọi đến | Ghi chú |
|--------|---------|---------|
| `Skill1` | `BattleManager.UseSkill(0)` + `SetMode(Battle)` | |
| `Skill2` | `BattleManager.UseSkill(1)` + `SetMode(Battle)` | |
| `Cancel` | `SetMode(Battle)` | Đóng skill menu |

### Gap InputController

| Gap | Mức độ | Cần làm |
|-----|--------|---------|
| **Skill3 không có keyboard binding** | **Trung bình** | SkillMenu chỉ bind Skill1, Skill2. Skill3 chỉ dùng được qua button UI. Cần thêm `Skill3` action vào `GameInput.inputactions` và bind trong `BindSkillMenuInput()` |
| **OpenItemMenu chưa implement** | Thấp | Để P3 |
| **BattleItemMenu** enum tồn tại nhưng không có binding | Thấp | Để P3 |
| **InputController null khi test trực tiếp BattleScene** | **Trung bình** | `BattleSceneBootstrap` có tạo fallback không? Nếu không → keyboard input sẽ không hoạt động khi test standalone. Cần kiểm tra. |

### Quan hệ InputController ↔ UI mới

Khi UI mới thêm button bấm → `BattleUI` gọi thẳng `BattleManager`, **không qua InputController**.  
Keyboard input đi qua `InputController` → `BattleManager` trực tiếp, **không qua BattleUI**.

```
[Keyboard] → InputController → BattleManager
[Button]   → BattleUI       → BattleManager
```

Cả hai đường đều hợp lệ và không xung đột — chỉ cần đảm bảo mode đúng:
- Bấm nút "Skill" trên UI → gọi `BattleUI.OnSkillMenuOpen()` nhưng **không** gọi `SetMode(BattleSkillMenu)` → phím Skill1/Skill2 vẫn active nếu mode chưa đổi
- **Nên** đồng bộ: khi `BattleUI.OnSkillMenuOpen()` được gọi, cũng gọi `InputController.Instance?.SetMode(InputMode.BattleSkillMenu)`
- Tương tự `BattleUI.OnSkillMenuClose()` → `SetMode(InputMode.Battle)`

---

## 3. Phân Tích Gap — Chỗ UI Mới Cần Tích Hợp

### 2A. Gap đã xác nhận

| Gap | Vấn đề | Cần làm |
|-----|--------|---------|
| **Enemy HUD highlight** | `SetHighlight()` chỉ có cho player HUD, không có cho enemy HUD | Thêm highlight visual vào `enemyHUDs` khi target đổi |
| **UI không biết target thay đổi** | Khi bấm NextTarget/PrevTarget, cursor world-space đổi nhưng HUD enemy không phản hồi | Thêm callback hoặc polling trong BattleUI |
| **isTargetingAlly chưa dùng** | Skill heal/buff có `SkillEffectTarget.SelectedAlly` nhưng không có cách chuyển sang chọn ally | Cần UI nút hoặc auto-switch khi skill heal |
| **Click chuột vào enemy** | Hiện chỉ có keyboard cycling, không có click-to-target | Tuỳ chọn: thêm `PointerClickHandler` lên SpawnedModel |

### 2B. Những gì đã hoạt động, không cần đụng vào

- `targetCursor` world-space đã chạy ổn (Update loop)
- `ChangeTargetInput` cycling logic
- `GetEnemyTarget()` auto-clamp khi enemy chết
- `PlayerAttack.ResolveEffectTarget()` đã xử lý Self/Enemy/SelectedAlly
- `UnitHUD.SetHighlight()` cho player

---

## 3. Kế Hoạch Triển Khai

### Bước 0 — Chuẩn bị

- [ ] Commit tất cả thay đổi chưa commit
- [ ] Backup: `Assets/Scenes/BattleScene.unity` → `BattleScene_backup.unity`
- [ ] Kiểm tra `Assets/Prefabs/` tồn tại

### Bước 1 — Xác định trạng thái UI mới

Trả lời trước khi động vào scene:

- [ ] UI mới đang ở đâu? (Scene riêng / Prefab / đang trong BattleScene?)
- [ ] UI mới có `BattleUI` component chưa hay chỉ là visual?
- [ ] Enemy HUD trong UI mới có `highlightImage` field chưa?
- [ ] Tên button có khớp heuristic `CopyTutorialUITool` không? (xem bảng Bước 2)

### Bước 2 — Merge Canvas vào BattleScene

**Lựa chọn A — CopyTutorialUITool** (UI mới nằm trong Tutorial scene):
```
Tools → Battle UI → Copy Tutorial UI to Battle Scene
  deleteLegacyCanvas          = ✓
  removePreviousTutorialCopies = ✓
  wireAutomatically            = ✓
  expandHUDSlots               = ✓  ← quan trọng: 2 player + 3 enemy
```

**Lựa chọn B — Merge thủ công** (UI mới là prefab riêng):
1. Drag Canvas vào BattleScene, đặt tên `BattleUI_Canvas`
2. Gắn component `BattleUI` nếu chưa có
3. Wire thủ công (checklist Bước 3)
4. Xóa Canvas cũ, đảm bảo chỉ 1 `EventSystem`

**Bảng tên button heuristic (phải đúng để auto-wire):**

| BattleUI field | Tên được nhận diện |
|----------------|-------------------|
| `btnAttack` | `Btn_Attack`, `Attack`, `BasicAttack` |
| `btnSkill` | `Btn_Skills`, `Btn_Skill`, `Skill` |
| `btnFlee` | `Btn_Flee`, `Flee`, `Btn_Run` |
| `btnParry` | `Btn_Parry`, `Parry`, `Btn_Energy` |
| `btnSkillBack` | `Btn_Back`, `Back`, `Btn_Cancel` |
| `skillButtons[0..2]` | `Btn_Skill1..3`, `Skill1..3` |

### Bước 3 — Wire BattleUI Inspector

Kiểm tra không có field nào là `None`:

**HUDs**
- [ ] `playerHUDs` — 2 slots, mỗi slot: NameText, HPSlider, HPText, APSlider, APText, Highlight
- [ ] `enemyHUDs` — 3 slots, mỗi slot: NameText, HPSlider, HPText, **highlightImage** (cần thêm nếu thiếu)

**Action Menu**
- [ ] `actionMenuPanel`, `btnAttack`, `btnSkill`, `btnFlee`, `btnParry`

**Skill Menu**
- [ ] `skillMenuPanel`, `btnSkillBack`, `skillButtons` (3), `skillLabels` (3)

**Display**
- [ ] `damagePopupPrefab`, `turnOrderPanel`, `turnOrderText`, `turnIndicatorText`
- [ ] `battleLogText`, `playerEffectsText`, `enemyEffectsText`

**Result**
- [ ] `resultPanel`, `resultText`, `expText`

### Bước 4 — Wire BattleManager Inspector

- [ ] `playerSpawnAnchor` — Transform
- [ ] `enemySpawnAnchor` — Transform
- [ ] `targetCursor` — GameObject prefab con trỏ world-space
- [ ] `spawnSpacing` — float (mặc định 2f)
- [ ] `cursorOffset` — Vector3 (chỉnh để cursor không che khuất model)

### Bước 5 — Tích Hợp Hệ Thống Chọn Mục Tiêu với UI Mới

Đây là phần **bổ sung mới** so với merge UI thông thường.

#### 5A. Highlight Enemy HUD khi target thay đổi

Hiện tại `UnitHUD.SetHighlight()` chỉ được gọi cho player HUD. Cần mở rộng để enemy HUD cũng highlight khi được chọn.

**Cách làm:**

1. Đảm bảo `UnitHUD` trên enemy HUD có `highlightImage` field được wire (Image có color alpha = 0 lúc ban đầu)

2. Thêm method vào `BattleUI.cs`:
```csharp
public void HighlightEnemyHUD(int slotId)
{
    for (int i = 0; i < enemyHUDs.Count; i++)
    {
        if (enemyHUDs[i] != null)
            enemyHUDs[i].SetHighlight(i == slotId);
    }
}
```

3. Gọi `HighlightEnemyHUD` từ `BattleManager` mỗi khi target đổi — thêm vào cuối `ChangeTargetInput()`:
```csharp
// Sau khi cập nhật currentTargetIndex
BattleUI.Instance?.HighlightEnemyHUD(currentTargetIndex);
```

4. Gọi lần đầu khi bắt đầu lượt player trong `PlayerTurn()`, sau `ShowActionMenu()`:
```csharp
BattleUI.Instance?.HighlightEnemyHUD(currentTargetIndex);
```

5. Tắt highlight khi hết lượt player, trong `HideActionMenu()` hoặc sau khi attack:
```csharp
BattleUI.Instance?.HighlightEnemyHUD(-1);
```

#### 5B. Đồng bộ targetCursor với UI

`targetCursor` (world-space) đã tự chạy qua `Update()`. Không cần sửa. Chỉ cần đảm bảo:

- [ ] `targetCursor` GameObject được assign trong BattleManager Inspector
- [ ] `cursorOffset` điều chỉnh vị trí cursor không che model (thử `(−1.5f, 0, 0)` để cursor ở bên trái enemy)
- [ ] Cursor prefab có visual rõ ràng (arrow, glow, v.v.) và `SortingLayer` đúng để hiển thị trước background

#### 5C. Hiển thị tên mục tiêu hiện tại trên UI (tuỳ chọn)

Nếu muốn hiển thị "Mục tiêu: [Tên Enemy]" trên UI:

1. Thêm `TextMeshProUGUI targetNameText` vào `BattleUI`
2. Thêm method `SetTargetName(string name)` vào `BattleUI`
3. Gọi từ `ChangeTargetInput()` và lúc init PlayerTurn

#### 5D. Ally targeting cho skill heal (kích hoạt `isTargetingAlly`)

Hiện `isTargetingAlly` luôn = `false`. Cần cơ chế tự động chuyển khi dùng skill heal:

**Cách đơn giản nhất — Auto-switch trong `UseSkill()`:**

```csharp
// Trong BattleManager.UseSkill(int index), trước khi gọi skill.Use():
if (skill.effectTarget == SkillEffectTarget.SelectedAlly)
{
    // Skill heal → target = GetAllyTarget() thay vì GetEnemyTarget()
    // PlayerAttack.ResolveEffectTarget() đã xử lý SelectedAlly → GetAllyTarget()
    // → không cần thay đổi gì thêm nếu skill data đúng
}
```

**Kiểm tra**: trong `PlayerAttackData`, skill heal phải có `effectTarget = SkillEffectTarget.SelectedAlly`. Nếu đúng thì `ResolveEffectTarget()` trong `PlayerAttack.cs:140` đã tự xử lý.

**Nếu muốn player chủ động chọn ally target** (cho skill buff/heal target cụ thể):
- Cần thêm nút "Đổi mục tiêu" → `isTargetingAlly = true` → NextTarget/PrevTarget cycle qua ally
- Phức tạp hơn, để P3

#### 5E. Click chuột vào enemy để chọn mục tiêu (tuỳ chọn - P3)

Thêm script `ClickableTarget.cs` vào SpawnedModel của enemy:

```csharp
public class ClickableTarget : MonoBehaviour
{
    public EnemyStatus status;
    void OnMouseDown()
    {
        if (BattleManager.Instance == null) return;
        // Tìm index của enemy này trong alive list rồi set currentTargetIndex
        BattleManager.Instance.SetTargetByStatus(status);
    }
}
```

Cần thêm `SetTargetByStatus(EnemyStatus)` vào BattleManager — phức tạp, để P3.

### Bước 6 — Tổ chức Hierarchy

```
Tools → Battle UI → Organize Battle Hierarchy
```
Kết quả:
```
[Systems]     BattleManager, BattleRunner, BattleSceneBootstrap
[Camera]      Main Camera
[UI]          BattleUI_Canvas, EventSystem
[Background]  BattleBackgroundController, visual prefabs
[Spawning]    PlayerSpawnAnchor, EnemySpawnAnchor
[Effects]     targetCursor (nếu là effect), particle systems
```

### Bước 7 — Test

**Target selection — test case:**
- [ ] Bắt đầu lượt player → cursor xuất hiện ở enemy đầu tiên, enemy HUD đó highlight
- [ ] Bấm NextTarget → cursor và HUD highlight chuyển sang enemy tiếp theo
- [ ] Bấm PrevTarget → cursor và HUD highlight quay lại
- [ ] Enemy chết trong lúc là target → cursor auto-chuyển sang enemy tiếp theo (không crash)
- [ ] Chỉ còn 1 enemy alive → bấm NextTarget không gây lỗi (clamp đúng)
- [ ] Bấm Attack → damage popup đúng target đang highlight
- [ ] Hết lượt player → cursor ẩn, enemy HUD tắt highlight

**InputController — test case:**
- [ ] Vào BattleScene từ Map → `InputController.BindBattleManager()` được gọi, log `[INPUT] Battle bound`
- [ ] Bấm phím BasicAttack → attack thực hiện (không cần bấm button UI)
- [ ] Bấm phím NextTarget / PrevTarget → cursor và HUD highlight đổi
- [ ] Bấm phím OpenSkillMenu → mode chuyển sang `BattleSkillMenu`, phím Skill1/Skill2 hoạt động
- [ ] Bấm phím Skill1 → UseSkill(0), mode trở về `Battle`
- [ ] Bấm phím Cancel (trong SkillMenu) → mode về `Battle`, action menu hiện lại
- [ ] Bấm phím Parry → RequestParry()
- [ ] Bấm phím Flee → TryFlee()
- [ ] Battle kết thúc → `UnbindBattleManager()`, mode về `Map`, phím battle không còn hoạt động
- [ ] Bấm nút UI "Skill" → Skill Menu hiện, **đồng thời** `SetMode(BattleSkillMenu)` được gọi
- [ ] Bấm nút UI "Back" trong SkillMenu → `SetMode(Battle)` được gọi

**UI merge — test case:**
- [ ] HUD hiển thị đúng tên, HP, AP
- [ ] Action Menu / Skill Menu mở đóng đúng
- [ ] Result Panel "THANG!" / "THUA!" đúng màu
- [ ] Battle log cập nhật
- [ ] Full flow từ Map Scene → Battle → Return về map

---

## 4. Thứ Tự Ưu Tiên

```
P0 — Blocking (phải xong trước khi test bất cứ gì)
  ├── Wire đủ BattleUI serialized fields
  ├── Wire BattleManager (spawn anchors + targetCursor)
  └── Canvas không duplicate, EventSystem duy nhất

P1 — Core gameplay
  ├── Attack / Skill / Flee / Parry hoạt động
  ├── HUD update HP/AP đúng
  ├── targetCursor hiển thị đúng enemy đang chọn (world-space)
  └── HighlightEnemyHUD() — highlight HUD enemy khi chọn [MỚI]

P2 — Feedback rõ ràng
  ├── Turn indicator
  ├── Battle log
  ├── Damage popup
  └── Result panel

P2.5 — InputController sync [MỚI]
  ├── Đồng bộ SetMode() khi bấm nút UI mở/đóng SkillMenu
  └── Kiểm tra BattleSceneBootstrap có tạo InputController fallback không

P3 — Nice to have
  ├── Thêm Skill3 keyboard binding vào GameInput.inputactions + BindSkillMenuInput()
  ├── Click-to-target (ClickableTarget script)
  ├── Ally target switching UI
  ├── Target name text label trên UI
  ├── OpenItemMenu implement
  └── Cursor animation / glow effect
```

---

## 5. Điểm Tích Hợp Quan Trọng

### InputController ↔ BattleUI (cần đồng bộ thêm)
```csharp
// BattleUI.OnSkillMenuOpen() — THÊM VÀO
InputController.Instance?.SetMode(InputMode.BattleSkillMenu);

// BattleUI.OnSkillMenuClose() — THÊM VÀO
InputController.Instance?.SetMode(InputMode.Battle);
```

### Target selection (không được phá vỡ)
```csharp
// Input → BattleManager
BattleManager.Instance.ChangeTargetInput(+1 / -1)   // NextTarget / PrevTarget

// BattleManager nội bộ (Update loop)
targetCursor.transform.position = target.SpawnedModel.transform.position + cursorOffset

// BattleManager → BattleUI [CẦN THÊM]
BattleUI.Instance.HighlightEnemyHUD(currentTargetIndex)   // khi target đổi
BattleUI.Instance.HighlightEnemyHUD(-1)                    // khi hết lượt player
```

### Skill target resolution (đã có, không cần sửa)
```csharp
// PlayerAttack.ResolveEffectTarget()
SkillEffectTarget.Self         → player
SkillEffectTarget.Enemy        → enemy (= GetEnemyTarget())
SkillEffectTarget.SelectedAlly → BattleManager.GetAllyTarget()
```

### BattleUI ↔ BattleManager (đã có)
```csharp
// UI → BattleManager
BattleManager.Instance.SelectBasicAttack()
BattleManager.Instance.UseSkill(index)
BattleManager.Instance.TryFlee()
BattleManager.Instance.RequestParry()

// BattleManager → BattleUI
BattleUI.Instance.ShowActionMenu(player)
BattleUI.Instance.HideActionMenu()
BattleUI.Instance.HighlightActivePlayerHUD(slotId)
BattleUI.Instance.SetTurnIndicator(label)
BattleUI.Instance.ShowDamagePopup(pos, value, isHeal)
BattleUI.Instance.Log(message)
BattleUI.Instance.SetExpResult(expInfo)
```

### GameEvents (phải tồn tại)
```
GameEvent.BattleStart → OnBattleStart()
GameEvent.BattleWin   → OnBattleWin()
GameEvent.BattleLose  → OnBattleLose()
GameEvent.BattleFlee  → OnBattleFlee()
GameEvent.UnitDied    → OnUnitDied()
```

---

## 6. Phân Tích Rủi Ro

| Rủi ro | Mức độ | Cách xử lý |
|--------|--------|------------|
| BattleUI serialized fields null sau khi thay Canvas | **Cao** | Re-wire hoặc chạy lại CopyTutorialUITool |
| `highlightImage` thiếu trong enemy HUD → `SetHighlight()` silent fail | **Cao** | Wire field trước khi test |
| `targetCursor` null → NullRef trong Update() mỗi frame | **Cao** | Assign trong Inspector, có null check sẵn |
| EventSystem duplicate → click không nhận | **Trung bình** | Xóa thừa, chỉ giữ 1 |
| `currentTargetIndex` out of range khi enemy chết đồng thời | **Thấp** | `GetEnemyTarget()` đã auto-clamp |
| `isTargetingAlly = true` không bao giờ được set → skill heal có thể target sai | **Thấp** | Kiểm tra `effectTarget` trong skill data |
| **InputController null** khi test BattleScene standalone | **Trung bình** | Keyboard input không hoạt động — kiểm tra BattleSceneBootstrap có tạo fallback không |
| **Mode không đồng bộ**: bấm nút "Skill" trên UI nhưng mode vẫn ở `Battle` | **Trung bình** | Thêm `InputController.Instance?.SetMode(BattleSkillMenu)` vào `BattleUI.OnSkillMenuOpen()` |
| **Skill3 không có phím tắt** → người chơi chỉ dùng được qua button | **Thấp** | Thêm `Skill3` action vào `GameInput.inputactions` và `BindSkillMenuInput()` |

---

## 7. Rollback

```bash
git checkout Assets/Scenes/BattleScene.unity   # khôi phục scene
# hoặc restore từ BattleScene_backup.unity
```

Script không cần rollback vì `HighlightEnemyHUD()` là addition, không sửa logic cũ.

---

## 8. Ghi Chú Kỹ Thuật

- `BattleUI` và `BattleManager` đều singleton — tránh duplicate trong scene
- `targetCursor` là world-space object, **không** phải UI Canvas child — để ngoài Canvas
- `cursorOffset` thử `(-1.5f, 0.5f, 0)` để cursor xuất hiện ở trên-trái model
- `UnitHUD.SetHighlight()` dùng alpha channel — `highlightImage` phải có `raycastTarget = false`
- `isTargetingAlly` chưa được set ở bất kỳ đâu — nếu cần ally targeting phải thêm logic set flag
- `BattleSceneBootstrap` chỉ chạy trong editor, không ảnh hưởng build
- TMP Essentials phải được import: `Window → TextMeshPro → Import TMP Essential Resources`
