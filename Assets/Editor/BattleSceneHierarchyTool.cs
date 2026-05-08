using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor tool: tổ chức lại hierarchy của BattleScene thành các nhóm rõ ràng,
/// và (tuỳ chọn) áp dụng background tạm thời từ Chapter1_Tutorial.
///
/// Nhóm chuẩn:
///   [Systems]    : BattleManager, BattleRunner, BattleSceneBootstrap, các manager runtime
///   [Camera]     : Main Camera + camera khác
///   [UI]         : Canvas chính (BattleUI_Canvas), EventSystem
///   [Background] : Visual roots (TutorialCopy_*), background prefab spawn anchor
///   [Spawning]   : PlayerAnchor, EnemyAnchor, các anchor khác
///   [Effects]    : Particle/FX root (nếu có)
///
/// Menu: Tools/Battle UI/Organize Battle Hierarchy
/// </summary>
public class BattleSceneHierarchyTool : EditorWindow
{
    private const string BATTLE_SCENE = "Assets/Scenes/BattleScene.unity";
    private const string TUTORIAL_SCENE = "Assets/Scenes/Chapter1_Tutorial.unity";

    // Tên các parent group sẽ được tạo ở root.
    private const string GROUP_SYSTEMS = "[Systems]";
    private const string GROUP_CAMERA = "[Camera]";
    private const string GROUP_UI = "[UI]";
    private const string GROUP_BACKGROUND = "[Background]";
    private const string GROUP_SPAWN = "[Spawning]";
    private const string GROUP_EFFECTS = "[Effects]";

    private bool organizeHierarchy = true;
    private bool applyTutorialBackground = false;
    private bool ensureBackgroundController = true;
    private bool ensureBootstrap = true;

    [MenuItem("Tools/Battle UI/Organize Battle Hierarchy")]
    public static void OpenWindow()
    {
        var w = GetWindow<BattleSceneHierarchyTool>("Battle Hierarchy");
        w.minSize = new Vector2(420, 320);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Battle Scene Hierarchy Cleanup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tool này gom các root GameObject của BattleScene vào các nhóm rõ ràng " +
            "([Systems], [UI], [Background], ...). Có thể tuỳ chọn áp dụng visual " +
            "background từ Chapter1_Tutorial.\n\n" +
            "Mở BattleScene trước khi chạy.",
            MessageType.Info);

        EditorGUILayout.Space(8);
        organizeHierarchy = EditorGUILayout.Toggle("Tổ chức lại hierarchy", organizeHierarchy);
        applyTutorialBackground = EditorGUILayout.Toggle("Copy visual roots từ Chapter1_Tutorial", applyTutorialBackground);
        ensureBackgroundController = EditorGUILayout.Toggle("Tạo BattleBackgroundController nếu thiếu", ensureBackgroundController);
        ensureBootstrap = EditorGUILayout.Toggle("Tạo BattleSceneBootstrap nếu thiếu", ensureBootstrap);

        EditorGUILayout.Space(12);
        if (GUILayout.Button("Chạy", GUILayout.Height(36)))
            Run();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Sau khi chạy:\n" +
            "- Kiểm tra group [Background] có chứa visual roots TutorialCopy_*.\n" +
            "- Nếu HUD của bạn vẫn hiển thị tên cố định, BattleUI sẽ tự reset về rỗng " +
            "khi runtime; tên/HP runtime sẽ ghi đè khi BattleManager init xong.\n" +
            "- BattleBackgroundController sẽ ưu tiên Mapdata.battleBackgroundPrefab " +
            "(nếu được set) hoặc fallback về defaultBackgroundPrefab.",
            MessageType.None);
    }

    private void Run()
    {
        Scene battleScene = EnsureBattleSceneOpen();
        if (!battleScene.IsValid()) return;

        if (applyTutorialBackground)
            CopyTutorialVisualRoots(battleScene);

        if (ensureBootstrap)
            EnsureBattleSceneBootstrap(battleScene);

        if (ensureBackgroundController)
            EnsureBackgroundController(battleScene);

        if (organizeHierarchy)
            OrganizeRoots(battleScene);

        EditorSceneManager.MarkSceneDirty(battleScene);
        EditorSceneManager.SaveScene(battleScene);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Hoàn tất",
            "BattleScene hierarchy đã được sắp xếp lại.\n" +
            "Mở Hierarchy panel để kiểm tra các group [Systems], [UI], [Background], v.v.",
            "OK");
    }

    private static Scene EnsureBattleSceneOpen()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path == BATTLE_SCENE) return active;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return default;
        return EditorSceneManager.OpenScene(BATTLE_SCENE, OpenSceneMode.Single);
    }

    // ─── HIERARCHY ORGANIZATION ──────────────────────────────────────────

    private static void OrganizeRoots(Scene scene)
    {
        var systems = EnsureGroup(scene, GROUP_SYSTEMS);
        var cameraGroup = EnsureGroup(scene, GROUP_CAMERA);
        var ui = EnsureGroup(scene, GROUP_UI);
        var background = EnsureGroup(scene, GROUP_BACKGROUND);
        var spawn = EnsureGroup(scene, GROUP_SPAWN);
        var effects = EnsureGroup(scene, GROUP_EFFECTS);

        // Snapshot roots vì sẽ thay đổi parent (root) trong loop.
        var roots = scene.GetRootGameObjects().ToList();

        foreach (var go in roots)
        {
            if (go == null) continue;
            if (IsGroupRoot(go)) continue; // Bỏ qua chính các group container.

            GameObject target = ClassifyRoot(go, systems, cameraGroup, ui, background, spawn, effects);
            if (target == null || target == go) continue;

            Undo.SetTransformParent(go.transform, target.transform, "Organize Battle Hierarchy");
        }

        // Xoá các group rỗng để Hierarchy không bị nhiễu.
        RemoveEmptyGroups(scene);
    }

    private static GameObject ClassifyRoot(
        GameObject go,
        GameObject systems,
        GameObject cameraGroup,
        GameObject ui,
        GameObject background,
        GameObject spawn,
        GameObject effects)
    {
        // Camera
        if (go.GetComponent<Camera>() != null) return cameraGroup;

        // UI / EventSystem / Canvas
        if (go.GetComponent<Canvas>() != null) return ui;
        if (go.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) return ui;

        // Systems (manager / runtime)
        if (HasAnyComponent(go,
            typeof(BattleManager),
            typeof(BattleRunner),
            typeof(BattleSceneBootstrap),
            typeof(MapManager),
            typeof(GameManager),
            typeof(InputController),
            typeof(QuestManager)))
        {
            return systems;
        }

        // Background controller hoặc các root đã copy từ Tutorial
        if (go.GetComponent<BattleBackgroundController>() != null) return background;
        if (go.name.StartsWith("TutorialCopy_")) return background;
        if (go.name.ToLower().Contains("background") || go.name.ToLower().Contains("battlefield"))
            return background;

        // Spawn anchors (heuristic theo tên hoặc empty transform có tên anchor)
        string lname = go.name.ToLower();
        if (lname.Contains("anchor") || lname.Contains("spawn"))
            return spawn;

        // Effects: ParticleSystem hoặc FX
        if (go.GetComponentInChildren<ParticleSystem>(true) != null)
            return effects;
        if (lname.Contains("fx") || lname.Contains("vfx") || lname.Contains("effect"))
            return effects;

        // Mặc định: nếu có Renderer / Animator → coi như background asset.
        if (go.GetComponentInChildren<Renderer>(true) != null ||
            go.GetComponentInChildren<Animator>(true) != null)
        {
            return background;
        }

        // Fallback: Systems (an toàn — không bị mất tham chiếu)
        return systems;
    }

    private static bool HasAnyComponent(GameObject go, params System.Type[] types)
    {
        foreach (var t in types)
        {
            if (t == null) continue;
            if (go.GetComponent(t) != null) return true;
        }
        return false;
    }

    private static GameObject EnsureGroup(Scene scene, string groupName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root != null && root.name == groupName) return root;
        }

        var go = new GameObject(groupName);
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create Hierarchy Group");
        return go;
    }

    private static bool IsGroupRoot(GameObject go)
    {
        return go.name == GROUP_SYSTEMS || go.name == GROUP_CAMERA ||
               go.name == GROUP_UI || go.name == GROUP_BACKGROUND ||
               go.name == GROUP_SPAWN || go.name == GROUP_EFFECTS;
    }

    private static void RemoveEmptyGroups(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects().ToList())
        {
            if (root == null) continue;
            if (!IsGroupRoot(root)) continue;
            if (root.transform.childCount == 0)
            {
                Undo.DestroyObjectImmediate(root);
            }
        }
    }

    // ─── TUTORIAL BACKGROUND COPY ────────────────────────────────────────

    private static void CopyTutorialVisualRoots(Scene battleScene)
    {
        Scene tutorial = EditorSceneManager.OpenScene(TUTORIAL_SCENE, OpenSceneMode.Additive);
        if (!tutorial.IsValid())
        {
            Debug.LogError($"[BattleHierarchy] Không mở được {TUTORIAL_SCENE}");
            return;
        }

        try
        {
            // Xoá các bản copy cũ để tránh trùng lặp.
            foreach (var root in battleScene.GetRootGameObjects().ToList())
            {
                if (root != null && root.name.StartsWith("TutorialCopy_"))
                    Undo.DestroyObjectImmediate(root);
            }

            // Tìm parent group [Background] trước để tránh khi xoá empty group sau
            // mất luôn các bản copy.
            foreach (var root in tutorial.GetRootGameObjects())
            {
                if (root == null) continue;
                if (!ShouldCopyAsBackground(root)) continue;

                var copy = Object.Instantiate(root);
                copy.name = $"TutorialCopy_{root.name}";
                SceneManager.MoveGameObjectToScene(copy, battleScene);
                copy.transform.SetParent(null);
                Undo.RegisterCreatedObjectUndo(copy, "Copy Tutorial Background");
                Debug.Log($"[BattleHierarchy] Copied background root: {copy.name}");
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(tutorial, true);
        }
    }

    private static bool ShouldCopyAsBackground(GameObject root)
    {
        if (root.GetComponent<Canvas>() != null) return false;
        if (root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) return false;
        if (root.GetComponent<Camera>() != null) return false;

        // Bỏ qua các manager system của tutorial.
        if (HasAnyComponent(root,
            typeof(BattleManager),
            typeof(MapManager),
            typeof(GameManager),
            typeof(InputController),
            typeof(QuestManager)))
        {
            return false;
        }

        bool hasVisual =
            root.GetComponentInChildren<Renderer>(true) != null ||
            root.GetComponentInChildren<ParticleSystem>(true) != null ||
            root.GetComponentInChildren<Animator>(true) != null;

        if (hasVisual) return true;

        string n = root.name.ToLower();
        return n.Contains("battlefield") || n.Contains("background") ||
               n.Contains("environment") || n.Contains("scenery");
    }

    // ─── BACKGROUND CONTROLLER + BOOTSTRAP ───────────────────────────────

    private static void EnsureBackgroundController(Scene scene)
    {
        if (Object.FindFirstObjectByType<BattleBackgroundController>() != null) return;

        var go = new GameObject("BattleBackgroundController");
        go.AddComponent<BattleBackgroundController>();
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create BattleBackgroundController");
        Debug.Log("[BattleHierarchy] Tạo BattleBackgroundController.");
    }

    private static void EnsureBattleSceneBootstrap(Scene scene)
    {
        if (Object.FindFirstObjectByType<BattleSceneBootstrap>() != null) return;

        var go = new GameObject("BattleSceneBootstrap");
        go.AddComponent<BattleSceneBootstrap>();
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create BattleSceneBootstrap");
        Debug.Log("[BattleHierarchy] Tạo BattleSceneBootstrap.");
    }
}
