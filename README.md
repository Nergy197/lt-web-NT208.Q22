# 🎮 Lập Trình Ứng Dụng Web — NT208.Q22

> Đồ án môn học **NT208 – Lập trình ứng dụng Web**, Đại học Công nghệ Thông tin – ĐHQG TP.HCM.
> Một game nhập vai theo lượt (Turn-based RPG) chạy trên nền tảng **Unity WebGL**, kết hợp **Node.js backend** và cơ sở dữ liệu **MongoDB**.

---

## 📑 Mục lục

- [Giới thiệu](#giới-thiệu)
- [Tính năng](#tính-năng)
- [Kiến trúc hệ thống](#kiến-trúc-hệ-thống)
- [Cấu trúc thư mục](#cấu-trúc-thư-mục)
- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Hướng dẫn cài đặt](#hướng-dẫn-cài-đặt)
- [Hướng dẫn chạy](#hướng-dẫn-chạy)
- [API Backend](#api-backend)
- [Công nghệ sử dụng](#công-nghệ-sử-dụng)
- [Thành viên nhóm](#thành-viên-nhóm)
- [Giấy phép](#giấy-phép)

---

## Giới thiệu

Dự án xây dựng một **game nhập vai theo lượt** (Turn-based RPG) triển khai trên nền tảng web thông qua **Unity WebGL**. Người chơi sẽ khám phá bản đồ, tương tác với NPC, thực hiện nhiệm vụ (quest) phân nhánh, và chiến đấu với quái vật theo cơ chế lượt. Dữ liệu người chơi được lưu trữ trên **MongoDB** thông qua một REST API backend viết bằng **Node.js / Express**.

---

## Tính năng

### ⚔️ Hệ thống chiến đấu theo lượt
- Chiến đấu lượt giữa party người chơi và quái vật.
- Hệ thống **Parry** (đỡ đòn) giảm sát thương từ kẻ thù.
- Kỹ năng tấn công (*Skill*) với hiệu ứng trạng thái: **Poison** (độc), **Stun** (choáng).
- Chọn mục tiêu tấn công linh hoạt, hỗ trợ chuyển target.
- Cơ chế **bỏ chạy** (Flee) khỏi trận đấu.
- Hệ thống EXP và phần thưởng sau trận đấu.

### 🗺️ Khám phá bản đồ
- Di chuyển nhân vật trên bản đồ 2D bằng Unity Input System.
- Chuyển cảnh (scene transition) giữa các khu vực thông qua MapTrigger.
- Gặp quái ngẫu nhiên (random encounter) khi đi vào vùng EnemyTrigger.
- Camera zone tự động điều chỉnh camera theo khu vực.

### 📜 Hệ thống nhiệm vụ (Quest)
- Nhiệm vụ phân nhánh với **QuestBranchChoice** — người chơi lựa chọn hướng đi câu chuyện.
- Quản lý mục tiêu nhiệm vụ (QuestObjective) chi tiết.
- Giao diện Quest UI hiển thị tiến độ nhiệm vụ.
- Lưu trạng thái nhiệm vụ (QuestSaveData) đồng bộ với backend.

### 💾 Hệ thống lưu trữ
- Lưu tiến trình tại các **Save Point** trên bản đồ.
- Gửi dữ liệu party & vị trí save point lên server qua REST API.
- Hồi sinh (respawn) tại save point cuối khi party bị đánh bại.

### 🤖 NPC & Tương tác
- Hệ thống NPC với hội thoại và tương tác.
- Kích hoạt sự kiện câu chuyện thông qua NpcTrigger.

---

## Kiến trúc hệ thống

```
┌──────────────┐        HTTP / REST API        ┌──────────────────┐
│  Unity WebGL │  ◄──────────────────────────►  │  Node.js Server  │
│  (Frontend)  │       (port 3000)              │  (Express.js)    │
└──────────────┘                                └────────┬─────────┘
                                                         │
                                                         ▼
                                                ┌──────────────────┐
                                                │     MongoDB      │
                                                │  (webgame DB)    │
                                                └──────────────────┘
```

- **Frontend**: Game Unity được build thành WebGL, phục vụ dưới dạng static files.
- **Backend**: Node.js + Express xử lý REST API (lưu/load dữ liệu người chơi).
- **Database**: MongoDB lưu trữ thông tin player, party, và vị trí save point.

---

## Cấu trúc thư mục

```
lt-web-NT208.Q22/
├── Assets/
│   ├── Scripts/
│   │   ├── Battle/          # Hệ thống chiến đấu (BattleManager, EnemyAttack, ...)
│   │   │   ├── Runtime/     # Logic chiến đấu chính
│   │   │   └── State/       # Trạng thái trận đấu
│   │   ├── Core/            # Core gameplay (Combat, Data, Factory, Status, Turn)
│   │   ├── Enemy/           # Dữ liệu & AI quái vật
│   │   │   ├── Data/        # EnemyAttackData, EnemyData
│   │   │   └── Runtime/     # EnemyStatus, logic runtime
│   │   ├── Map/             # Bản đồ, NPC, SavePoint, Camera
│   │   ├── Player/          # Dữ liệu & runtime nhân vật
│   │   ├── Quest/           # Hệ thống nhiệm vụ
│   │   ├── Save/            # PlayerSave, UnitSave
│   │   ├── System/          # GameManager, EventManager, InputController
│   │   └── UI/              # Giao diện người dùng
│   └── Data/                # GameInput, ScriptableObjects, ...
├── Backend/
│   ├── server.js            # Entry point server Node.js
│   ├── package.json         # Dependencies (express, mongodb, cors)
│   ├── Database/            # Seed data (webgame.players.json)
│   └── public/              # Unity WebGL build output
├── Packages/                # Unity packages
├── ProjectSettings/         # Cấu hình Unity project
└── README.md                # Tài liệu này
```

---

## Yêu cầu hệ thống

| Thành phần     | Yêu cầu                                           |
| -------------- | -------------------------------------------------- |
| **Unity**      | 2021.3 LTS trở lên (khuyến nghị 2022.x)           |
| **Node.js**    | v16 trở lên                                        |
| **MongoDB**    | v5.0 trở lên (chạy local hoặc cloud)               |
| **IDE**        | Visual Studio 2022 / VS Code                       |
| **Trình duyệt**| Chrome, Firefox, Edge (hỗ trợ WebGL 2.0)          |

---

## Hướng dẫn cài đặt

### 1. Clone repository

```bash
git clone <repository-url>
cd lt-web-NT208.Q22
```

### 2. Mở project Unity

1. Mở **Unity Hub**.
2. Nhấn **Add** → chọn thư mục `lt-web-NT208.Q22`.
3. Unity sẽ tự động tải các package cần thiết khi mở lần đầu.

### 3. Cài đặt Backend

```bash
cd Backend
npm install
```

### 4. Cài đặt MongoDB

- Cài MongoDB Community Edition và đảm bảo service đang chạy trên `mongodb://127.0.0.1:27017`.
- Hoặc cấu hình connection string trong `Backend/server.js` nếu dùng MongoDB Atlas.

---

## Hướng dẫn chạy

### Chạy trong Unity Editor (Development)

1. Mở project trong Unity.
2. Nhấn nút **Play** để chạy game trực tiếp trong Editor.

### Chạy bản WebGL (Production)

1. **Build WebGL** trong Unity:
   - Vào *File → Build Settings* → chọn **WebGL** → nhấn **Build**.
   - Xuất ra thư mục `Backend/public/`.

2. **Khởi động Backend**:
   ```bash
   cd Backend
   npm start
   ```
   Server sẽ chạy tại `http://localhost:3000`.

3. **Truy cập game**: Mở trình duyệt và vào `http://localhost:3000`.

---

## API Backend

Backend cung cấp các endpoint sau:

| Phương thức | Endpoint           | Mô tả                                    |
| ----------- | ------------------ | ----------------------------------------- |
| `GET`       | `/player/:id`      | Lấy thông tin người chơi theo ID          |
| `POST`      | `/player/save`     | Lưu dữ liệu party & vị trí save point    |
| `GET`       | `/sync`            | Đồng bộ dữ liệu từ file seed vào MongoDB |

### Ví dụ request

```bash
# Lấy thông tin player
curl http://localhost:3000/player/player1

# Lưu dữ liệu
curl -X POST http://localhost:3000/player/save \
  -H "Content-Type: application/json" \
  -d '{"_id": "player1", "party": [...], "lastSavePointId": "sp01", "lastSaveScene": "Map01"}'
```

---

## Công nghệ sử dụng

| Công nghệ       | Vai trò                                   |
| ---------------- | ----------------------------------------- |
| **Unity**        | Game engine – xây dựng gameplay & UI      |
| **C#**           | Ngôn ngữ lập trình game (scripts)         |
| **WebGL**        | Nền tảng triển khai game trên trình duyệt |
| **Node.js**      | Runtime backend                           |
| **Express.js**   | Web framework xử lý REST API             |
| **MongoDB**      | Cơ sở dữ liệu NoSQL lưu trữ player data |
| **Unity Input System** | Quản lý input người chơi            |

---

## Thành viên nhóm

| Vai trò     | Họ và tên          | MSSV       | Phân chia    |
| ----------- | ------------------ | ---------- | ------------ |
| Thành viên  | Trầm Tính Ân       | 24520074   | Nhóm B       |
| Thành viên  | Trần Đức Chuẩn     | 24520228   | Nhóm B       |
| Nhóm trưởng | Nguyễn Mạnh Cường  | 24520238   | Nhóm A       |
| Thành viên  | Nguyễn Tấn Danh    | 24520262   | Nhóm A       |

### 🅰️ Nhóm A — Gameplay & Logic

| STT | Nội dung cần học       | Mục tiêu học                    | Công nghệ / Công cụ   |
| --- | ---------------------- | ------------------------------- | ---------------------- |
| 1   | Unity Editor cơ bản    | Biết tạo scene, prefab, script  | Unity Editor           |
| 2   | C# cơ bản trong Unity  | Viết script, hiểu MonoBehaviour | C#                     |
| 3   | ScriptableObject       | Quản lý dữ liệu nhân vật, skill| ScriptableObject       |
| 4   | Turn-based logic       | Sắp xếp lượt theo SPD          | C# (List, Sort)        |
| 5   | BattleManager          | Quản lý trạng thái trận         | C#                     |
| 6   | Skill & damage         | Tính sát thương                 | C#                     |
| 7   | AI cơ bản              | Chọn mục tiêu, hành động       | Rule-based AI          |

### 🅱️ Nhóm B — UI, Audio & Deployment

| STT | Nội dung cần học       | Mục tiêu học                    | Công nghệ / Công cụ   |
| --- | ---------------------- | ------------------------------- | ---------------------- |
| 1   | Unity UI cơ bản        | Hiểu Canvas, Panel, Button     | Unity UI               |
| 2   | Slider & TextMeshPro   | Hiển thị HP, text               | Slider, TMP            |
| 3   | UI Event               | Kết nối nút với logic           | Unity Event            |
| 4   | UI feedback            | Damage popup, đổi lượt          | C#, TMP                |
| 5   | Audio trong Unity      | Thêm SFX                       | AudioSource            |
| 6   | Save/Load cơ bản       | Lưu & tải tiến trình            | PlayerPrefs / JSON     |
| 7   | WebGL build            | Build game chạy web             | Unity WebGL            |
| 8   | Deploy itch.io         | Upload & test                   | itch.io                |

---

## Giấy phép

Dự án này được phát triển phục vụ mục đích học tập trong khuôn khổ môn học **NT208 – Lập trình ứng dụng Web**, Đại học Công nghệ Thông tin – ĐHQG TP.HCM.
