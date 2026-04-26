# BÁO CÁO KỸ THUẬT ĐỒ ÁN GIỮA KÌ
**Môn học:** NT208.Q22 — Lập trình ứng dụng Web
**Dự án:** Turn-Based RPG (Unity WebGL + Node.js)
**Trạng thái:** Hoàn thiện 85% Core Gameplay & Backend

---

## 1. PHÂN TÍCH KIẾN TRÚC KỸ THUẬT

Dự án được xây dựng dựa trên mô hình **Client-Server Integration**, tối ưu hóa cho trải nghiệm Web với các điểm nhấn công nghệ sau:

### A. Hệ thống Quản lý Trạng thái (Game State & Persistence)
*   **Hybrid Storage Strategy:** Sử dụng mô hình lưu trữ kép. Dữ liệu được ưu tiên ghi vào `LocalStorage` (thông qua PlayerPrefs) để đảm bảo tốc độ, sau đó được đồng bộ không đồng bộ (Asynchronous) lên **MongoDB Atlas** thông qua REST API.
*   **Identity Management:** Hệ thống tự động cấp phát `Guest ID` (GUID) duy nhất cho mỗi trình duyệt, cho phép người chơi quay lại game mà không cần đăng ký tài khoản rườm rà.
*   **Safety Net:** Cơ chế `QuickSave` tích hợp vào sự kiện `OnApplicationQuit` và `OnApplicationPause` giúp chống mất dữ liệu khi người chơi vô tình đóng tab hoặc chuyển tab trên mobile.

### B. Hệ thống Chiến đấu & AI (Combat Engine)
*   **Turn-based Logic:** Hệ thống lượt đánh dựa trên tốc độ (Speed-based sorting) thực hiện qua `TurnManager`.
*   **Cơ chế Buff/Debuff:** Đã triển khai hoàn thiện logic cho các trạng thái Poison (sát thương theo thời gian) và Stun (mất lượt).
*   **Active Defense:** Hệ thống **Parry** cho phép người chơi tương tác trực tiếp trong lượt của quái vật để giảm sát thương, tăng tính hấp dẫn cho combat.
*   **Rule-based AI:** Quái vật có khả năng tính toán hành động dựa trên trạng thái của người chơi (máu thấp, số lượng unit...) thay vì chỉ đánh ngẫu nhiên.

### C. Hệ thống Nhiệm vụ & Cốt truyện (Quest & Narrative)
*   **ScriptableObject Architecture:** Toàn bộ dữ liệu nhiệm vụ được tách rời khỏi code logic, giúp việc mở rộng nội dung game dễ dàng.
*   **Branching Quest:** Hỗ trợ cốt truyện phân nhánh, lựa chọn của người chơi sẽ dẫn đến các chuỗi nhiệm vụ (Quest Chain) khác nhau.
*   **Event-Driven Integration:** Hệ thống nhiệm vụ được kết nối trực tiếp với hệ thống Battle và Map thông qua các sự kiện (Events), cho phép tự động hoàn thành nhiệm vụ ngay khi kết thúc trận đánh hoặc đi tới vùng đất mới.

---

## 2. CÁC KHÓ KHĂN ĐÃ GIẢI QUYẾT & ĐANG TỒN ĐẠI

### ✅ Đã giải quyết:
- **CORS & Web Request:** Xử lý triệt để lỗi chặn API khi chạy WebGL trên các domain khác nhau.
- **WebGL Memory Management:** Tối ưu hóa dung lượng build và quản lý asset để game chạy ổn định trên các trình duyệt phổ thông.
- **SPA Fallback:** Cấu hình server Node.js để hỗ trợ điều hướng trang khi người chơi sử dụng phím Back/Forward của trình duyệt.

### ⚠️ Khó khăn hiện tại:
- **Tối ưu hóa UI/UX:** Việc thiết kế UI chuyên sâu (Octopath Traveler style) trên Unity đòi hỏi rất nhiều công sức hiệu chỉnh Pixel-perfect để không bị mờ trên các độ phân giải màn hình khác nhau.
- **Asset Consistency:** Tìm kiếm tài nguyên đồ họa (2D Sprites) đồng nhất là thách thức lớn nhất của nhóm về mặt thẩm mỹ.

---

## 3. KẾ HOẠCH HOÀN THIỆN (GIAI ĐOẠN CUỐI)
1. **Hoàn thiện UI Settings:** Triển khai bảng điều khiển âm thanh, đồ họa và gán phím (Keybindings).
2. **Hệ thống Túi đồ (Inventory):** Mở rộng tính năng sử dụng vật phẩm trong và ngoài trận đấu.
3. **Đóng gói & Phân phối:** Tối ưu hóa nén Gzip/Brotli trên server để giảm thời gian tải trang ban đầu.

---
**GitHub:** https://github.com/Nergy197/lt-web-NT208.Q22.git
**Demo:** 

