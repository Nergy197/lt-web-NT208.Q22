using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tool copy toàn bộ UI Canvas từ Chapter1_Tutorial → BattleScene,
/// sau đó tự động wire các UI elements vào BattleUI component.
///
/// Menu: Tools/Battle UI/Copy Tutorial UI to Battle Scene
/// </summary>
public class CopyTutorialUITool : EditorWindow
{
    // ─── Paths ───────────────────────────────────────────────────────────────
    private const string TUTORIAL_SCENE = "Assets/Scenes/Chapter1_Tutorial.unity";
    private const string BATTLE_SCENE   = "Assets/Scenes/BattleScene.unity";

    // ─── Heuristic: tên GameObject cần tìm trong Tutorial Canvas ─────────────
    // Tên các root UI containers ta cần copy (từ kết quả scan scene)
    private static readonly string[] UI_ROOT_NAMES = {
        "BattleCanvas",   // thử tên canvas chính
        "Canvas",         // fallback
        "UI",
        "HUD",
        "BattleUI",
    };

    // ─── GUI state ───────────────────────────────────────────────────────────
    private bool deleteLegacyCanvas = true;
    private bool removePreviousTutorialCopies = true;
    private bool copyEnvironmentPrefabs = true;
    private bool wireAutomatically  = true;
    private bool expandHUDSlots = true;

    [MenuItem("Tools/Battle UI/Copy Tutorial UI to Battle Scene")]
    public static void OpenWindow()
    {
        var w = GetWindow<CopyTutorialUITool>("Copy Tutorial UI");
        w.minSize = new Vector2(400, 280);
        w.Show();
    }

    [MenuItem("Tools/Battle UI/Scan Tutorial UI Structure (Log)")]
    public static void ScanTutorial()
    {
        var additive = EditorSceneManager.OpenScene(TUTORIAL_SCENE, OpenSceneMode.Additive);
        Debug.Log("=== TUTORIAL UI SCAN ===");
        foreach (var go in additive.GetRootGameObjects())
        {
            var canvas = go.GetComponent<Canvas>();
            Debug.Log($"[ROOT] {go.name} " + (canvas != null ? "[CANVAS]" : ""));
            ScanChildren(go.transform, 1);
        }
        Debug.Log("=== END SCAN ===");
        EditorSceneManager.CloseScene(additive, true);
    }

    private static void ScanChildren(Transform t, int depth)
    {
        if (depth > 4) return;
        string indent = new string(' ', depth * 2);
        foreach (Transform child in t)
        {
            var comps = child.GetComponents<Component>();
            string compNames = string.Join(", ", comps
                .Where(c => c != null && !(c is Transform))
                .Select(c => c.GetType().Name));
            Debug.Log($"{indent}└ {child.name}  [{compNames}]");
            ScanChildren(child, depth + 1);
        }
    }

    // ─── GUI ─────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Copy Tutorial UI → Battle Scene", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tool này sẽ:\n" +
            "1. Mở Chapter1_Tutorial.unity (additive)\n" +
            "2. Tìm Canvas UI chính trong Tutorial\n" +
            "3. Copy toàn bộ Canvas sang BattleScene\n" +
            "4. Copy các root visual/prefab của Tutorial sang BattleScene (background, character models...)\n" +
            "5. Tự động wire các element vào BattleUI (ưu tiên reuse)\n" +
            "6. Mở rộng HUD slots cho 2 player / tối đa 3 enemy nếu đủ template\n" +
            "7. Lưu BattleScene",
            MessageType.Info);

        EditorGUILayout.Space(8);
        deleteLegacyCanvas = EditorGUILayout.Toggle("Xóa Canvas cũ (disabled) trong BattleScene", deleteLegacyCanvas);
        removePreviousTutorialCopies = EditorGUILayout.Toggle("Xóa bản copy cũ từ Tutorial", removePreviousTutorialCopies);
        copyEnvironmentPrefabs = EditorGUILayout.Toggle("Copy background/prefabs visual từ Tutorial", copyEnvironmentPrefabs);
        wireAutomatically  = EditorGUILayout.Toggle("Tự động wire BattleUI", wireAutomatically);
        expandHUDSlots = EditorGUILayout.Toggle("Mở rộng HUD slots (2 player / 3 enemy)", expandHUDSlots);

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "⚠️ Chạy tool này khi BattleScene đang là Active Scene.\n" +
            "Sau khi chạy, kiểm tra Inspector của BattleUI để wire các slot còn thiếu.",
            MessageType.Warning);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("▶  Chạy Copy", GUILayout.Height(36)))
            Run();

        EditorGUILayout.Space(4);
        if (GUILayout.Button("🔍  Scan Tutorial UI Structure (Log Only)"))
            ScanTutorial();
    }

    // ─── Main ────────────────────────────────────────────────────────────────
    private void Run()
    {
        // Lưu scene hiện tại
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1. Đảm bảo BattleScene là active scene
        Scene battleScene;
        if (SceneManager.GetActiveScene().path == BATTLE_SCENE)
        {
            battleScene = SceneManager.GetActiveScene();
        }
        else
        {
            battleScene = EditorSceneManager.OpenScene(BATTLE_SCENE, OpenSceneMode.Single);
        }

        // 2. Mở Tutorial scene additive để lấy UI
        Scene tutorialScene = EditorSceneManager.OpenScene(TUTORIAL_SCENE, OpenSceneMode.Additive);

        // 3. Tìm Canvas chính trong Tutorial
        GameObject tutorialCanvas = FindMainCanvas(tutorialScene);
        if (tutorialCanvas == null)
        {
            EditorSceneManager.CloseScene(tutorialScene, true);
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas UI trong Chapter1_Tutorial.unity!\nDùng Scan tool để kiểm tra tên.", "OK");
            return;
        }

        Debug.Log($"[CopyUI] Tìm thấy Tutorial Canvas: '{tutorialCanvas.name}'");

        // 4. Xóa Canvas cũ (disabled) trong BattleScene nếu cần
        if (deleteLegacyCanvas)
        {
            RemoveDisabledCanvases(battleScene);
        }

        // 4b. Xóa bản copy cũ từ Tutorial (nếu chọn)
        if (removePreviousTutorialCopies)
            RemoveTutorialCopies(battleScene);

        // 5. Duplicate Canvas sang BattleScene
        GameObject copiedCanvas = DuplicateToScene(tutorialCanvas, battleScene);

        // 6. Bật Canvas nếu bị tắt
        copiedCanvas.SetActive(true);
        var canvas = copiedCanvas.GetComponent<Canvas>();
        if (canvas != null) canvas.enabled = true;

        // Đặt tên rõ ràng
        copiedCanvas.name = "BattleUI_Canvas";

        // 6b. Copy root visual/prefab từ tutorial (không copy systems)
        if (copyEnvironmentPrefabs)
            CopyTutorialVisualRoots(tutorialScene, battleScene, tutorialCanvas);

        // Đóng Tutorial scene
        EditorSceneManager.CloseScene(tutorialScene, true);

        EnsureEventSystemInScene(battleScene);

        // 7. Wire BattleUI
        if (wireAutomatically)
        {
            WireBattleUI(copiedCanvas, battleScene);
        }

        // 8. Mở rộng HUD slots theo battle requirements
        if (expandHUDSlots && wireAutomatically)
            EnsureBattleHUDSlots(copiedCanvas, 2, 3);

        // 9. Lưu
        EditorSceneManager.MarkSceneDirty(battleScene);
        EditorSceneManager.SaveScene(battleScene);
        AssetDatabase.Refresh();

        Debug.Log("[CopyUI] ✅ Hoàn tất! UI + Tutorial visual roots đã được copy sang BattleScene.");
        EditorUtility.DisplayDialog("Hoàn Tất",
            "UI và prefab/background visual từ Tutorial đã được copy vào BattleScene!\n\n" +
            "Kiểm tra Inspector của BattleUI để wire các slot còn thiếu:\n" +
            "• playerHUDs, enemyHUDs\n" +
            "• actionMenuPanel, skillMenuPanel\n" +
            "• resultPanel",
            "OK");
    }

    // ─── Find Canvas ─────────────────────────────────────────────────────────

    private GameObject FindMainCanvas(Scene scene)
    {
        // Ưu tiên: tìm Canvas có nhiều children nhất (Canvas UI chính của game)
        Canvas bestCanvas = null;
        int bestChildCount = -1;

        foreach (var root in scene.GetRootGameObjects())
        {
            // Tìm tất cả Canvas trong root và con
            var canvases = root.GetComponentsInChildren<Canvas>(includeInactive: true);
            foreach (var c in canvases)
            {
                // Chỉ lấy Canvas ở root level (không phải nested canvas)
                if (c.transform.parent != null) continue;

                int childCount = c.transform.childCount;
                if (childCount > bestChildCount)
                {
                    bestChildCount = childCount;
                    bestCanvas = c;
                }
            }

            // Nếu root chính là Canvas
            var rootCanvas = root.GetComponent<Canvas>();
            if (rootCanvas != null && root.transform.childCount > bestChildCount)
            {
                bestChildCount = root.transform.childCount;
                bestCanvas = rootCanvas;
            }
        }

        return bestCanvas?.gameObject;
    }

    private void RemoveDisabledCanvases(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas != null && (!root.activeSelf || !canvas.enabled))
            {
                Debug.Log($"[CopyUI] Xóa Canvas cũ bị disabled: '{root.name}'");
                Undo.DestroyObjectImmediate(root);
            }
        }
    }

    private void RemoveTutorialCopies(Scene scene)
    {
        var roots = scene.GetRootGameObjects().ToList();
        foreach (var root in roots)
        {
            if (root.name.StartsWith("TutorialCopy_"))
            {
                Debug.Log($"[CopyUI] Xóa bản copy cũ: '{root.name}'");
                Undo.DestroyObjectImmediate(root);
            }
        }
    }

    // ─── Duplicate GameObject sang scene khác ────────────────────────────────

    private GameObject DuplicateToScene(GameObject source, Scene targetScene)
    {
        // Tạo bản sao
        GameObject copy = Instantiate(source);
        copy.name = source.name;

        // Chuyển sang target scene
        SceneManager.MoveGameObjectToScene(copy, targetScene);

        // Reset transform (Canvas không cần)
        copy.transform.SetParent(null);

        Undo.RegisterCreatedObjectUndo(copy, "Copy Tutorial UI");
        return copy;
    }

    private void CopyTutorialVisualRoots(Scene tutorialScene, Scene battleScene, GameObject tutorialCanvas)
    {
        foreach (var root in tutorialScene.GetRootGameObjects())
        {
            if (root == null) continue;
            if (root == tutorialCanvas) continue;
            if (!ShouldCopyRoot(root)) continue;

            var copy = DuplicateToScene(root, battleScene);
            copy.name = $"TutorialCopy_{root.name}";
            copy.SetActive(root.activeSelf);
            Debug.Log($"[CopyUI] Copied visual root: {root.name} -> {copy.name}");
        }
    }

    private bool ShouldCopyRoot(GameObject root)
    {
        if (root.GetComponent<Canvas>() != null) return false;
        if (root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) return false;
        if (root.GetComponent<SimpleTutorialManager>() != null) return false;
        if (root.GetComponent<BattleManager>() != null) return false;
        if (root.GetComponent<BattleUI>() != null) return false;
        if (root.GetComponent<MapManager>() != null) return false;
        if (root.GetComponent<GameManager>() != null) return false;

        // Copy root khi có thành phần visual / prefab instance.
        bool hasVisual =
            root.GetComponentInChildren<Renderer>(true) != null ||
            root.GetComponentInChildren<ParticleSystem>(true) != null ||
            root.GetComponentInChildren<Animator>(true) != null;

        if (hasVisual) return true;

        // Backup heuristic theo tên (đề phòng object visual chưa bật renderer ngay).
        string n = root.name.ToLower();
        return n.Contains("battlefield") || n.Contains("background") || n.Contains("player") ||
               n.Contains("enemy") || n.Contains("linh") || n.Contains("tuan");
    }

    private void EnsureEventSystemInScene(Scene battleScene)
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        SceneManager.MoveGameObjectToScene(es, battleScene);
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    // ─── Wire BattleUI ───────────────────────────────────────────────────────

    private void WireBattleUI(GameObject canvasGO, Scene battleScene)
    {
        // Tìm hoặc tạo BattleUI component
        BattleUI battleUI = canvasGO.GetComponentInChildren<BattleUI>(includeInactive: true);
        if (battleUI == null)
        {
            // Thêm BattleUI vào root Canvas
            battleUI = canvasGO.AddComponent<BattleUI>();
            Debug.Log("[CopyUI] Đã thêm BattleUI component vào Canvas.");
        }

        var so = new SerializedObject(battleUI);

        // ── Action Menu Panel ────────────────────────────────────────────────
        TryWirePanel(so, "actionMenuPanel", canvasGO,
            "Action_Frame", "ActionMenu", "Main_Frame", "MainMenu", "Battle_Menu", "ActionFrame");

        // ── Skill Menu Panel ─────────────────────────────────────────────────
        TryWirePanel(so, "skillMenuPanel", canvasGO,
            "Skill_Frame", "SkillMenu", "Skill_Menu", "SkillFrame", "SkillPanel");

        // ── Result Panel ─────────────────────────────────────────────────────
        TryWirePanel(so, "resultPanel", canvasGO,
            "Result", "ResultPanel", "Win_Panel", "Lose_Panel", "BattleResult");

        // ── Buttons ──────────────────────────────────────────────────────────
        TryWireButton(so, "btnAttack", canvasGO, "Btn_Attack", "Attack", "BasicAttack");
        TryWireButton(so, "btnSkill",  canvasGO, "Btn_Skills", "Btn_Skill", "Skill", "SkillBtn", "Btn_Skill1");
        TryWireButton(so, "btnFlee",   canvasGO, "Btn_Flee",   "Flee",   "BtnFlee",  "Btn_Items", "Btn_Run");
        TryWireButton(so, "btnParry",  canvasGO, "Btn_Parry",  "Parry",  "BtnParry", "Btn_Energy", "Btn_Heal");
        TryWireButton(so, "btnSkillBack", canvasGO, "Btn_Back", "Back", "BtnBack", "Btn_Cancel", "Btn_Cleanse");

        // ── Skill Buttons (list) ─────────────────────────────────────────────
        TryWireSkillButtonsList(so, canvasGO);

        // ── Player/Enemy HUD ─────────────────────────────────────────────────
        TryWireUnitHUDs(so, "playerHUDs", canvasGO,
            "Player_HUD", "PlayerHUD", "HUD_Background", "Tuan_HUD", "HeroHUD");
        TryWireUnitHUDs(so, "enemyHUDs", canvasGO,
            "Enemy_HUD", "EnemyHUD", "Enemy_Background", "Linh_HUD", "MonsterHUD");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(battleUI);

        Debug.Log("[CopyUI] BattleUI wire hoàn tất. Kiểm tra Inspector để điền các slot còn thiếu.");
    }

    // ─── Wire Helpers ────────────────────────────────────────────────────────

    private void TryWirePanel(SerializedObject so, string propName, GameObject root, params string[] candidates)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || prop.objectReferenceValue != null) return;

        foreach (var name in candidates)
        {
            var found = FindDeepByName(root.transform, name);
            if (found != null)
            {
                prop.objectReferenceValue = found;
                Debug.Log($"[CopyUI] Wired {propName} → {found.name}");
                return;
            }
        }
        Debug.LogWarning($"[CopyUI] ⚠️ Không tìm thấy panel cho: {propName} (tried: {string.Join(", ", candidates)})");
    }

    private void TryWireButton(SerializedObject so, string propName, GameObject root, params string[] candidates)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || prop.objectReferenceValue != null) return;

        foreach (var name in candidates)
        {
            var found = FindDeepByName(root.transform, name);
            if (found != null)
            {
                var btn = found.GetComponent<Button>();
                if (btn != null)
                {
                    prop.objectReferenceValue = btn;
                    Debug.Log($"[CopyUI] Wired {propName} → {found.name}");
                    return;
                }
            }
        }
        Debug.LogWarning($"[CopyUI] ⚠️ Không tìm thấy button cho: {propName}");
    }

    private void TryWireSkillButtonsList(SerializedObject so, GameObject root)
    {
        var listProp = so.FindProperty("skillButtons");
        if (listProp == null || listProp.arraySize > 0) return;

        // Tìm tất cả button có tên dạng Btn_Skill1, Btn_Skill2, Btn_Skill3
        var skillBtns = new List<Button>();
        for (int i = 1; i <= 3; i++)
        {
            foreach (var name in new[] { $"Btn_Skill{i}", $"Skill{i}", $"SkillBtn{i}", $"Btn_Skill_{i}" })
            {
                var found = FindDeepByName(root.transform, name);
                if (found != null)
                {
                    var btn = found.GetComponent<Button>();
                    if (btn != null) { skillBtns.Add(btn); break; }
                }
            }
        }

        if (skillBtns.Count > 0)
        {
            listProp.arraySize = skillBtns.Count;
            for (int i = 0; i < skillBtns.Count; i++)
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = skillBtns[i];
            Debug.Log($"[CopyUI] Wired skillButtons: {skillBtns.Count} buttons");
        }
    }

    private void TryWireUnitHUDs(SerializedObject so, string propName, GameObject root, params string[] candidates)
    {
        var listProp = so.FindProperty(propName);
        if (listProp == null || listProp.arraySize > 0) return;

        var huds = new List<UnitHUD>();
        foreach (var name in candidates)
        {
            var found = FindDeepByName(root.transform, name);
            if (found != null)
            {
                var hud = found.GetComponent<UnitHUD>();
                if (hud == null) hud = found.AddComponent<UnitHUD>();
                huds.Add(hud);
            }
        }

        if (huds.Count > 0)
        {
            listProp.arraySize = huds.Count;
            for (int i = 0; i < huds.Count; i++)
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = huds[i];
            Debug.Log($"[CopyUI] Wired {propName}: {huds.Count} HUDs");
        }
        else
        {
            Debug.LogWarning($"[CopyUI] ⚠️ Không tìm thấy UnitHUD cho: {propName}");
        }
    }

    private void EnsureBattleHUDSlots(GameObject canvasGO, int requiredPlayerSlots, int requiredEnemySlots)
    {
        var battleUI = canvasGO.GetComponentInChildren<BattleUI>(true);
        if (battleUI == null) return;

        var so = new SerializedObject(battleUI);

        var allHuds = canvasGO.GetComponentsInChildren<UnitHUD>(true).ToList();
        var player = allHuds.Where(IsPlayerHUD).ToList();
        var enemy = allHuds.Where(IsEnemyHUD).ToList();

        if (player.Count == 0 && allHuds.Count > 0) player.Add(allHuds[0]);
        if (enemy.Count == 0 && allHuds.Count > 1) enemy.Add(allHuds[1]);

        ExpandByCloning(player, requiredPlayerSlots, "PlayerHUD_");
        ExpandByCloning(enemy, requiredEnemySlots, "EnemyHUD_");

        SetHudList(so, "playerHUDs", player.Take(requiredPlayerSlots).ToList());
        SetHudList(so, "enemyHUDs", enemy.Take(requiredEnemySlots).ToList());

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(battleUI);
    }

    private static bool IsPlayerHUD(UnitHUD hud)
    {
        if (hud == null) return false;
        string n = hud.gameObject.name.ToLower();
        return n.Contains("player") || n.Contains("hero") || n.Contains("tuan");
    }

    private static bool IsEnemyHUD(UnitHUD hud)
    {
        if (hud == null) return false;
        string n = hud.gameObject.name.ToLower();
        return n.Contains("enemy") || n.Contains("linh") || n.Contains("monster");
    }

    private static void ExpandByCloning(List<UnitHUD> list, int targetCount, string baseName)
    {
        if (list.Count == 0) return;
        int safety = 0;
        while (list.Count < targetCount && safety++ < 10)
        {
            var template = list[0];
            var cloneObj = Object.Instantiate(template.gameObject, template.transform.parent);
            cloneObj.name = baseName + (list.Count);
            var cloneRt = cloneObj.GetComponent<RectTransform>();
            var templateRt = template.GetComponent<RectTransform>();
            if (cloneRt != null && templateRt != null)
            {
                cloneRt.anchoredPosition = templateRt.anchoredPosition + new Vector2(0f, -110f * list.Count);
            }
            var cloneHud = cloneObj.GetComponent<UnitHUD>();
            if (cloneHud != null) list.Add(cloneHud);
        }
    }

    private static void SetHudList(SerializedObject so, string propName, List<UnitHUD> huds)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) return;

        prop.arraySize = huds.Count;
        for (int i = 0; i < huds.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = huds[i];
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    /// <summary>Tìm GameObject theo tên (case-insensitive, breadth-first).</summary>
    private static GameObject FindDeepByName(Transform root, string name)
    {
        string lowerName = name.ToLower();
        var queue = new Queue<Transform>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.name.ToLower() == lowerName)
                return current.gameObject;

            foreach (Transform child in current)
                queue.Enqueue(child);
        }
        return null;
    }
}
