# Plan: Tối ưu Mobile & Đánh giá xuất APK

> Phân tích dựa trên: ProjectSettings.asset, QualitySettings.asset, 95 C# scripts, 341 sprites (17MB), URP pipeline.

---

## Đánh giá khả năng xuất APK — Tổng quan

| Hạng mục | Trạng thái | Mức độ |
|----------|-----------|--------|
| Engine hỗ trợ Android | Unity 2022.3 LTS ✅ | Sẵn sàng |
| Render pipeline (URP) | URP ✅ | Tốt cho mobile |
| Mobile input (joystick) | Có cơ bản ✅ | Cần nâng cấp |
| Application ID (Bundle ID) | ❌ Chưa set | **Chặn build** |
| Scripting Backend | ❌ Mono (chưa set IL2CPP) | **Cần đổi** |
| Texture compression (ETC2/ASTC) | ❌ Chưa cấu hình | Build nặng |
| Frame rate cap | ❌ Chưa set | Hao pin |
| Screen orientation lock | ❌ Auto-rotate | Layout vỡ |
| Safe Area / Notch | ❌ `renderOutsideSafeArea: 1` | UI bị che |
| Âm thanh | ❌ Không có file audio | Game câm |
| Company / Product name | ❌ `DefaultCompany` / `core_gameplay_test` | Cần đổi |

**Kết luận:** Game **có thể build APK ngay** nhưng sẽ có nhiều vấn đề nghiêm trọng — build rất nặng (~200MB+), hao pin, UI vỡ trên notch, không có âm thanh. Cần 3–5 ngày để đạt mức chất lượng có thể phát hành.

---

## Phần A — Cấu hình Build (Chặn xuất APK)

### A-1: Application ID và thông tin App

**Vấn đề:** `applicationIdentifier` trống, `companyName: DefaultCompany`, `productName: core_gameplay_test`. APK không thể lên Play Store và dễ bị conflict với app khác trên máy.

**Cách fix — Project Settings → Player:**
```
Company Name:     [Tên nhóm của bạn]       ví dụ: NT208Team
Product Name:     [Tên game]               ví dụ: ChienThuat2D
Bundle Identifier: com.[company].[game]    ví dụ: com.nt208team.chienthuat2d
Version:          1.0.0
Bundle Version Code: 1
```

---

### A-2: Scripting Backend → IL2CPP

**Vấn đề:** `scriptingBackend: {}` (trống) = mặc định Mono. Mono tạo APK lớn hơn, chậm hơn, và **không được chấp nhận trên Google Play Store 64-bit requirement**.

**Cách fix — Project Settings → Player → Android → Other Settings:**
- **Scripting Backend:** `IL2CPP`
- **Target Architectures:** `ARM64` ✅ (đã set đúng, value = 2)
- **Managed Stripping Level:** `Medium` (giảm size mà không vỡ game)

> ⚠️ IL2CPP yêu cầu cài **Android NDK** trong Unity Hub. Nếu chưa có: Unity Hub → Installs → Add Modules → Android Build Support (bao gồm NDK và JDK).

---

### A-3: Keystore để ký APK

APK phải được ký mới cài được trên Android. Unity tự tạo debug keystore cho dev, nhưng để release cần keystore riêng.

**Cách tạo — Project Settings → Player → Android → Publishing Settings:**
1. `Keystore Manager → Create new` → nhập alias, password
2. Lưu file `.keystore` vào nơi an toàn (KHÔNG commit lên git)
3. Thêm `.keystore` vào `.gitignore`

---

## Phần B — Tối ưu Texture (Ảnh hưởng lớn nhất đến build size)

### B-1: Thiếu Android Texture Compression

**Vấn đề phát hiện:** Tất cả 341 sprites đều dùng `textureCompression: 0` (không nén) và không có Android platform override. Trên Android, Unity sẽ không tự nén → build size phình to, RAM tăng.

**Ước tính tác động:**
| Trạng thái | Build Size | RAM Textures |
|-----------|-----------|-------------|
| Hiện tại (không nén) | ~250–350MB | ~80–120MB |
| Sau khi nén ETC2/ASTC | ~80–120MB | ~30–50MB |

**Cách fix — Trong Unity Texture Import Settings:**

Có 2 cách:

**Cách 1 — Thủ công từng texture (không khuyến nghị với 341 sprites):**
- Chọn texture → Inspector → Platform override → Android
- Format: `ASTC 6x6` (cân bằng chất lượng/size) hoặc `ETC2 RGBA8`

**Cách 2 — Editor Script để set hàng loạt (khuyến nghị):**
```csharp
// Assets/Editor/AndroidTextureOptimizer.cs
using UnityEditor;
using UnityEngine;

public class AndroidTextureOptimizer
{
    [MenuItem("Tools/Set Android Texture Compression")]
    static void SetAllTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites", "Assets/Data" });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            // Lấy hoặc tạo Android platform setting
            TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
            androidSettings.overridden = true;
            androidSettings.maxTextureSize = GetMaxSize(importer);
            androidSettings.format = TextureImporterFormat.ASTC_6x6;  // tốt cho sprite có alpha
            androidSettings.compressionQuality = 50;

            importer.SetPlatformTextureSettings(androidSettings);
            EditorUtility.SetDirty(importer);
            AssetDatabase.ImportAsset(path);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[AndroidTexOpt] Đã set ASTC 6x6 cho {count} textures.");
    }

    static int GetMaxSize(TextureImporter importer)
    {
        // UI sprites: giữ 2048; character sprites: 512; background: 1024
        if (importer.assetPath.Contains("/UI/")) return 2048;
        if (importer.assetPath.Contains("Background") || importer.assetPath.Contains("Map")) return 1024;
        return 512;
    }
}
```

### B-2: Sprite Atlas

**Vấn đề:** 341 sprites rời nhau = 341 draw calls riêng lẻ khi render. Mỗi sprite là 1 texture bind → GPU pipeline tắt nghẽn.

**Cách fix — Tạo Sprite Atlas:**
```
Assets → Create → 2D → Sprite Atlas
```

Gợi ý tổ chức 3 atlas:
| Atlas | Nội dung | Ước tính |
|-------|---------|---------|
| `Atlas_Characters` | Sprite nhân vật, enemy (battle) | ~80 sprites |
| `Atlas_UI` | Tất cả UI icons, buttons, frames | ~120 sprites |
| `Atlas_Map` | Tiles, decoration map | ~141 sprites |

Sau khi có Sprite Atlas: Unity tự gộp draw calls → giảm 60–80% draw calls trên map scene.

---

## Phần C — Tối ưu Code (Performance)

### C-1: `Camera.main` trong Update() — Critical

**File:** [Assets/Scripts/UI/EnemyHPBar.cs](Assets/Scripts/UI/EnemyHPBar.cs#L19)

**Vấn đề:** `Camera.main` gọi `FindGameObjectWithTag("MainCamera")` bên trong — đây là scene scan mỗi frame. Với nhiều enemy HP bars cùng lúc, đây là hotspot.

```csharp
// Hiện tại — BAD: scan scene mỗi frame Update()
void Update()
{
    // cam = Camera.main; // đã cache trong Start() — OK
    // Nhưng nếu cam null (scene reload), Update vẫn chạy
}
```

**Fix:** Đã cache trong `Start()` — tốt. Tuy nhiên cần guard khi camera bị destroy và recreate giữa scene:
```csharp
void Update()
{
    if (cam == null) cam = Camera.main;  // re-cache nếu cần, không gọi mỗi frame
    // ...
}
```

Tương tự [Assets/Scripts/Map/Minimap.cs](Assets/Scripts/Map/Minimap.cs#L159) — cache `Camera.main` vào biến member thay vì gọi trong LateUpdate.

---

### C-2: `FindObjectsByType` trong Minimap — Critical trên Mobile

**File:** [Assets/Scripts/Map/Minimap.cs](Assets/Scripts/Map/Minimap.cs#L365)

**Vấn đề:** `FindObjectsByType<Tilemap>()` và `FindObjectsByType<SpriteRenderer>()` scan toàn bộ scene graph. Trên mobile với tilemap lớn, đây có thể tốn 5–20ms một lần gọi (tức là lag spike).

**Fix:** Chỉ gọi 1 lần khi setup minimap, lưu kết quả vào List:
```csharp
// Minimap.cs — cache trong Start() hoặc khi SetupMinimap()
private Tilemap[] _cachedTilemaps;
private SpriteRenderer[] _cachedRenderers;

void SetupMinimap()
{
    _cachedTilemaps  = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
    _cachedRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
    // Dùng cache trong các hàm sau
}
```

---

### C-3: Minimap Render Frequency

**File:** [Assets/Scripts/Map/Minimap.cs](Assets/Scripts/Map/Minimap.cs)

**Vấn đề:** `minimapCamera.Render()` trong LateUpdate (dù có frame skip N). Manual Camera.Render() vẫn tốn kém trên mobile vì phải render toàn bộ minimap layer.

**Cách tối ưu:**
```csharp
// Thay vì render mỗi N frame cố định, render khi player di chuyển đủ xa
private Vector3 _lastPlayerPos;
private const float MINIMAP_UPDATE_DISTANCE = 0.5f;  // chỉ update khi đi 0.5 unit

void LateUpdate()
{
    if (playerTransform == null) return;

    float dist = Vector3.Distance(playerTransform.position, _lastPlayerPos);
    if (dist < MINIMAP_UPDATE_DISTANCE) return;

    _lastPlayerPos = playerTransform.position;
    UpdateMinimapPosition();
    RenderMinimapManually();
}
```

---

### C-4: Thiếu Frame Rate Cap — Hao pin nghiêm trọng

**Vấn đề:** Không có `Application.targetFrameRate` → game chạy tốc độ tối đa của màn hình (90/120/144Hz trên flagship phones). Game 2D turn-based không cần 120fps — hao pin vô ích.

**Fix — thêm vào `GameManager.Awake()`:**
```csharp
void Awake()
{
    // ...
#if UNITY_ANDROID || UNITY_IOS
    Application.targetFrameRate = 60;
    QualitySettings.vSyncCount = 0;  // tắt vsync, dùng targetFrameRate
#endif
}
```

---

### C-5: GC Pressure — String Allocations

**Phát hiện:** 74 chỗ tạo string mới (`$"..."`, `string +`) trong gameplay code. Mỗi string allocation là 1 GC object → gây GC spike (lag nhỏ nhưng liên tục trên mobile).

**Điểm cần ưu tiên sửa:**
- `Debug.Log($"...")` trong Update/FixedUpdate — tắt trong release build
- String format trong `UnitHUD.UpdateDisplay()` — dùng `StringBuilder` hoặc cache

```csharp
// Thêm vào đầu các file có nhiều Debug.Log:
#if !UNITY_EDITOR
    // Disable debug logs in release
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    static void DebugLog(string msg) => Debug.Log(msg);
#endif
```

Hoặc dùng global define symbol `DISABLE_LOGS` trong Build Settings.

---

### C-6: Object Pooling cho Battle Enemies

**Vấn đề:** `Instantiate` / `Destroy` enemy GameObject mỗi trận → GC spike khi vào/thoát battle. Với 15 Instantiate calls tìm thấy.

**Fix cơ bản — Simple Pool:**
```csharp
// BattleManager.cs — thay thế Instantiate/Destroy
public class SimplePool
{
    readonly GameObject prefab;
    readonly Queue<GameObject> pool = new();

    public SimplePool(GameObject prefab) => this.prefab = prefab;

    public GameObject Get(Transform parent)
    {
        var obj = pool.Count > 0 ? pool.Dequeue() : Object.Instantiate(prefab, parent);
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

---

## Phần D — Tối ưu Render / GPU

### D-1: URP Asset — Tắt tính năng không dùng

**Vấn đề:** `m_StripUnusedPostProcessingVariants: 0` — Unity không strip shader variants của Post Processing dù game 2D không dùng shadow, reflection probe, hay post FX phức tạp. Shader bundle phình to.

**Cách fix — Edit URP Renderer Asset:**
- Tắt: `Depth Texture`, `Opaque Texture` (nếu không dùng shader refraction)
- Tắt: `HDR`, `MSAA` (2D pixel art không cần)
- `Render Scale`: 1.0 → có thể giảm xuống 0.85 trên low-end devices

**Trong `UniversalRenderPipelineGlobalSettings.asset`** — đã set:
- `m_StripUnusedPostProcessingVariants: 0` → đổi thành `1`

---

### D-2: RenderTexture cho Minimap

**Vấn đề:** `MinimapRT.renderTexture` là extra render target. Mỗi frame (hoặc khi render thủ công) GPU phải switch render target — tốn băng thông memory trên mobile GPU (đặc biệt tile-based GPU như Adreno, Mali).

**Tối ưu:**
- Giảm resolution MinimapRT từ mặc định (thường 1024×1024) xuống **256×256** — minimap không cần độ phân giải cao
- Trong Unity: chọn `MinimapRT.renderTexture` → đổi Size về 256×256

---

### D-3: Linear Color Space trên Mobile

**Phát hiện:** `m_ActiveColorSpace: 1` = Linear color space. Tốt cho chất lượng màu nhưng trên GPU mobile cũ (OpenGL ES 2.0), linear rendering chậm hơn Gamma.

**Quyết định:**
- Nếu game target Android 8+ (API 26+): giữ Linear → OK (hầu hết GPU hỗ trợ)
- Nếu cần hỗ trợ máy cũ hơn: đổi về Gamma space

---

## Phần E — Tính năng thiếu nghiêm trọng cho APK

### E-1: Không có Âm thanh (Critical cho UX)

**Phát hiện:** Không có folder `Assets/Audio`, 0 file `.mp3/.wav/.ogg`. Game hoàn toàn **câm** trên APK.

**Roadmap Audio tối thiểu:**
| Loại | Ưu tiên | Gợi ý |
|------|---------|-------|
| Nhạc nền Map | Cao | 1 track loop, 1–2MB .ogg |
| Nhạc nền Battle | Cao | 1 track loop |
| SFX: tấn công, nhận hit | Cao | 5–10 clips ngắn |
| SFX: level up, save | Trung bình | 3–5 clips |
| SFX: UI click | Thấp | 1–2 clips |

**Setup AudioMixer** (kết hợp với UX-2 trong plan-ux-gameplay.md):
1. Tạo `Assets/Audio/BGM/` và `Assets/Audio/SFX/`
2. Import nhạc dạng `.ogg` (nén tốt hơn mp3 trên Unity)
3. Gán `Load Type: Streaming` cho BGM (không load hết vào RAM), `Decompress on Load` cho SFX ngắn

---

### E-2: Managed Stripping Level chưa set

**Phát hiện:** `managedStrippingLevel: {}` = trống → Unity dùng `Disabled` mặc định → không strip unused code → build size lớn hơn 20–40MB.

**Fix — Project Settings → Player → Other Settings:**
- `Managed Stripping Level: Medium`
- Test kỹ sau khi set (stripping aggressive có thể xóa class cần thiết cho Reflection/JsonUtility)

---

## Phần F — Checklist Build APK

### F-1: Trước khi build lần đầu

- [ ] **A-1:** Set Bundle ID, Company Name, Product Name
- [ ] **A-2:** Đổi Scripting Backend → IL2CPP, cài NDK trong Unity Hub
- [ ] **A-3:** Tạo Keystore và cấu hình signing
- [ ] **MOB-6** *(từ plan-ux-gameplay.md)*: Lock orientation → Landscape Left
- [ ] **MOB-1** *(từ plan-ux-gameplay.md)*: Tắt `androidRenderOutsideSafeArea`
- [ ] Set `Application.targetFrameRate = 60` trong GameManager

### F-2: Tối ưu Build Size

- [ ] **B-1:** Chạy `AndroidTextureOptimizer` Editor script → set ASTC 6x6
- [ ] **B-2:** Tạo 3 Sprite Atlas (Characters, UI, Map)
- [ ] **E-2:** Set Managed Stripping Level → Medium
- [ ] Giảm MinimapRT về 256×256
- [ ] Strip Post Processing shader variants

### F-3: Tối ưu Runtime Performance

- [ ] **C-1:** Fix Camera.main re-cache trong EnemyHPBar.Update()
- [ ] **C-2:** Cache FindObjectsByType trong Minimap.SetupMinimap()
- [ ] **C-3:** Minimap render theo distance thay vì fixed frame
- [ ] **C-4:** targetFrameRate = 60 trên mobile
- [ ] **C-6:** Pool enemy GameObjects trong BattleManager
- [ ] Tắt Debug.Log trong release build

### F-4: Tính năng cần có trước release

- [ ] **E-1:** Có ít nhất nhạc nền Map + Battle + SFX attack/hit
- [ ] **MOB-3:** Nút Pause trên mobile
- [ ] **MOB-4:** Android Back button không thoát app thẳng
- [ ] SafeAreaFitter trên tất cả mobile UI panels

---

## Ước tính thời gian

```
Giai đoạn 1 — Build được APK (1 ngày):
├── A-1: App ID + tên game          (30 phút)
├── A-2: IL2CPP + NDK cài đặt       (1 giờ)
├── A-3: Keystore                   (30 phút)
├── MOB-6: Lock orientation         (30 phút)
└── C-4: targetFrameRate            (15 phút)

Giai đoạn 2 — APK chất lượng tốt (2–3 ngày):
├── B-1: Texture compression script  (2 giờ)
├── B-2: Sprite Atlas x3             (3–4 giờ)
├── C-1/C-2/C-3: Code performance    (2 giờ)
├── E-2: Stripping level             (30 phút)
└── MOB-1: Safe Area                 (2 giờ)

Giai đoạn 3 — APK chất lượng phát hành (3–5 ngày):
├── E-1: Audio (tìm asset + tích hợp) (2–3 ngày)
├── C-6: Object Pool                  (3 giờ)
├── MOB-3/4: Back button + Pause btn  (2 giờ)
└── D-1: URP Asset optimization       (1 giờ)
```

---

## Tham chiếu nhanh — Android SDK versions

| Setting | Giá trị gợi ý | Lý do |
|---------|--------------|-------|
| Min API Level | 26 (Android 8.0) | Bắt buộc cho IL2CPP ARM64 |
| Target API Level | 34 (Android 14) | Google Play yêu cầu từ 2024 |
| Scripting Backend | IL2CPP | Play Store 64-bit requirement |
| Target Architecture | ARM64 | ✅ Đã set đúng |
