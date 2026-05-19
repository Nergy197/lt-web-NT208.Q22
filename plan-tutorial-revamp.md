# Kế hoạch sửa lại Tutorial Scene

## Tiếp cận

**Chapter1_Tutorial.unity = BattleScene.unity + 2 component thêm vào.**

BattleManager chạy hoàn toàn bình thường — không sửa. Tutorial chỉ là lớp
overlay mỏng ngồi trên, lắng nghe BattleEvents và hiện prompt.

---

## Vì sao không cần sửa BattleManager

BattleManager đã có sẵn:
- `useDemoModeIfMissingManager = true` → chạy được không cần GameManager
- `debugPlayerData` → kéo PlayerData vào Inspector là có player
- `debugEnemyAttack` → kéo EnemyAttackData vào là có enemy
- `EndBattle()` không có MapManager → tự load `MapScene`

→ Tutorial chỉ cần cấu hình Inspector đúng, không cần thêm code vào BattleManager.

---

## Luồng tutorial

```
StartScene
  └─ GameManager.StartGame()
       ├─ tutorialCompleted == false → load "Chapter1_Tutorial"
       └─ tutorialCompleted == true  → load "Chapter1_CutScene" (như cũ)

Chapter1_Tutorial (= BattleScene + TutorialBootstrap + TutorialController)
  │
  ├─ BattleManager khởi động (demo mode: debugPlayerData + debugEnemyAttack)
  ├─ TutorialController.Start() → subscribe BattleEvents, hiện bước 1
  │
  ├─ [Bước 1] Lượt player → "Nhấn Q để tấn công"
  │     event: OnAttackFinished (player turn) → advance bước 2
  │
  ├─ [Bước 2] Lượt enemy → "Địch đang chuẩn bị..."
  │     event: OnEnemyAttackAnnounced → hiện cảnh báo
  │     event: OnParryWindowOpened   → flash "PARRY!"
  │     event: OnParrySuccess        → advance bước 3 (parry thành công)
  │            (hoặc OnAttackFinished → advance bước 3 nếu không parry)
  │
  ├─ [Bước 3] Lượt player → "Dùng chiêu thức: mở menu [W]"
  │     event: OnAttackFinished (skill used) → advance Done
  │
  └─ [Done] TutorialController.SetTutorialComplete()
            → PlayerPrefs "tutorialCompleted" = 1
            → BattleManager.EndBattle() tự load MapScene (hoặc Chapter1_CutScene)
```

---

## Các file cần làm

### Tạo mới (3 script + 2 asset)

#### `Assets/Scripts/Tutorial/TutorialController.cs`
MonoBehaviour đặt trong Chapter1_Tutorial scene.

```
Fields:
  TutorialPromptUI promptUI
  int currentStep = 0
  bool playerJustAttacked = false

OnEnable / OnDisable:
  subscribe / unsubscribe BattleEvents

Handlers:
  OnEnemyAttackAnnounced → step==2: promptUI.Show("Địch chuẩn bị tấn công!")
  OnEnemyHitIncoming     → step==2: promptUI.Show("Sắp tới rồi...")
  OnParryWindowOpened    → step==2: promptUI.FlashParry()
  OnParrySuccess         → step==2: AdvanceStep()
  OnAttackFinished       → step==1: check if player attacked → AdvanceStep()
                        → step==2: AdvanceStep() (parry bị miss → vẫn tiếp)
                        → step==3: check if skill used → AdvanceStep()

AdvanceStep():
  currentStep++
  Cập nhật promptUI theo step mới
  Nếu currentStep == Done → SetTutorialComplete()

SetTutorialComplete():
  PlayerPrefs.SetInt("tutorialCompleted", 1)
  PlayerPrefs.Save()
  // BattleManager tự EndBattle khi battle kết thúc → load scene tiếp theo
```

#### `Assets/Scripts/Tutorial/TutorialPromptUI.cs`
MonoBehaviour với Canvas overlay đơn giản.

```
Methods:
  Show(string text)     → hiện panel + set text
  Hide()                → ẩn panel
  FlashParry()          → animation nhấp nháy text PARRY (dùng DOTween hoặc Coroutine)
```

#### `Assets/Data/Tutorial/TutorialEnemyAttack.asset` (EnemyAttackData)
```yaml
attackName: "Đòn Tập"
hits:
  - canBeParried: 1
    animDuration: 2.0      # chậm, dễ thấy
    parryOpenTime: 0.4     # window mở sớm, còn 1.6s để parry
    damageMultiplier: 0.4  # ít damage, không kill player
    apRestoreOnParry: 2
```

#### `Assets/Data/Tutorial/TutorialPlayerData.asset` (PlayerData) — nếu cần
Nếu muốn player tutorial khác player thật: tạo PlayerData riêng với stats thấp hơn, chỉ có skill cơ bản.
Nếu dùng chung `Data_TranQuocTuan` thì không cần file này.

### Sửa (1 script)

#### `Assets/Scripts/System/GameManager.cs`
Thêm routing đến tutorial:

```csharp
// Trong StartGame():
bool tutorialDone = PlayerPrefs.GetInt("tutorialCompleted", 0) == 1;
if (!tutorialDone && pendingSaveScene == null)
{
    SceneManager.LoadScene("Chapter1_Tutorial");
    return;
}
// ... logic cũ tiếp theo
```

### Không sửa

| File | Lý do |
|---|---|
| `BattleManager.cs` | Chạy bình thường, demo mode đã hỗ trợ |
| `BattleEvents.cs` | Đã có đủ events |
| `EnemyAttack.cs` | Không thay đổi |
| `InputController.cs` | Không block input — tutorial không ép buộc thứ tự |
| `SimpleTutorialManager.cs` | Giữ lại, không xóa |

---

## Chapter1_Tutorial.unity — cấu hình Inspector

Copy toàn bộ BattleScene.unity, thêm:

| Component | Trên GameObject | Cấu hình |
|---|---|---|
| `BattleManager` | (đã có) | `useDemoModeIfMissingManager = true`, `debugPlayerData = TutorialPlayerData`, `debugEnemyAttack = TutorialEnemyAttack` |
| `TutorialController` | GameObject "Tutorial" mới | kéo TutorialPromptUI vào |
| `TutorialPromptUI` | Canvas con | panel overlay góc dưới màn hình |

---

## Thứ tự implement

```
1. TutorialEnemyAttack.asset       ← asset, không cần code
2. TutorialPromptUI.cs             ← UI thuần, không phụ thuộc gì
3. TutorialController.cs           ← logic chính
4. GameManager.cs                  ← thêm 5 dòng routing
5. Chapter1_Tutorial.unity         ← copy BattleScene, add components, cấu hình Inspector
```

---

## Kết quả sau khi hoàn thành

- Không có code fake battle nào — mọi cơ chế đều là thật
- BattleManager / EnemyAttack / parry system đều chạy như trong game thật
- Tutorial chỉ là 2 MonoBehaviour mỏng + 1 asset
- Dễ bảo trì: cập nhật battle system → tutorial tự cập nhật theo
