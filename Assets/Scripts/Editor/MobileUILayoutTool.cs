using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool chỉnh layout tất cả element của MobileInputCanvas.
/// Menu: Tools → Mobile UI Layout
/// </summary>
public class MobileUILayoutTool : EditorWindow
{
    const string PREFAB_PATH = "Assets/Prefabs/UI/Mobile/MobileInputCanvas.prefab";

    // ── Phần tử cần chỉnh ────────────────────────────────────────────────────

    [System.Serializable]
    class ElementConfig
    {
        public string label;
        public string path;          // path trong prefab (e.g. "MapPanel/JoystickBackground")
        public Vector2 anchor;       // anchorMin = anchorMax = anchor (corner anchor)
        public Vector2 position;     // anchoredPosition (offset từ anchor)
        public Vector2 size;         // sizeDelta
        public bool canEditAnchor;
        public bool foldout;
    }

    ElementConfig[] elements;

    // ── State ─────────────────────────────────────────────────────────────────
    GameObject prefabRoot;
    Vector2 scroll;
    string statusMsg = "";
    Color statusColor = Color.white;

    // ── Menu ──────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Mobile UI Layout")]
    static void Open()
    {
        var w = GetWindow<MobileUILayoutTool>("Mobile UI Layout");
        w.minSize = new Vector2(480, 600);
        w.Show();
    }

    void OnEnable() => LoadFromPrefab();

    // ── Khởi tạo giá trị từ prefab ───────────────────────────────────────────

    void InitDefaultElements()
    {
        elements = new[]
        {
            new ElementConfig { label="🕹 Joystick Background", path="MapPanel/JoystickBackground",
                anchor=new Vector2(0,0), position=new Vector2(200,200), size=new Vector2(300,300), canEditAnchor=true },
            new ElementConfig { label="  ↳ Joystick Handle",   path="MapPanel/JoystickBackground/JoystickHandle",
                anchor=new Vector2(.5f,.5f), position=Vector2.zero, size=new Vector2(150,150), canEditAnchor=false },
            new ElementConfig { label="⏸ Pause Button",        path="PauseButton",
                anchor=new Vector2(1,1), position=new Vector2(-80,-70), size=new Vector2(120,100), canEditAnchor=true },
            new ElementConfig { label="🤜 Interact Button",     path="InteractButton",
                anchor=new Vector2(1,0), position=new Vector2(-130,130), size=new Vector2(180,180), canEditAnchor=true },
            new ElementConfig { label="◀ Battle Back",         path="BattleBackButton",
                anchor=new Vector2(0,0), position=new Vector2(130,130), size=new Vector2(160,70), canEditAnchor=true },
            new ElementConfig { label="✓ Battle Confirm",      path="BattleConfirmButton",
                anchor=new Vector2(1,0), position=new Vector2(-130,130), size=new Vector2(160,70), canEditAnchor=true },
            new ElementConfig { label="🛡 Battle Parry",        path="BattleParryButton",
                anchor=new Vector2(1,0), position=new Vector2(-130,260), size=new Vector2(160,70), canEditAnchor=true },
        };
    }

    void LoadFromPrefab()
    {
        InitDefaultElements();

        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (asset == null) { statusMsg = "Không tìm thấy prefab!"; statusColor = Color.red; return; }

        prefabRoot = asset;

        foreach (var el in elements)
        {
            var rt = FindRT(el.path);
            if (rt == null) continue;

            el.anchor   = rt.anchorMin;
            el.position = rt.anchoredPosition;
            el.size     = rt.sizeDelta;
        }

        statusMsg = "Đã đọc từ prefab.";
        statusColor = Color.cyan;
    }

    RectTransform FindRT(string path)
    {
        if (prefabRoot == null) return null;
        var t = path == "" ? prefabRoot.transform : prefabRoot.transform.Find(path);
        return t != null ? t.GetComponent<RectTransform>() : null;
    }

    // ── GUI ───────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        // Header
        EditorGUILayout.LabelField("Mobile UI Layout", EditorStyles.whiteLargeLabel);
        EditorGUILayout.HelpBox(
            "Chỉnh vị trí, kích thước và anchor của từng element trong MobileInputCanvas.\n" +
            "Anchor (0,0)=góc trái dưới  (1,0)=phải dưới  (0,1)=trái trên  (1,1)=phải trên  (0.5,0.5)=giữa\n" +
            "Position = offset từ điểm anchor theo pixel của canvas (1920×1080).",
            MessageType.Info);

        EditorGUILayout.Space(4);

        // Status
        if (!string.IsNullOrEmpty(statusMsg))
        {
            var style = new GUIStyle(EditorStyles.helpBox);
            style.normal.textColor = statusColor;
            EditorGUILayout.LabelField(statusMsg, style);
        }

        // Prefab path
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Prefab:", GUILayout.Width(50));
        EditorGUILayout.LabelField(PREFAB_PATH, EditorStyles.miniLabel);
        if (GUILayout.Button("Reload", GUILayout.Width(70))) LoadFromPrefab();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        if (elements == null) { LoadFromPrefab(); return; }

        // Scroll area
        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < elements.Length; i++)
        {
            var el = elements[i];
            DrawElement(el);
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);

        // Preset buttons
        EditorGUILayout.LabelField("Preset nhanh", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Joystick nhỏ\n(300px)"))   ApplyJoystickPreset(300, 150);
        if (GUILayout.Button("Joystick vừa\n(400px)"))   ApplyJoystickPreset(400, 200);
        if (GUILayout.Button("Joystick lớn\n(500px)"))   ApplyJoystickPreset(500, 250);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // Apply buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("💾 Apply → Prefab", GUILayout.Height(36)))
            ApplyToPrefab();
        if (GUILayout.Button("↩ Reset về mặc định", GUILayout.Height(36)))
        {
            if (EditorUtility.DisplayDialog("Reset", "Reset về giá trị mặc định của tool?", "OK", "Hủy"))
                InitDefaultElements();
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawElement(ElementConfig el)
    {
        var rt = FindRT(el.path);
        bool missing = rt == null;

        var bgColor = missing ? new Color(0.4f, 0.15f, 0.15f) : new Color(0.2f, 0.22f, 0.25f);
        var oldBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = oldBg;

        // Foldout header
        el.foldout = EditorGUILayout.Foldout(el.foldout,
            el.label + (missing ? " ⚠ không tìm thấy" : ""), true, EditorStyles.foldoutHeader);

        if (!el.foldout) { EditorGUILayout.EndVertical(); return; }

        EditorGUI.indentLevel++;

        if (el.canEditAnchor)
        {
            el.anchor = EditorGUILayout.Vector2Field(
                new GUIContent("Anchor", "(x,y): 0=trái/dưới  1=phải/trên  0.5=giữa"),
                el.anchor);
            el.anchor.x = Mathf.Clamp01(el.anchor.x);
            el.anchor.y = Mathf.Clamp01(el.anchor.y);
        }
        else
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector2Field("Anchor (cố định)", el.anchor);
            EditorGUI.EndDisabledGroup();
        }

        el.position = EditorGUILayout.Vector2Field(
            new GUIContent("Position", "Offset pixel từ điểm anchor. Âm=về phía trong màn hình."),
            el.position);

        el.size = EditorGUILayout.Vector2Field(
            new GUIContent("Size (px)", "Kích thước theo pixel của canvas 1920×1080."),
            el.size);

        // Preview current vs new
        if (rt != null)
        {
            bool posChanged  = Vector2.Distance(rt.anchoredPosition, el.position) > 0.1f;
            bool sizeChanged = Vector2.Distance(rt.sizeDelta, el.size) > 0.1f;
            bool ancChanged  = el.canEditAnchor && Vector2.Distance(rt.anchorMin, el.anchor) > 0.01f;

            if (posChanged || sizeChanged || ancChanged)
            {
                EditorGUILayout.HelpBox(
                    $"Thay đổi chưa lưu:\n" +
                    (ancChanged  ? $"  Anchor: {rt.anchorMin:F2} → {el.anchor:F2}\n" : "") +
                    (posChanged  ? $"  Pos: {rt.anchoredPosition:F0} → {el.position:F0}\n" : "") +
                    (sizeChanged ? $"  Size: {rt.sizeDelta:F0} → {el.size:F0}" : ""),
                    MessageType.Warning);
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }

    // ── Preset helpers ────────────────────────────────────────────────────────

    void ApplyJoystickPreset(float bgSize, float handleSize)
    {
        foreach (var el in elements)
        {
            if (el.path.EndsWith("JoystickBackground"))
            {
                el.size = new Vector2(bgSize, bgSize);
                float half = bgSize / 2f + 20;
                el.position = new Vector2(half, half);
                el.anchor   = Vector2.zero;
            }
            else if (el.path.EndsWith("JoystickHandle"))
            {
                el.size = new Vector2(handleSize, handleSize);
            }
        }
    }

    // ── Apply to prefab ───────────────────────────────────────────────────────

    void ApplyToPrefab()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (asset == null) { statusMsg = "Không tìm thấy prefab!"; statusColor = Color.red; return; }

        using var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH);
        var root = scope.prefabContentsRoot;

        int changed = 0;
        foreach (var el in elements)
        {
            Transform t = el.path == "" ? root.transform : root.transform.Find(el.path);
            if (t == null) { Debug.LogWarning($"[MobileUILayout] Không tìm thấy: {el.path}"); continue; }

            var rt = t.GetComponent<RectTransform>();
            if (rt == null) continue;

            if (el.canEditAnchor)
            {
                rt.anchorMin = el.anchor;
                rt.anchorMax = el.anchor;
            }

            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = el.position;
            rt.sizeDelta = el.size;
            changed++;
        }

        statusMsg = $"✓ Đã áp dụng {changed} element vào prefab.";
        statusColor = Color.green;

        LoadFromPrefab(); // refresh display
        Repaint();
    }
}
