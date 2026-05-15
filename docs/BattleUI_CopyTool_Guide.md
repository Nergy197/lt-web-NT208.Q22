# Hướng Dẫn: Copy Battle UI từ Tutorial sang BattleScene

## Tổng quan

Tool `CopyTutorialUITool` tự động copy toàn bộ Canvas UI từ `Chapter1_Tutorial.unity` sang `BattleScene.unity`, sau đó wire các element vào `BattleUI.cs`.

**Truy cập**: Unity Editor → menu **Tools → Battle UI → Copy Tutorial UI to Battle Scene**

---

## Tại sao cần tool này?

BattleScene hiện chỉ có một Canvas trống bị disabled (scale = 0, m_Enabled = 0). Toàn bộ UI chiến đấu (HP bars, action menu, skill panel...) đang nằm trong Chapter1_Tutorial. Tool này giúp đồng bộ UI sang BattleScene mà không cần làm lại thủ công.

---

## Cách sử dụng

### Bước 1 — Mở tool
Vào menu **Tools → Battle UI → Copy Tutorial UI to Battle Scene**

### Bước 2 — Chọn tùy chọn

| Tùy chọn | Mặc định | Mô tả |
|---|---|---|
| Xóa Canvas cũ bị disabled | ✅ bật | Dọn Canvas rỗng trong BattleScene trước khi copy |
| Xóa bản copy cũ từ Tutorial | ✅ bật | Tránh duplicate nếu chạy tool nhiều lần |
| Copy background/prefabs visual từ Tutorial | ✅ bật | Copy background, character models... |
| Tự động wire BattleUI | ✅ bật | Tự fill các serialized field của BattleUI.cs |
| Mở rộng HUD slots (2 player / 3 enemy) | ✅ bật | Clone HUD template để đủ slot cho toàn party |

### Bước 3 — Chạy
Nhấn **"▶ Chạy Copy"**. Tool sẽ tự động:
1. Mở `Chapter1_Tutorial.unity` (additive)
2. Tìm Canvas UI chính (Canvas có nhiều children nhất)
3. Duplicate Canvas sang `BattleScene`
4. Bật Canvas (enable + set active)
5. Copy các root visual/prefab (background, characters)
6. Wire các field của `BattleUI.cs`
7. Clone HUD slots đủ cho 2 player + 3 enemy
8. Lưu BattleScene

---

## Sau khi chạy — Wire thủ công

Một số field của `BattleUI.cs` không có element tương ứng trong Tutorial và cần gán thủ công trong Inspector:

| Field | Ghi chú |
|---|---|
| `damagePopupPrefab` | Cần tạo prefab riêng (Text nổi khi nhận damage) |
| `turnOrderPanel` | Panel hiển thị thứ tự lượt |
| `turnOrderText` | Text hiển thị danh sách lượt |
| `turnIndicatorText` | Text chỉ lượt hiện tại |
| `battleLogText` | Log các hành động trong battle |
| `playerEffectsText` | Text hiển thị status effect của player |
| `enemyEffectsText` | Text hiển thị status effect của enemy |
| `resultPanel` | Panel thắng/thua |
| `resultText` | Text kết quả (Victory / Defeat) |

---

## Scan UI cấu trúc Tutorial (debug)

Nếu tool không tìm được đúng Canvas, dùng menu phụ để log cấu trúc:

**Tools → Battle UI → Scan Tutorial UI Structure (Log)**

Kết quả in trong Unity Console, liệt kê toàn bộ GameObject và Component trong Tutorial scene.

---

## Các button được wire tự động

| Field BattleUI | Tên button trong Tutorial |
|---|---|
| `btnAttack` | `Btn_Attack` |
| `btnSkill` | `Btn_Skills` |
| `btnFlee` | `Btn_Flee` / `Btn_Items` |
| `btnParry` | `Btn_Parry` / `Btn_Energy` / `Btn_Heal` |
| `btnSkillBack` | `Btn_Back` / `Btn_Cleanse` |
| `skillButtons[0..2]` | `Btn_Skill1`, `Btn_Skill2`, `Btn_Skill3` |

---

## Lưu ý

- Chạy tool khi **BattleScene là Active Scene** trong Unity Editor
- Sau khi chạy, mở **Inspector của BattleUI** để kiểm tra các slot còn trống
- Tool hỗ trợ **Undo** (Ctrl+Z) cho các thao tác tạo/xóa GameObject
