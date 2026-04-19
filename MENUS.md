# Danh sách Menu cần làm

Tham khảo style: **Octopath Traveler** — dark background, list dọc, cursor ▶ di chuyển, viền ornate.

---

## ✅ Đã có

| Menu | File | Trạng thái |
|---|---|---|
| Main Menu (Start / Quit / Save Slots) | `StartMenuUI.cs` | Có, cần nâng cấp UI |
| Battle UI | `BattleUI.cs` | Có |
| Save Point UI | `SavePointUI.cs` | Có |
| Teleport Menu | `TeleportMenuUI.cs` | Có |

---

## 🔲 Cần làm

### 1. Graphics Settings Menu
Mở từ Main Menu → Cài đặt → Đồ họa

| Tùy chọn | Loại | Ghi chú |
|---|---|---|
| Độ phân giải | Dropdown | Lấy từ `Screen.resolutions` |
| Toàn màn hình | Toggle | Fullscreen / Windowed |
| VSync | Toggle | `QualitySettings.vSyncCount` |
| Chất lượng đồ họa | Dropdown | Low / Medium / High / Ultra |
| Độ sáng | Slider | 0.0 → 2.0, dùng Post Processing |

---

### 2. Audio Settings Menu
Mở từ Main Menu → Cài đặt → Âm thanh

| Tùy chọn | Loại | Ghi chú |
|---|---|---|
| Âm lượng tổng | Slider | AudioMixer `MasterVolume` |
| Nhạc nền | Slider | AudioMixer `MusicVolume` |
| Hiệu ứng âm thanh | Slider | AudioMixer `SFXVolume` |

---

### 3. Settings Hub Menu
Panel trung gian chứa các tab: Đồ họa / Âm thanh / Game

| Tùy chọn | Loại | Ghi chú |
|---|---|---|
| Tab Đồ họa | Button | Mở Graphics Settings |
| Tab Âm thanh | Button | Mở Audio Settings |
| Tab Game | Button | Ngôn ngữ, tốc độ text... |
| Nút Quay lại | Button | Đóng Settings, về Main Menu |

---

### 4. Pause Menu (In-game)
Mở bằng ESC hoặc nút Pause trong lúc khám phá map

| Tùy chọn | Loại | Ghi chú |
|---|---|---|
| Tiếp tục | Button | Đóng pause menu |
| Cài đặt | Button | Mở Settings Hub |
| Lưu game | Button | Gọi SaveManager |
| Về Main Menu | Button | Xác nhận trước khi thoát |

---

### 5. Main Menu — Nâng cấp
Thêm vào `StartMenuUI.cs` hiện có

| Thêm | Ghi chú |
|---|---|
| Nút "Cài đặt" | Mở Settings Hub |
| Keyboard navigation | ▲▼ di chuyển, Enter xác nhận |
| Cursor ▶ | Hiển thị bên trái item đang chọn |
| Fade-in khi vào | DOTween hoặc Coroutine alpha 0→1 |

---

### 6. Inventory / Party Menu *(tùy chọn)*
Xem trong lúc khám phá map — chưa ưu tiên

| Tùy chọn | Ghi chú |
|---|---|
| Thông tin party | HP, level từng nhân vật |
| Túi đồ | Danh sách item đang giữ |
| Trang bị | Slot vũ khí / giáp |

---

## Thứ tự ưu tiên

1. **Settings Hub + Graphics Settings** — cần nhất cho submission
2. **Audio Settings** — đơn giản, làm cùng Graphics
3. **Main Menu nâng cấp** — keyboard nav + cursor
4. **Pause Menu** — cần cho gameplay hoàn chỉnh
5. **Inventory/Party** — thấp nhất, làm sau
