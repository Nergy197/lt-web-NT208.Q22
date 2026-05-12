# Plan Chapter 1 — Tuyến nhiệm vụ & nối kỹ thuật

Tài liệu này đồng bộ với chuỗi quest trong `Assets/Data/Quest/` (`Q001`–`Q008`). Không sửa file plan trong `.cursor/plans/`.

## Mục tiêu cốt truyện

- **Chủ đề:** Khởi đầu và xung đột nội tâm (Trần Quốc Tuấn).
- **Q001** (đã có trong game): Di nguyện của cha — điểm xuất phát cảm xúc.
- **Q002–Q008:** Chiến tranh Mông Nguyên, thất bại có chủ đích, hậu trận (cứu người + lệnh rút lui), đường rút, chiến thuật vườn không, Đông Bộ Đầu, kết chương hai con đường.

## Chuỗi quest (ScriptableObject)

| Id   | Title                    | Ghi chú ngắn |
|------|--------------------------|--------------|
| Q001 | Di nguyện của cha        | Đã làm; `NextQuests` → Q002 |
| Q002 | Tiếng trống biên ải      | Chuẩn bị ra trận |
| Q003 | Trận chiến đầu tiên      | Battle **thua bắt buộc** |
| Q004 | Lệnh rút lui             | Scene hậu trận, điều khiển Tuấn, cứu binh, ra lệnh rút |
| Q005 | Dấu tro trên đường rút   | Ven đường / làng vắng |
| Q006 | Vườn không nhà trống     | Sơ tán, phá lương, tránh giao chiến lớn |
| Q007 | Đông Bộ Đầu              | Battle thắng + chứng kiến giặc rút |
| Q008 | Hai con đường          | Kết chương, chưa chốt lựa chọn cuối |

## Bảng nối kỹ thuật (scene / trigger)

Cột **Hook** chỉ component hoặc hành động cần cấu hình trong Unity Inspector.

| Quest | Objective (Id) | Ý nghĩa gameplay | Hook gợi ý |
|-------|------------------|------------------|------------|
| Q002 | O1 | Đến điểm họp quân / nhận tin | `QuestZoneTrigger` OnEnterZone |
| Q002 | O2 | Nghe báo cáo biên ải | `QuestAction` OnDialogueEnd |
| Q002 | O3 | Gặp tướng đồng minh | NPC interact → OnDialogueEnd |
| Q002 | O4 | Nhận lệnh xuất phát | NPC / object → OnDialogueEnd |
| Q003 | O1 | Vào trận, pha mở đầu | `BattleQuestTrigger` OnBattleWin hoặc OnAnyEnd tùy script trận |
| Q003 | O2 | Mục tiêu phụ trong trận | Battle script / trigger |
| Q003 | O3 | Áp lực địch | Scripted wave / trigger |
| Q003 | O4 | Thua theo kịch bản | `BattleQuestTrigger` **OnBattleLoss** → `CompleteObjective` + `LoadScene` (xem dưới) |
| Q004 | O1–O7 | Hậu trận: quan sát, cứu binh, lệnh rút, tập kết | `QuestZoneTrigger` + NPC `QuestAction` OnDialogueEnd |
| Q005 | O1–O6 | Đường rút | Zone + NPC |
| Q006 | O1–O5 | Vườn không | Zone + interact |
| Q007 | O1–O5 | Hội quân → battle thắng → quan sát | Zone + `BattleQuestTrigger` OnBattleWin |
| Q008 | O1–O5 | Nội tâm kết chương | Zone + dialogue / UI |

### Q003 — Thua bắt buộc và chuyển scene

1. Trận không có win route (balance + script).
2. Trên `BattleScene`, object có `BattleQuestTrigger`: nhánh **OnBattleLoss** (và/hoặc **onAnyEndActions** nếu dùng điều kiện scripted loss):
   - `QuestAction`: `CompleteObjective` — quest `Q003`, objective `O4` (sau khi O1–O3 đã xong trong trận).
   - `QuestAction`: **LoadScene** — tên scene đích (ví dụ `MapScene` tạm thời, hoặc scene chuyên `Chapter1_Aftermath` khi đã tạo trong Editor).
3. Khi toàn bộ objective `Q003` xong, `QuestManager` tự `StartQuest` **Q004** qua `NextQuests` (player đã ở scene hậu trận).

**Thứ tự gợi ý trong `onLoseActions` (cùng `TriggerOn = OnBattleLoss`):**

| Thứ tự | Action | Quest | ObjectiveId / SceneName |
|--------|--------|-------|---------------------------|
| 1 | CompleteObjective | Q003 | O4 |
| 2 | LoadScene | — | `MapScene` (hoặc scene hậu trận sau khi tạo) |

**Trong trận (OnBattleWin / OnAnyEnd / Manual):** hoàn thành `Q003` `O1`–`O3` theo kịch bản (tutorial + sóng địch), để khi thua thì `O4` là mục cuối và quest đóng → mở `Q004`.

**Lưu ý:** `LoadScene` không cần field `Quest`; chỉ cần `SceneName` khớp tên trong **Build Settings**.

### Luồng scene (đã thống nhất)

- `Chapter0_Introduction` → **`Chapter1_CutScene`** (cùng hướng với new game load cốt truyện chính, tránh nhảy thẳng vào tutorial combat).
- `GameManager.StartGame()` khi không có save: vẫn load **`Chapter1_CutScene`** (giữ hành vi hiện tại; intro Chapter 0 là lựa chọn menu / flow khác nếu có).

## Hướng dẫn triển khai chi tiết

### 0. Điều kiện ban đầu (một lần)

1. **`StartScene`** phải có `QuestManager` với **`AllQuests`** chứa đủ `Q001`…`Q008` (đã cấu hình trong repo). Nút phải chuột component → **Collect All Quests From Chain** nếu thêm quest mới sau này.
2. **`StartingQuests`** chỉ nên có **`Q001`** (new game: chỉ quest đầu active; `Q002` mở khi `Q001` hoàn thành nhờ `NextQuests`).
3. Mọi scene đích của **`LoadScene`** (ví dụ `MapScene`, `Chapter1_Aftermath`) phải có trong **File → Build Settings → Scenes In Build** và tên scene **khớp chính xác** (phân biệt hoa thường theo mặc định Unity).
4. `QuestZoneTrigger` chỉ fire khi collider chạm object có **Tag `Player`** — kiểm tra prefab nhân vật trên map.

### 1. Công cụ Editor có sẵn

Menu **Tools → Quest System**:

| Mục menu | Việc làm |
|-----------|----------|
| **1. Tạo Quest UI Tự Động** | Sinh `QuestUI` + panel (nếu scene chưa có). |
| **2. Tạo NPC có Quest Actions** | Tạo object mẫu gắn `QuestAction` (chỉnh lại list trong Inspector). |
| **3. Tạo Quest Zone Trigger** | Empty + `Collider2D` IsTrigger + `QuestZoneTrigger`. |
| **4. Kiểm tra lỗi hệ thống Quest** | Chạy trước khi build để thấy cảnh báo `StartingQuests` / chain. |
| **5. Liệt kê tất cả QuestSO** | Dò asset trong project. |

### 2. Nguyên tắc gắn `QuestAction`

- **`TriggerOn`**: khớp sự kiện thật (`OnEnterZone`, `OnDialogueEnd`, `OnBattleWin`, `OnBattleLoss`, …).
- **`CompleteObjective`**: bắt buộc kéo **`Quest`** + điền **`ObjectiveId`** đúng ký tự với `QuestSO` (ví dụ `O1`, `O4`, hoặc `Q001` vẫn dùng `01`).
- **`LoadScene`**: điền **`SceneName`**, **`Quest` để trống** (code cho phép).
- **Thứ tự trong list**: Unity chạy lần lượt từ trên xuống. Với **thua Q003**: nên **CompleteObjective O4 trước**, rồi **`LoadScene`** — để `QuestManager` kịp đóng `Q003` và `StartQuest(Q004)` trước khi đổi scene (tránh race tùy frame; nếu lỗi, gom logic vào một script nhỏ gọi tuần tự).

### 3. Triển khai theo từng quest

#### Q001 — `Chapter1_CutScene`

- Khi **kết thúc** tương tác di nguyện (hết thoại giường / cutscene), gọi `QuestManager.Instance.CompleteObjective("Q001", "01")` **hoặc** dùng component có list `QuestAction` + `QuestAction.Execute(..., When.OnDialogueEnd)` ở cuối thoại.
- Sau khi `Q001` completed, **`NextQuests`** tự mở **`Q002`**. Nếu player vẫn ở cutscene, có thể **chưa thấy UI Q002** cho đến khi vào map có `QuestUI` — đảm bảo scene tiếp theo có Canvas + `QuestUI`.

#### Q002 — Map / scene briefing (ví dụ `MapScene` hoặc scene riêng)

- **O1**: `QuestZoneTrigger`, `triggerOnce = true`, `questActions`: `TriggerOn = OnEnterZone`, `CompleteObjective` → `Q002` / `O1`.
- **O2–O4**: NPC hoặc object tương tác: ở cuối hội thoại gọi `QuestAction.Execute(list, QuestAction.When.OnDialogueEnd)` với từng action tương ứng `O2`…`O4`.
- **Chuyển vào battle Q003**: có thể dùng `QuestAction` kiểu **`StartQuest`** không cần (Q003 đã chain sau Q002) — chỉ cần **load `BattleScene`** từ `MapManager` / portal hiện có; đảm bảo khi vào battle thì **`Q003` đã active** (đã xong `Q002`).

#### Q003 — `BattleScene` (thua bắt buộc)

1. **Balance / script trận**: không cho path thắng; có thể buff địch, giới hạn lượt, hoặc kết thúc sớm bằng script gọi logic thua.
2. **Trong trận — O1…O3**:
   - **`onWinActions`**: mỗi action đặt **`TriggerOn = OnBattleWin`** nếu gắn vào sự kiện thắng (trận Q003 thường **không** dùng).
   - **`onAnyEndActions`**: code gọi `QuestAction.Execute(..., When.Manual)` — mọi phần tử trong list phải đặt **`TriggerOn = Manual`** (dùng khi cần chạy sau **mọi** kết thúc trận; **lưu ý** cả thắng và thua đều chạy `onAnyEndActions`, tránh complete nhầm objective chỉ dành cho thua).
   - Hoặc script trong trận (tutorial / wave) gọi thẳng `QuestManager.Instance.CompleteObjective("Q003", "O1")`… tại đúng mốc.
3. **Thua — O4**: trên cùng GameObject **`BattleQuestTrigger`**, mục **`onLoseActions`**:
   - Element 1: `TriggerOn = OnBattleLoss`, `Action = CompleteObjective`, `Quest = Q003`, `ObjectiveId = O4`.
   - Element 2: `TriggerOn = OnBattleLoss`, `Action = LoadScene`, `SceneName = ...` (scene hậu trận).
4. **`BattleManager`**: khi **thua**, block `SaveProgress()` không chạy — nếu cần giữ tiến độ ngay sau thua, thêm **`QuestManager.Instance.SaveProgress()`** vào action thứ 3 (script nhỏ) hoặc sau `LoadScene` ở scene đích (`Awake` bootstrap).

#### Q004 — Scene hậu trận (ưu tiên scene riêng)

- Dùng cùng pattern **`PlayerMovement_Cutscene`** hoặc movement map hiện có; bố trí marker **quan sát**, **NPC thương binh**, **vật thể lệnh rút lui**, **zone tập kết**.
- Mỗi bước: `QuestZoneTrigger` hoặc NPC gọi `CompleteObjective` cho `Q004` / `O1`…`O7` đúng thứ tự thiết kế (hệ thống **không** khóa thứ tự — do bạn đặt trigger không cho skip).

#### Q005 — Q006

- Tiếp tục **map** (có thể cùng scene với Q004 nếu nối tiếp không load, hoặc load scene mới + giữ `DontDestroyOnLoad` `QuestManager`).
- Lặp mẫu zone + NPC như Q002.

#### Q007 — `BattleScene` + map

- **`onWinActions`**: `CompleteObjective` `Q007` / `O3` (và các mục khác nếu gắn vào thắng trận).
- **O1, O2, O4, O5** trên map trước/sau battle: zone + thoại.

#### Q008 — Scene kết chương

- Chủ yếu zone + thoại + UI journal; **không** bắt buộc `BranchChoices` trên `QuestSO` nếu chỉ tượng trưng UI riêng.

### 4. Nối thoại (`OnDialogueEnd`)

`QuanLyHoiThoai` hiện **không** tự gọi quest. Cần một trong các cách:

- Thêm vào **`KetThucThoai()`** hoặc chỗ set **`daXongHetKichBan`**: `QuestAction.Execute(danhSachQuestActions, QuestAction.When.OnDialogueEnd);` với list cấu hình trong Inspector; **hoặc**
- Tạo component nhỏ **`DialogueQuestHook`** (một list `QuestAction`), gọi ở **UnityEvent** / animation event / cuối coroutine thoại.

`TriggerOn` của từng phần tử trong list vẫn phải là **`OnDialogueEnd`** để khớp tham số `Execute`.

### 5. Battle và `MapManager`

- `BattleManager.EndBattle()`: nếu **`MapManager.Instance != null`** thì gọi **`MapManager.EndBattle(playerWon)`** — thường **không** `LoadScene` trùng với `QuestAction.LoadScene`. Khi thiết kế Q003 thua: hoặc **tắt / không dùng** nhánh `MapManager` cho trận này (nếu API cho phép), hoặc để **`LoadScene` của quest chạy sau** và chấp nhận test kỹ thứ tự; hoặc mở rộng code battle để nhận “story loss” không đi qua map mặc định.
- Nếu **`MapManager` null** (test battle thuần), `BattleManager` **tự load `MapScene`** — có thể **đè** `LoadScene` của quest nếu chạy cùng frame; **nên luôn có `MapManager` trong flow thật** hoặc chỉnh `BattleManager` khi làm chapter scripted (là việc ngoài phạm vi asset quest).

### 6. Kiểm thử nhanh

| Bước | Việc làm |
|------|----------|
| 1 | Play từ `StartScene`, new game: có `Q001` active. |
| 2 | Hoàn thành `Q001` (hoặc cheat `CompleteObjective` trong Editor play). |
| 3 | Xác nhận `Q002` hiện trên `QuestUI`. |
| 4 | Vào battle Q003: hoàn thành O1–O3 rồi thua → O4 + đổi scene + `Q004` active. |
| 5 | `Tools/Quest System/4. Kiểm tra lỗi` trước khi commit scene/prefab. |

## Rủi ro / việc còn lại trong Editor (tóm tắt)

- **`BattleScene`**: cần object **`BattleQuestTrigger`** + cấu hình list action (chưa có thì battle không đụng quest).
- **Scene hậu trận Q004**: tạo và add **Build Settings**; đồng bộ tên với **`LoadScene`** của Q003.
- **Thoại**: sửa trong Unity / script hook để gọi **`QuestAction.Execute`** hoặc **`CompleteObjective`** trực tiếp.

## Tham chiếu code

- `QuestSO`, `QuestManager` — `Assets/Scripts/Quest/`
- `QuestAction` (gồm **LoadScene** cho nhánh thua `Q003`), `QuestZoneTrigger`, `BattleQuestTrigger` — `Assets/Scripts/Quest/`, `Assets/Scripts/Map/`
- Cutscene Chapter 1 — `Assets/Scripts/CutScene/Chapter1/`
