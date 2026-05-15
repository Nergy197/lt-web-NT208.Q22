# Kế Hoạch Tạo Lại BattleScene

> Ngày: 2026-05-15 | Scene cũ đã xoá | Tạo mới từ đầu

---

## 1. Mục Tiêu

Tạo lại `BattleScene.unity` kết nối đúng với hệ thống battle đã có.

- **Lấy từ Tutorial:** chỉ menu buttons (Menu_Frame, Skill_Frame, Item_Frame) + background
- **Tạo mới hoàn toàn:** HUD nhân vật (player/enemy) — dùng `Slider` chuẩn, nạp data runtime từ MapData
- **Không lấy:** `HUD_Background`, `Enemy_HP_Canvas` từ Tutorial (chúng dùng Image.fillAmount không phải Slider)

---

## 2. Nguồn UI Từ Chapter1_Tutorial

Canvas: **`TranQuocTuan_Canvas`** — chỉ lấy phần buttons:

```
TranQuocTuan_Canvas
├── Menu_Frame       → Action Menu (Btn_Attack, Btn_Skills, Btn_Items)
├── Skill_Frame      → Skill Menu  (Btn_Skill1, Btn_Skill2, Btn_Skill3)
└── Item_Frame       → Parry/Back  (Btn_Heal, Btn_Energy, Btn_Cleanse)
```

Bỏ qua: `HUD_Background`, `Enemy_HP_Canvas` — tool sẽ tạo HUD mới.

Background: **`Scene_Combat`** — copy nguyên làm nền tĩnh.

---

## 3. Mapping Tutorial UI → BattleUI Fields

| BattleUI Field | Nguồn | Ghi chú |
|---------------|-------|---------|
| `actionMenuPanel` | `Menu_Frame` | Action menu chính |
| `btnAttack` | `Btn_Attack` | Trong Menu_Frame |
| `btnSkill` | `Btn_Skills` | Trong Menu_Frame |
| `btnFlee` | `Btn_Items` | Repurpose |
| `skillMenuPanel` | `Skill_Frame` | |
| `skillButtons[0..2]` | `Btn_Skill1..3` | Trong Skill_Frame |
| `btnParry` | `Btn_Heal` | Trong Item_Frame, repurpose |
| `btnSkillBack` | `Btn_Energy` | Trong Item_Frame, repurpose |

---

## 4. HUD Tạo Mới (Không Lấy Từ Tutorial)

Tool tạo 2 player HUD (top-left) và 3 enemy HUD (top-right) với `Slider` chuẩn:

```
PlayerHUD_0  (UnitHUD component)          EnemyHUD_0  (UnitHUD component)
├── Background  (Image, xanh)             ├── Background  (Image, đỏ)
├── Highlight   (Image, alpha=0)          ├── Highlight   (Image, alpha=0)
├── NameText    (TextMeshProUGUI)         ├── NameText    (TextMeshProUGUI)
├── HPSlider    (Slider, đỏ)             ├── HPSlider    (Slider, đỏ)
├── HPText      (TextMeshProUGUI)         └── HPText      (TextMeshProUGUI)
├── APSlider    (Slider, xanh)           (không có AP — enemy không dùng)
└── APText      (TextMeshProUGUI)
```

Wire tự động vào `BattleUI.playerHUDs` và `BattleUI.enemyHUDs`.

---

## 5. Elements Tạo Mới Thêm Vào Canvas

| BattleUI Field | Tạo | Vị trí |
|---------------|-----|--------|
| `turnIndicatorText` | TMP 32pt | Top center |
| `turnOrderPanel` + `turnOrderText` | Panel + TMP 24pt | Top center, dưới indicator |
| `battleLogText` | TMP 18pt | Bottom left |
| `playerEffectsText` | TMP 13pt | Left side |
| `enemyEffectsText` | TMP 13pt | Right side |
| `resultPanel` + `resultText` + `expText` | Panel (ẩn) + TMP 60pt + 24pt | Center |
| `targetNameText` | TMP 20pt | Gần cursor |
| `skillLabels[0..2]` | TMP 14pt | Trên mỗi Skill button |
| `damagePopupPrefab` | Prefab TextMeshPro | `Assets/Prefabs/UI/DamagePopup.prefab` |

---

## 6. Hierarchy BattleScene

```
BattleScene
├── [Systems]
│   ├── BattleManager           spawnAnchor + targetCursor wired
│   ├── BattleSceneBootstrap    debugMapData → random enemy từ MapData
│   ├── BattleRunner
│   └── BattleBackgroundController
├── [Camera]
│   └── Main Camera
├── [UI]
│   ├── BattleUI_Canvas         BattleUI component, tất cả fields wired
│   └── EventSystem
├── [Background]
│   └── TutorialCopy_Scene_Combat
└── [Spawning]
    ├── PlayerSpawnAnchor       (-4, 0, 0)
    ├── EnemySpawnAnchor        (+4, 0, 0)
    └── TargetCursor            visual con trỏ chọn mục tiêu
```

---

## 7. Các Bước Thực Hiện

- [ ] **B1:** `File → New Scene (Empty)` → lưu `Assets/Scenes/BattleScene.unity` → thêm Build Settings
- [ ] **B2:** `Tools → Battle → Rebuild Battle Scene` → tick tất cả → **Rebuild**
- [ ] **B3:** Inspector `BattleUI` — kiểm tra không có field nào `None`
- [ ] **B4:** Inspector `BattleManager` — wire `targetCursor`, kiểm tra spawn anchors
- [ ] **B5:** Inspector `BattleSceneBootstrap` — gán `debugMapData`
- [ ] **B6:** Play test — xem log `[BattleBootstrap]` nạp enemy, HUD hiển thị đúng

---

## 8. Test Cases

| Test | Kết quả mong đợi |
|------|-----------------|
| Play từ BattleScene | Log `[BattleBootstrap] Spawn N enemy ngẫu nhiên từ MapData` |
| Đến lượt player | `Menu_Frame` hiện, cursor xuất hiện ở enemy |
| Btn_Attack | Damage popup, enemy HP giảm |
| Btn_Skills | `Skill_Frame` hiện, Btn_Skill1/2/3 có label |
| Enemy chết | HUD tắt highlight, cursor chuyển sang enemy còn sống |
| Thắng | ResultPanel hiện "THẮNG!", ExpText hiện EXP |
