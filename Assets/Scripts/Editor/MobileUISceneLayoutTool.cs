using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool chỉnh mobile UI layout cho từng scene riêng biệt.
/// Menu: Tools → Mobile UI Scene Layout
///
/// Workflow:
///   1. Chọn scene ở sidebar trái.
///   2. Chỉnh vị trí / kích thước từng element.
///   3. Nhấn "Kiểm tra overflow" để validate trước khi lưu.
///   4. Nhấn "Lưu Preset" → tạo ScriptableObject trong Assets/Settings/MobileLayouts/.
///   5. (Tuỳ chọn) "Apply → Prefab" để ghi thẳng vào MobileInputCanvas.prefab.
/// </summary>
public class MobileUISceneLayoutTool : EditorWindow
{
    // ── Paths ─────────────────────────────────────────────────────────────────
    const string PREFAB_PATH  = "Assets/Prefabs/UI/Mobile/MobileInputCanvas.prefab";
    const string PRESET_DIR   = "Assets/Settings/MobileLayouts";

    // ── Scene definitions ─────────────────────────────────────────────────────
    static readonly string[] SCENE_NAMES = {
        "Chapter0_Login",
        "Chapter1_Introduction",
        "Chapter2_Tutorial",
        "Chapter3_FatherWill",
        "Chapter4_WarNews",
        "Chapter5_MapBattle",
        "Chapter5a_Battle",
        "Chapter6_Village",
    };

    // ── Element definitions (path trong prefab) ───────────────────────────────
    [System.Serializable]
    class ElemDef
    {
        public string label;
        public string path;
        public bool   canEditAnchor;
    }

    static readonly ElemDef[] ELEM_DEFS = {
        new ElemDef { label="🕹 Joystick Background", path="MapPanel/JoystickBackground",   canEditAnchor=true  },
        new ElemDef { label="  ↳ Joystick Handle",   path="MapPanel/JoystickBackground/JoystickHandle", canEditAnchor=false },
        new ElemDef { label="⏸ Pause Button",        path="PauseButton",                    canEditAnchor=true  },
        new ElemDef { label="🤜 Interact Button",     path="InteractButton",                 canEditAnchor=true  },
        new ElemDef { label="◀ Battle Back",         path="BattleBackButton",               canEditAnchor=true  },
        new ElemDef { label="✓ Battle Confirm",      path="BattleConfirmButton",            canEditAnchor=true  },
        new ElemDef { label="🛡 Battle Parry",        path="BattleParryButton",              canEditAnchor=true  },
    };

    // ── State ─────────────────────────────────────────────────────────────────
    int    selectedScene = 0;
    Vector2 leftScroll;
    Vector2 rightScroll;

    // Working copy của layout đang chỉnh (không tham chiếu trực tiếp asset)
    MobileSceneLayout.ElementLayout[] workingLayouts;
    Vector2Int workingRefRes   = new Vector2Int(1920, 1080);
    float      workingMatch    = 0.5f;

    // Foldout state
    bool[] foldouts;

    string statusMsg   = "";
    Color  statusColor = Color.white;

    // Cache prefab root để đọc current values
    GameObject prefabRoot;

    // ── Menu ──────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Mobile UI Scene Layout")]
    static void Open()
    {
        var w = GetWindow<MobileUISceneLayoutTool>("Mobile UI Scene Layout");
        w.minSize = new Vector2(680, 580);
        w.Show();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void OnEnable()
    {
        prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        InitFoldouts();
        LoadSceneData(selectedScene);
    }

    void InitFoldouts()
    {
        foldouts = new bool[ELEM_DEFS.Length];
        for (int i = 0; i < foldouts.Length; i++) foldouts[i] = true;
    }

    // ── Load data for a scene ─────────────────────────────────────────────────

    void LoadSceneData(int sceneIdx)
    {
        string sceneName = SCENE_NAMES[sceneIdx];
        var preset = LoadPreset(sceneName);

        workingLayouts = new MobileSceneLayout.ElementLayout[ELEM_DEFS.Length];

        for (int i = 0; i < ELEM_DEFS.Length; i++)
        {
            string path = ELEM_DEFS[i].path;

            // Priority: preset → prefab current value → hardcoded default
            var saved = preset?.Get(path);
            if (saved != null)
            {
                workingLayouts[i] = Clone(saved);
            }
            else
            {
                workingLayouts[i] = ReadFromPrefab(path) ?? DefaultLayout(path);
            }
        }

        if (preset != null)
        {
            workingRefRes = preset.referenceResolution;
            workingMatch  = preset.matchWidthOrHeight;
        }
        else
        {
            workingRefRes = new Vector2Int(1920, 1080);
            workingMatch  = 0.5f;
        }

        statusMsg = $"Đã tải: {sceneName}";
        statusColor = Color.cyan;
        Repaint();
    }

    // ── Read / defaults ───────────────────────────────────────────────────────

    MobileSceneLayout.ElementLayout ReadFromPrefab(string path)
    {
        if (prefabRoot == null) return null;
        var t = path == "" ? prefabRoot.transform : prefabRoot.transform.Find(path);
        if (t == null) return null;

        var rt = t.GetComponent<RectTransform>();
        if (rt == null) return null;

        return new MobileSceneLayout.ElementLayout
        {
            path      = path,
            anchorMin = rt.anchorMin,
            anchorMax = rt.anchorMax,
            pivot     = rt.pivot,
            position  = rt.anchoredPosition,
            size      = rt.sizeDelta,
            active    = t.gameObject.activeSelf,
        };
    }

    MobileSceneLayout.ElementLayout DefaultLayout(string path) => path switch
    {
        "MapPanel/JoystickBackground"               => MkLayout(path, V2(0,0), V2(0,0), V2(170,170), V2(300,300)),
        "MapPanel/JoystickBackground/JoystickHandle"=> MkLayout(path, V2(.5f,.5f), V2(.5f,.5f), V2(0,0), V2(150,150)),
        "PauseButton"                               => MkLayout(path, V2(1,1), V2(1,1), V2(-80,-70), V2(120,100)),
        "InteractButton"                            => MkLayout(path, V2(1,0), V2(1,0), V2(-130,130), V2(180,180)),
        "BattleBackButton"                          => MkLayout(path, V2(0,0), V2(0,0), V2(130,130), V2(160,70)),
        "BattleConfirmButton"                       => MkLayout(path, V2(1,0), V2(1,0), V2(-130,130), V2(160,70)),
        "BattleParryButton"                         => MkLayout(path, V2(1,0), V2(1,0), V2(-130,260), V2(160,70)),
        _ => MkLayout(path, V2(0,0), V2(0,0), V2(100,100), V2(100,100))
    };

    static MobileSceneLayout.ElementLayout MkLayout(string path, Vector2 aMin, Vector2 aMax,
                                                     Vector2 pos, Vector2 size)
        => new MobileSceneLayout.ElementLayout
           { path=path, anchorMin=aMin, anchorMax=aMax, pivot=V2(.5f,.5f), position=pos, size=size, active=true };

    static Vector2 V2(float x, float y) => new Vector2(x, y);

    static MobileSceneLayout.ElementLayout Clone(MobileSceneLayout.ElementLayout src)
        => new MobileSceneLayout.ElementLayout
           { path=src.path, anchorMin=src.anchorMin, anchorMax=src.anchorMax,
             pivot=src.pivot, position=src.position, size=src.size, active=src.active };

    // ── Preset IO ─────────────────────────────────────────────────────────────

    static string PresetPath(string sceneName) => $"{PRESET_DIR}/MobileLayout_{sceneName}.asset";

    static MobileSceneLayout LoadPreset(string sceneName)
        => AssetDatabase.LoadAssetAtPath<MobileSceneLayout>(PresetPath(sceneName));

    void SavePreset(int sceneIdx)
    {
        string sceneName = SCENE_NAMES[sceneIdx];
        string assetPath = PresetPath(sceneName);

        if (!AssetDatabase.IsValidFolder(PRESET_DIR))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "../", PRESET_DIR));
            AssetDatabase.Refresh();
        }

        var preset = LoadPreset(sceneName);
        bool isNew = preset == null;
        if (isNew)
            preset = ScriptableObject.CreateInstance<MobileSceneLayout>();

        preset.referenceResolution = workingRefRes;
        preset.matchWidthOrHeight  = workingMatch;

        preset.elements = new List<MobileSceneLayout.ElementLayout>();
        foreach (var el in workingLayouts)
            preset.elements.Add(Clone(el));

        if (isNew)
            AssetDatabase.CreateAsset(preset, assetPath);
        else
            EditorUtility.SetDirty(preset);

        AssetDatabase.SaveAssets();
        statusMsg   = $"✓ Đã lưu preset: {assetPath}";
        statusColor = Color.green;
    }

    // ── GUI ───────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.BeginHorizontal();

        // Left: scene list
        DrawSceneList();

        // Separator
        var sepRect = EditorGUILayout.GetControlRect(GUILayout.Width(4), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(sepRect, new Color(0.1f, 0.1f, 0.1f, 0.8f));

        // Right: element editor
        DrawElementEditor();

        EditorGUILayout.EndHorizontal();
    }

    void DrawHeader()
    {
        EditorGUILayout.LabelField("Mobile UI Scene Layout", EditorStyles.whiteLargeLabel);
        EditorGUILayout.HelpBox(
            "Chỉnh layout mobile cho từng scene riêng.\n" +
            "1) Chọn scene ở sidebar.  2) Chỉnh các element.  " +
            "3) Kiểm tra overflow.  4) Lưu Preset hoặc Apply → Prefab.",
            MessageType.Info);

        if (!string.IsNullOrEmpty(statusMsg))
        {
            var s = new GUIStyle(EditorStyles.helpBox);
            s.normal.textColor = statusColor;
            EditorGUILayout.LabelField(statusMsg, s);
        }

        EditorGUILayout.Space(2);
    }

    void DrawSceneList()
    {
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll,
            GUILayout.Width(200), GUILayout.ExpandHeight(true));

        EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        for (int i = 0; i < SCENE_NAMES.Length; i++)
        {
            bool isSelected = i == selectedScene;
            bool hasPreset  = LoadPreset(SCENE_NAMES[i]) != null;

            var style = new GUIStyle(EditorStyles.miniButton);
            style.normal.textColor  = isSelected ? Color.white :
                                      hasPreset  ? new Color(0.5f, 1f, 0.5f) : Color.gray;
            style.fontStyle         = isSelected ? FontStyle.Bold : FontStyle.Normal;

            string label = (hasPreset ? "● " : "○ ") + SCENE_NAMES[i];

            if (GUILayout.Button(label, style, GUILayout.Height(26)))
            {
                selectedScene = i;
                InitFoldouts();
                LoadSceneData(i);
            }
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("● = có preset  ○ = dùng prefab", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndScrollView();
    }

    void DrawElementEditor()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        // Canvas Scaler
        EditorGUILayout.LabelField($"Scene: {SCENE_NAMES[selectedScene]}", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Canvas Scaler", EditorStyles.boldLabel);
        workingRefRes = EditorGUILayout.Vector2IntField("Reference Resolution", workingRefRes);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Match W/H", GUILayout.Width(100));
        workingMatch = EditorGUILayout.Slider(workingMatch, 0f, 1f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1920×1080", EditorStyles.miniButton)) { workingRefRes = new Vector2Int(1920,1080); workingMatch=0.5f; }
        if (GUILayout.Button("1280×720",  EditorStyles.miniButton)) { workingRefRes = new Vector2Int(1280,720);  workingMatch=0.5f; }
        if (GUILayout.Button("1080×1920 Portrait", EditorStyles.miniButton)) { workingRefRes = new Vector2Int(1080,1920); workingMatch=0.5f; }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // Overflow warning box
        var overflows = CheckOverflows();
        if (overflows.Count > 0)
        {
            EditorGUILayout.HelpBox(
                "⚠ Phát hiện overflow (element tràn ra ngoài canvas):\n" +
                string.Join("\n", overflows),
                MessageType.Warning);
        }

        // Elements scroll
        rightScroll = EditorGUILayout.BeginScrollView(rightScroll, GUILayout.ExpandHeight(true));

        for (int i = 0; i < ELEM_DEFS.Length; i++)
        {
            if (i < workingLayouts.Length)
                DrawElement(i);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        DrawPresetButtons();

        EditorGUILayout.EndVertical();
    }

    void DrawElement(int i)
    {
        var def = ELEM_DEFS[i];
        var el  = workingLayouts[i];

        bool prefabHas = prefabRoot != null &&
                         prefabRoot.transform.Find(def.path) != null;

        var bgCol = !prefabHas ? new Color(0.4f, 0.15f, 0.15f) : new Color(0.18f, 0.2f, 0.22f);
        var oldBg = GUI.backgroundColor;
        GUI.backgroundColor = bgCol;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = oldBg;

        foldouts[i] = EditorGUILayout.Foldout(foldouts[i],
            def.label + (!prefabHas ? " ⚠ không có trong prefab" : ""), true,
            EditorStyles.foldoutHeader);

        if (foldouts[i])
        {
            EditorGUI.indentLevel++;

            // Active toggle
            el.active = EditorGUILayout.Toggle(new GUIContent("Active", "Hiện/ẩn element này"), el.active);

            using (new EditorGUI.DisabledScope(!el.active))
            {
                // Anchor
                if (def.canEditAnchor)
                {
                    el.anchorMin = EditorGUILayout.Vector2Field(
                        new GUIContent("Anchor Min", "0=trái/dưới  1=phải/trên"), el.anchorMin);
                    el.anchorMax = EditorGUILayout.Vector2Field(
                        new GUIContent("Anchor Max", "Giống anchorMin để dùng corner anchor"), el.anchorMax);
                    el.anchorMin.x = Mathf.Clamp01(el.anchorMin.x);
                    el.anchorMin.y = Mathf.Clamp01(el.anchorMin.y);
                    el.anchorMax.x = Mathf.Clamp01(el.anchorMax.x);
                    el.anchorMax.y = Mathf.Clamp01(el.anchorMax.y);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Vector2Field("Anchor Min (cố định)", el.anchorMin);
                    EditorGUI.EndDisabledGroup();
                }

                el.position = EditorGUILayout.Vector2Field(
                    new GUIContent("Position", "Offset pixel từ điểm anchor. Âm = vào trong màn hình."),
                    el.position);
                el.size = EditorGUILayout.Vector2Field(
                    new GUIContent("Size (px)", "Kích thước theo canvas reference."),
                    el.size);

                // Overflow indicator for this element
                string overflowMsg = CheckElementOverflow(el);
                if (!string.IsNullOrEmpty(overflowMsg))
                    EditorGUILayout.HelpBox(overflowMsg, MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    void DrawPresetButtons()
    {
        // Preset quick-fill
        EditorGUILayout.LabelField("Preset nhanh cho scene này", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Joystick nhỏ (300px)")) ApplyJoystickPreset(300, 150);
        if (GUILayout.Button("Joystick vừa (400px)")) ApplyJoystickPreset(400, 200);
        if (GUILayout.Button("Joystick lớn (500px)")) ApplyJoystickPreset(500, 250);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Main actions
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🔍 Kiểm tra Overflow", GUILayout.Height(30)))
        {
            var ov = CheckOverflows();
            statusMsg   = ov.Count == 0 ? "✓ Không có overflow!" : $"⚠ {ov.Count} element có thể tràn.";
            statusColor = ov.Count == 0 ? Color.green : Color.yellow;
        }

        if (GUILayout.Button("💾 Lưu Preset", GUILayout.Height(30)))
        {
            SavePreset(selectedScene);
            Repaint();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("📥 Đọc từ Prefab", GUILayout.Height(30)))
        {
            prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            LoadSceneData(selectedScene);
        }

        if (GUILayout.Button("🔧 Apply → Prefab", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Apply to Prefab",
                $"Ghi layout scene '{SCENE_NAMES[selectedScene]}' vào MobileInputCanvas.prefab?\n" +
                "(Các scene khác sẽ bị ảnh hưởng nếu không dùng Runtime Preset Override.)",
                "Apply", "Hủy"))
                ApplyToPrefab();
        }

        if (GUILayout.Button("↩ Reset scene", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Reset", "Reset về giá trị prefab hiện tại?", "OK", "Hủy"))
            {
                prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                for (int i = 0; i < ELEM_DEFS.Length; i++)
                    workingLayouts[i] = ReadFromPrefab(ELEM_DEFS[i].path) ?? DefaultLayout(ELEM_DEFS[i].path);
                statusMsg   = "Đã reset về giá trị prefab.";
                statusColor = Color.cyan;
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 Copy sang tất cả scenes", GUILayout.Height(26)))
        {
            if (EditorUtility.DisplayDialog("Copy to All",
                $"Copy layout '{SCENE_NAMES[selectedScene]}' sang TẤT CẢ scenes?",
                "Copy", "Hủy"))
                CopyToAllScenes();
        }
        if (GUILayout.Button("🗑 Xóa preset scene này", GUILayout.Height(26)))
        {
            if (EditorUtility.DisplayDialog("Xóa Preset",
                $"Xóa preset của scene '{SCENE_NAMES[selectedScene]}'?", "Xóa", "Hủy"))
            {
                string path = PresetPath(SCENE_NAMES[selectedScene]);
                if (AssetDatabase.DeleteAsset(path))
                {
                    statusMsg   = $"Đã xóa preset: {path}";
                    statusColor = Color.yellow;
                    LoadSceneData(selectedScene);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // ── Overflow detection ────────────────────────────────────────────────────

    List<string> CheckOverflows()
    {
        var result = new List<string>();
        for (int i = 0; i < workingLayouts.Length; i++)
        {
            string msg = CheckElementOverflow(workingLayouts[i]);
            if (!string.IsNullOrEmpty(msg))
                result.Add($"{ELEM_DEFS[i].label}: {msg}");
        }
        return result;
    }

    string CheckElementOverflow(MobileSceneLayout.ElementLayout el)
    {
        if (!el.active) return "";

        float W = workingRefRes.x;
        float H = workingRefRes.y;

        // Corner anchor: pivot center, so element spans [pos - size/2, pos + size/2] from anchor point
        float anchorPixelX = el.anchorMin.x * W;
        float anchorPixelY = el.anchorMin.y * H;

        float left   = anchorPixelX + el.position.x - el.size.x * 0.5f;
        float right  = anchorPixelX + el.position.x + el.size.x * 0.5f;
        float bottom = anchorPixelY + el.position.y - el.size.y * 0.5f;
        float top    = anchorPixelY + el.position.y + el.size.y * 0.5f;

        var issues = new List<string>();
        if (left < 0)   issues.Add($"trái tràn {-left:F0}px");
        if (right > W)  issues.Add($"phải tràn {right-W:F0}px");
        if (bottom < 0) issues.Add($"dưới tràn {-bottom:F0}px");
        if (top > H)    issues.Add($"trên tràn {top-H:F0}px");

        return issues.Count > 0 ? string.Join(", ", issues) : "";
    }

    // ── Preset helpers ────────────────────────────────────────────────────────

    void ApplyJoystickPreset(float bgSize, float handleSize)
    {
        for (int i = 0; i < ELEM_DEFS.Length; i++)
        {
            if (ELEM_DEFS[i].path.EndsWith("JoystickBackground"))
            {
                workingLayouts[i].size     = new Vector2(bgSize, bgSize);
                float half = bgSize / 2f + 20;
                workingLayouts[i].position  = new Vector2(half, half);
                workingLayouts[i].anchorMin = Vector2.zero;
                workingLayouts[i].anchorMax = Vector2.zero;
            }
            else if (ELEM_DEFS[i].path.EndsWith("JoystickHandle"))
            {
                workingLayouts[i].size = new Vector2(handleSize, handleSize);
            }
        }
    }

    void CopyToAllScenes()
    {
        for (int i = 0; i < SCENE_NAMES.Length; i++)
        {
            int prev = selectedScene;
            selectedScene = i;
            SavePreset(i);
            selectedScene = prev;
        }
        statusMsg   = $"✓ Đã copy layout sang {SCENE_NAMES.Length} scenes.";
        statusColor = Color.green;
    }

    // ── Apply to prefab ───────────────────────────────────────────────────────

    void ApplyToPrefab()
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "../", PREFAB_PATH)))
        {
            statusMsg   = "✗ Không tìm thấy prefab!";
            statusColor = Color.red;
            return;
        }

        using var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH);
        var root = scope.prefabContentsRoot;

        // Fix CanvasScaler
        var scaler = root.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(workingRefRes.x, workingRefRes.y);
            scaler.matchWidthOrHeight  = workingMatch;
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        int changed = 0;
        for (int i = 0; i < ELEM_DEFS.Length; i++)
        {
            string path = ELEM_DEFS[i].path;
            var el = workingLayouts[i];

            Transform t = path == "" ? root.transform : root.transform.Find(path);
            if (t == null)
            {
                Debug.LogWarning($"[MobileUISceneLayout] Không tìm thấy: {path}");
                continue;
            }

            t.gameObject.SetActive(el.active);

            var rt = t.GetComponent<RectTransform>();
            if (rt == null) continue;

            if (ELEM_DEFS[i].canEditAnchor)
            {
                rt.anchorMin = el.anchorMin;
                rt.anchorMax = el.anchorMax;
            }

            rt.pivot            = el.pivot;
            rt.anchoredPosition = el.position;
            rt.sizeDelta        = el.size;
            changed++;
        }

        statusMsg   = $"✓ Đã apply {changed} element vào prefab (CanvasScaler: {workingRefRes.x}×{workingRefRes.y}, match={workingMatch:F2}).";
        statusColor = Color.green;

        // Reload display
        prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        Repaint();
    }
}
