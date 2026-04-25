<div align="center">

# Turn-Based RPG — Unity WebGL

**NT208.Q22 — Lap trinh ung dung Web**
Truong Dai hoc Cong nghe Thong tin — DHQG TP.HCM

[![Unity](https://img.shields.io/badge/Unity-2022.3_LTS-000000?style=for-the-badge&logo=unity&logoColor=white)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-10.0-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![WebGL](https://img.shields.io/badge/WebGL-2.0-990000?style=for-the-badge&logo=webgl&logoColor=white)](https://www.khronos.org/webgl/)
[![Node.js](https://img.shields.io/badge/Node.js-18+-339933?style=for-the-badge&logo=nodedotjs&logoColor=white)](https://nodejs.org/)
[![Express](https://img.shields.io/badge/Express.js-4.x-000000?style=for-the-badge&logo=express&logoColor=white)](https://expressjs.com/)
[![MongoDB](https://img.shields.io/badge/MongoDB-5.0+-47A248?style=for-the-badge&logo=mongodb&logoColor=white)](https://www.mongodb.com/)

[![Status](https://img.shields.io/badge/Status-In_Development-yellow?style=flat-square)]()
[![License](https://img.shields.io/badge/License-Academic-blue?style=flat-square)]()
[![Platform](https://img.shields.io/badge/Platform-Web_Browser-orange?style=flat-square)]()

</div>

---

## Gioi thieu

Game nhap vai theo luot (Turn-based RPG) trien khai tren nen tang web thong qua **Unity WebGL**. Nguoi choi kham pha ban do, tuong tac voi NPC, thuc hien nhiem vu phan nhanh va chien dau voi quai vat theo co che luot. Du lieu nguoi choi duoc dong bo len server thong qua REST API.

---

## Muc luc

- [Tinh nang chinh](#tinh-nang-chinh)
- [Kien truc he thong](#kien-truc-he-thong)
- [Cau truc thu muc](#cau-truc-thu-muc)
- [Yeu cau he thong](#yeu-cau-he-thong)
- [Huong dan cai dat](#huong-dan-cai-dat)
- [Huong dan chay](#huong-dan-chay)
- [API Backend](#api-backend)
- [Thanh vien nhom](#thanh-vien-nhom)
- [Giay phep](#giay-phep)

---

## Tinh nang chinh

### He thong chien dau theo luot

- Chien dau luot giua party nguoi choi va quai vat
- He thong **Parry** giam sat thuong tu ke thu
- Ky nang tan cong voi hieu ung trang thai: Poison, Stun
- Chon muc tieu linh hoat, ho tro chuyen target
- Co che bo chay (Flee) khoi tran dau
- He thong EXP va phan thuong sau tran dau

### Kham pha ban do

- Di chuyen nhan vat tren ban do 2D bang Unity Input System
- Chuyen canh giua cac khu vuc thong qua MapTrigger va TeleportPillar
- Gap quai ngau nhien khi di vao vung EnemyTrigger
- Camera zone tu dong dieu chinh theo khu vuc
- He thong Minimap hien thi vi tri nguoi choi va cac diem quan trong

### He thong nhiem vu (Quest)

- Nhiem vu phan nhanh voi QuestBranchChoice
- Quan ly muc tieu nhiem vu chi tiet
- Giao dien Quest UI hien thi tien do
- Luu trang thai nhiem vu dong bo voi backend

### He thong luu tru da nen tang

- **Local Storage**: Luu tuc thi vao trinh duyet bang PlayerPrefs
- **Cloud Save**: Dong bo du lieu len server qua REST API
- **Auto-save**: Tu dong luu khi nguoi choi dong hoac tai lai tab
- **Transfer Code**: Tu sinh Guest ID duy nhat, cho phep chuyen du lieu sang may khac
- Hoi sinh tai save point cuoi khi party bi danh bai

### NPC va Tuong tac

- He thong NPC voi hoi thoai va tuong tac
- Kich hoat su kien cau chuyen thong qua NpcTrigger

---

## Kien truc he thong

```
+----------------+        HTTP / REST API        +--------------------+
|  Unity WebGL   |  <------------------------->  |  Node.js Server    |
|  (Frontend)    |       (port 3000)             |  (Express.js)      |
+----------------+                               +---------+----------+
                                                           |
                                                           v
                                                  +--------------------+
                                                  |     MongoDB        |
                                                  |  (webgame DB)      |
                                                  +--------------------+
```

| Tang          | Mo ta                                                           |
| ------------- | --------------------------------------------------------------- |
| **Frontend**  | Unity WebGL build, phuc vu duoi dang static files               |
| **Backend**   | Node.js + Express xu ly REST API (luu/load du lieu nguoi choi)  |
| **Database**  | MongoDB luu tru thong tin player, party va vi tri save point    |

---

## Cau truc thu muc

```
lt-web-NT208.Q22/
├── Assets/
│   ├── Scripts/
│   │   ├── Battle/            # He thong chien dau
│   │   │   ├── Runtime/       # Logic chien dau chinh
│   │   │   └── State/         # Trang thai tran dau
│   │   ├── Core/              # Core gameplay (Combat, Data, Factory, Status, Turn)
│   │   ├── Enemy/             # Du lieu va AI quai vat
│   │   │   ├── Data/          # EnemyAttackData, EnemyData
│   │   │   └── Runtime/       # EnemyStatus, logic runtime
│   │   ├── Map/               # Ban do, NPC, SavePoint, TeleportPillar
│   │   ├── Player/            # Du lieu va runtime nhan vat
│   │   ├── Quest/             # He thong nhiem vu
│   │   ├── Save/              # PlayerSave, UnitSave
│   │   ├── System/            # GameManager, EventManager, InputController
│   │   └── UI/                # Giao dien nguoi dung
│   ├── Editor/                # Editor tools (Teleport, Minimap, Quest setup)
│   └── Data/                  # GameInput, ScriptableObjects
├── Backend/
│   ├── server.js              # Entry point server Node.js
│   ├── package.json           # Dependencies (express, mongodb, cors)
│   ├── Database/              # Seed data (webgame.players.json)
│   └── public/                # Unity WebGL build output
├── Packages/                  # Unity packages
├── ProjectSettings/           # Cau hinh Unity project
└── README.md
```

---

## Yeu cau he thong

| Thanh phan      | Yeu cau                                             |
| --------------- | --------------------------------------------------- |
| **Unity**       | 2021.3 LTS tro len (khuyen nghi 2022.x)            |
| **Node.js**     | v16 tro len                                         |
| **MongoDB**     | v5.0 tro len (local hoac cloud)                     |
| **IDE**         | Visual Studio 2022 / VS Code                        |
| **Trinh duyet** | Chrome, Firefox, Edge (ho tro WebGL 2.0)            |

---

## Huong dan cai dat

### 1. Clone repository

```bash
git clone https://github.com/Nergy197/lt-web-NT208.Q22.git
cd lt-web-NT208.Q22
```

### 2. Mo project Unity

1. Mo **Unity Hub**
2. Nhan **Add** va chon thu muc `lt-web-NT208.Q22`
3. Unity se tu dong tai cac package can thiet khi mo lan dau

### 3. Cai dat Backend

```bash
cd Backend
npm install
```

### 4. Cai dat MongoDB

- Cai MongoDB Community Edition va dam bao service dang chay tren `mongodb://127.0.0.1:27017`
- Hoac cau hinh connection string trong `Backend/server.js` neu dung MongoDB Atlas

---

## Huong dan chay

### Development (Unity Editor)

1. Mo project trong Unity
2. Nhan **Play** de chay game truc tiep trong Editor

### Production (WebGL)

1. Build WebGL trong Unity: *File > Build Settings > WebGL > Build*. Xuat ra thu muc `Backend/public/`

2. Khoi dong Backend:
   ```bash
   cd Backend
   npm start
   ```
   Server chay tai `http://localhost:3000`

3. Truy cap game: Mo trinh duyet va vao `http://localhost:3000`

---

## API Backend

| Phuong thuc | Endpoint           | Mo ta                                     |
| ----------- | ------------------ | ----------------------------------------- |
| `GET`       | `/player/:id`      | Lay thong tin nguoi choi theo ID          |
| `POST`      | `/player/save`     | Luu du lieu party va vi tri save point    |
| `GET`       | `/sync`            | Dong bo du lieu tu file seed vao MongoDB  |

### Vi du request

```bash
# Lay thong tin player bang Guest ID
curl http://localhost:3000/player/guest_a3f8b2c1_slot_0

# Luu du lieu len Cloud
curl -X POST http://localhost:3000/player/save \
  -H "Content-Type: application/json" \
  -d '{"_id": "guest_a3f8b2c1_slot_0", "slotId": 0, "party": [...], "lastSavePointId": "sp01"}'
```

---

## Cong nghe su dung

| Cong nghe            | Vai tro                                    |
| -------------------- | ------------------------------------------ |
| **Unity**            | Game engine — xay dung gameplay va UI      |
| **C#**               | Ngon ngu lap trinh game (scripts)          |
| **WebGL**            | Nen tang trien khai game tren trinh duyet  |
| **Node.js**          | Runtime backend                            |
| **Express.js**       | Web framework xu ly REST API               |
| **MongoDB**          | Co so du lieu NoSQL luu tru player data    |
| **Unity Input System** | Quan ly input nguoi choi                 |
| **TextMeshPro**      | Hien thi text chat luong cao               |

---

## Thanh vien nhom

| Vai tro     | Ho va ten          | MSSV       | Phan nhom    |
| ----------- | ------------------ | ---------- | ------------ |
| Nhom truong | Nguyen Manh Cuong  | 24520238   | Nhom A       |
| Thanh vien  | Nguyen Tan Danh    | 24520262   | Nhom A       |
| Thanh vien  | Tram Tinh An       | 24520074   | Nhom B       |
| Thanh vien  | Tran Duc Chuan     | 24520228   | Nhom B       |

### Nhom A — Gameplay va Logic

| STT | Noi dung               | Muc tieu                          | Cong nghe              |
| --- | ---------------------- | --------------------------------- | ---------------------- |
| 1   | Unity Editor co ban    | Tao scene, prefab, script         | Unity Editor           |
| 2   | C# co ban trong Unity  | Viet script, hieu MonoBehaviour   | C#                     |
| 3   | ScriptableObject       | Quan ly du lieu nhan vat, skill   | ScriptableObject       |
| 4   | Turn-based logic       | Sap xep luot theo SPD             | C# (List, Sort)        |
| 5   | BattleManager          | Quan ly trang thai tran           | C#                     |
| 6   | Skill va damage        | Tinh sat thuong                   | C#                     |
| 7   | AI co ban              | Chon muc tieu, hanh dong          | Rule-based AI          |

### Nhom B — UI, Audio va Deployment

| STT | Noi dung               | Muc tieu                          | Cong nghe              |
| --- | ---------------------- | --------------------------------- | ---------------------- |
| 1   | Unity UI co ban        | Hieu Canvas, Panel, Button        | Unity UI               |
| 2   | Slider va TextMeshPro  | Hien thi HP, text                 | Slider, TMP            |
| 3   | UI Event               | Ket noi nut voi logic             | Unity Event            |
| 4   | UI feedback            | Damage popup, doi luot            | C#, TMP                |
| 5   | Audio trong Unity      | Them SFX                          | AudioSource            |
| 6   | Save/Load co ban       | Luu va tai tien trinh             | PlayerPrefs / JSON     |
| 7   | WebGL build            | Build game chay web               | Unity WebGL            |
| 8   | Deploy itch.io         | Upload va test                    | itch.io                |

---

## Giay phep

Du an nay duoc phat trien phuc vu muc dich hoc tap trong khuon kho mon hoc **NT208 — Lap trinh ung dung Web**, Truong Dai hoc Cong nghe Thong tin — DHQG TP.HCM.
