using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Tool: sửa CanvasScaler + EventSystem InputModule cho tất cả scenes.
/// Mở qua menu: Tools → Mobile UI Fixer
/// </summary>
public class MobileUIFixerTool : EditorWindow
{
    // ── Settings ──────────────────────────────────────────────────────────────

    private Vector2Int refRes          = new Vector2Int(1280, 720);
    private float      matchValue      = 0.5f;
    private bool       fixCanvasScaler = true;
    private bool       fixEventSystem  = true;

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly List<LogLine> log = new();
    private Vector2 scroll;

    struct LogLine
    {
        public string text;
        public Color  color;
    }

    // ── Menu entry ────────────────────────────────────────────────────────────

    [MenuItem("Tools/Mobile UI Fixer")]
    static void Open()
    {
        var w = GetWindow<MobileUIFixerTool>("Mobile UI Fixer");
        w.minSize = new Vector2(460, 600);
        w.Show();
    }

    // ── GUI ───────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        EditorGUILayout.LabelField("Mobile UI Fixer", EditorStyles.whiteLargeLabel);
        EditorGUILayout.HelpBox(
            "Tự động sửa CanvasScaler (ScaleWithScreenSize) và EventSystem InputModule " +
            "(StandaloneInputModule → InputSystemUIInputModule) cho các scenes được chọn.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        // ── Canvas Scaler ─────────────────────────────────────────────────────
        EditorGUILayout.LabelField("CanvasScaler", EditorStyles.boldLabel);
        fixCanvasScaler = EditorGUILayout.ToggleLeft("Sửa CanvasScaler", fixCanvasScaler);

        using (new EditorGUI.DisabledScope(!fixCanvasScaler))
        using (new EditorGUI.IndentLevelScope())
        {
            refRes = EditorGUILayout.Vector2IntField("Reference Resolution", refRes);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Match Width / Height", GUILayout.Width(170));
            matchValue = EditorGUILayout.Slider(matchValue, 0f, 1f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Presets:", GUILayout.Width(60));
            if (GUILayout.Button("Portrait 1080×1920", EditorStyles.miniButton))
                { refRes = new Vector2Int(1080, 1920); matchValue = 0.5f; }
            if (GUILayout.Button("Landscape 1280×720", EditorStyles.miniButton))
                { refRes = new Vector2Int(1280, 720); matchValue = 0.5f; }
            if (GUILayout.Button("Landscape 1920×1080", EditorStyles.miniButton))
                { refRes = new Vector2Int(1920, 1080); matchValue = 0.5f; }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(6);

        // ── EventSystem ───────────────────────────────────────────────────────
        EditorGUILayout.LabelField("EventSystem", EditorStyles.boldLabel);
        fixEventSystem = EditorGUILayout.ToggleLeft(
            "Thay StandaloneInputModule → InputSystemUIInputModule", fixEventSystem);

        EditorGUILayout.Space(12);

        // ── Action buttons ────────────────────────────────────────────────────
        if (GUILayout.Button("Scan tất cả scenes (Preview, không lưu)", GUILayout.Height(28)))
            RunAllScenes(dryRun: true);

        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Áp dụng scene hiện tại", GUILayout.Height(34)))
                RunCurrentScene();

            if (GUILayout.Button("Áp dụng TẤT CẢ scenes", GUILayout.Height(34)))
            {
                if (EditorUtility.DisplayDialog("Xác nhận",
                    "Áp dụng cho TẤT CẢ scenes trong Build Settings?\n" +
                    "Mỗi scene sẽ được mở, sửa và lưu tự động.",
                    "Áp dụng", "Huỷ"))
                    RunAllScenes(dryRun: false);
            }
        }

        // ── Log ───────────────────────────────────────────────────────────────
        if (log.Count > 0)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Kết quả:", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            foreach (var line in log)
            {
                var old = GUI.color;
                GUI.color = line.color;
                EditorGUILayout.LabelField(line.text, EditorStyles.wordWrappedLabel);
                GUI.color = old;
            }
            EditorGUILayout.EndScrollView();
        }
    }

    // ── Helpers: logging ──────────────────────────────────────────────────────

    void Log(string msg, Color c) => log.Add(new LogLine { text = msg, color = c });

    void LogOK(string msg)   => Log("✓ " + msg, new Color(0.4f, 1f, 0.4f));
    void LogFix(string msg)  => Log("→ " + msg, Color.yellow);
    void LogInfo(string msg) => Log("  " + msg, Color.white);
    void LogErr(string msg)  => Log("✗ " + msg, new Color(1f, 0.4f, 0.4f));
    void LogHead(string msg) => Log(msg, new Color(0.8f, 0.8f, 1f));

    // ── Core: fix current scene ───────────────────────────────────────────────

    void RunCurrentScene()
    {
        log.Clear();
        LogHead("=== Scene hiện tại ===");

        var scene = EditorSceneManager.GetActiveScene();
        bool dirty = ProcessOpenScene();

        if (dirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            LogOK("Scene đã được đánh dấu dirty — nhớ Ctrl+S để lưu.");
        }
        else
        {
            LogInfo("Không có gì cần sửa.");
        }

        Repaint();
    }

    // ── Core: fix all scenes ──────────────────────────────────────────────────

    void RunAllScenes(bool dryRun)
    {
        log.Clear();
        LogHead(dryRun ? "=== PREVIEW (chỉ quét, không lưu) ===" : "=== ÁP DỤNG TẤT CẢ SCENES ===");

        if (!dryRun)
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        string originalPath = EditorSceneManager.GetActiveScene().path;
        int totalFixed = 0;

        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled) continue;
            string path = buildScene.path;
            if (string.IsNullOrEmpty(path)) continue;

            LogHead($"\n─── {System.IO.Path.GetFileName(path)} ───");

            var scene = EditorSceneManager.OpenScene(path,
                dryRun ? OpenSceneMode.Additive : OpenSceneMode.Single);

            bool dirty = ProcessOpenScene(dryRun, out int fixCount);
            totalFixed += fixCount;

            if (!dryRun && dirty)
            {
                EditorSceneManager.SaveScene(scene, path);
                LogOK("Đã lưu.");
            }

            if (dryRun)
                EditorSceneManager.CloseScene(scene, true);
        }

        if (!dryRun && !string.IsNullOrEmpty(originalPath))
            EditorSceneManager.OpenScene(originalPath);

        LogHead($"\n=== XONG: {totalFixed} thay đổi ===");
        Repaint();
    }

    // ── Core: process currently-open scene ────────────────────────────────────

    bool ProcessOpenScene(bool dryRun, out int fixCount)
    {
        fixCount = 0;
        bool anyDirty = false;

        if (fixCanvasScaler)
            anyDirty |= FixCanvasScalers(dryRun, ref fixCount);

        if (fixEventSystem)
            anyDirty |= FixEventSystems(dryRun, ref fixCount);

        return anyDirty;
    }

    bool ProcessOpenScene() { return ProcessOpenScene(false, out _); }

    // ── CanvasScaler ──────────────────────────────────────────────────────────

    bool FixCanvasScalers(bool dryRun, ref int fixCount)
    {
        bool any = false;
        var target = new Vector2(refRes.x, refRes.y);

        foreach (var scaler in FindAllInScene<CanvasScaler>())
        {
            var changes = new List<string>();

            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                changes.Add($"ScaleMode: {scaler.uiScaleMode} → ScaleWithScreenSize");

            if (scaler.referenceResolution != target)
                changes.Add($"Ref: {(int)scaler.referenceResolution.x}×{(int)scaler.referenceResolution.y} → {refRes.x}×{refRes.y}");

            if (Mathf.Abs(scaler.matchWidthOrHeight - matchValue) > 0.01f)
                changes.Add($"Match: {scaler.matchWidthOrHeight:F2} → {matchValue:F2}");

            string go = scaler.gameObject.name;
            if (changes.Count == 0)
            {
                LogOK($"CanvasScaler [{go}] — đã đúng");
                continue;
            }

            LogFix($"CanvasScaler [{go}]: {string.Join(", ", changes)}");

            if (!dryRun)
            {
                scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = target;
                scaler.matchWidthOrHeight  = matchValue;
                EditorUtility.SetDirty(scaler);
                any = true;
            }

            fixCount += changes.Count;
        }

        return any;
    }

    // ── EventSystem ───────────────────────────────────────────────────────────

    bool FixEventSystems(bool dryRun, ref int fixCount)
    {
        bool any = false;

        foreach (var es in FindAllInScene<EventSystem>())
        {
            var standalone = es.GetComponent<StandaloneInputModule>();

            if (standalone == null)
            {
#if ENABLE_INPUT_SYSTEM
                bool hasNew = es.GetComponent<InputSystemUIInputModule>() != null;
                if (hasNew)
                    LogOK($"EventSystem [{es.gameObject.name}] — InputSystemUIInputModule ✓");
                else
                    LogErr($"EventSystem [{es.gameObject.name}] — không có input module nào!");
#else
                LogInfo($"EventSystem [{es.gameObject.name}] — (ENABLE_INPUT_SYSTEM chưa kích hoạt)");
#endif
                continue;
            }

            LogFix($"EventSystem [{es.gameObject.name}]: StandaloneInputModule → InputSystemUIInputModule");

            if (!dryRun)
            {
#if ENABLE_INPUT_SYSTEM
                Object.DestroyImmediate(standalone);
                es.gameObject.AddComponent<InputSystemUIInputModule>();
                EditorUtility.SetDirty(es.gameObject);
                any = true;
                fixCount++;
                LogOK($"  InputSystemUIInputModule đã thêm.");
#else
                LogErr("  Không thể thêm InputSystemUIInputModule — cần cài package com.unity.inputsystem.");
#endif
            }
            else
            {
                fixCount++;
            }
        }

        return any;
    }

    // ── Utility: find components chỉ trong scene hiện tại ─────────────────────

    static List<T> FindAllInScene<T>() where T : Component
    {
        var result = new List<T>();
        var activeScene = EditorSceneManager.GetActiveScene();

        foreach (var root in activeScene.GetRootGameObjects())
        {
            result.AddRange(root.GetComponentsInChildren<T>(includeInactive: true));
        }

        return result;
    }
}
