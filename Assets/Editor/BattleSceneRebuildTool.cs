using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Rebuild toàn bộ BattleScene từ một scene trống.
/// Menu: Tools/Battle/Rebuild Battle Scene
/// </summary>
public class BattleSceneRebuildTool : EditorWindow
{
    const string BATTLE_SCENE      = "Assets/Scenes/BattleScene.unity";
    const string TUTORIAL_SCENE    = "Assets/Scenes/Chapter1_Tutorial.unity";
    const string DAMAGE_POPUP_PATH = "Assets/Prefabs/UI/DamagePopup.prefab";

    const string GRP_SYSTEMS    = "[Systems]";
    const string GRP_CAMERA     = "[Camera]";
    const string GRP_UI         = "[UI]";
    const string GRP_BACKGROUND = "[Background]";
    const string GRP_SPAWN      = "[Spawning]";
    const string GRP_EFFECTS    = "[Effects]";

    bool optCleanup      = true;
    bool optCopyUI       = true;
    bool optWireUI       = true;
    bool optSpawnAnchors = true;
    bool optSystems      = true;
    bool optOrganize     = true;

    [MenuItem("Tools/Battle/Rebuild Battle Scene")]
    public static void Open()
    {
        var w = GetWindow<BattleSceneRebuildTool>("Rebuild Battle Scene");
        w.minSize = new Vector2(440, 400);
        w.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Rebuild Battle Scene", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tạo lại BattleScene hoàn chỉnh:\n" +
            "  1. Dọn sạch Canvas/Tutorial cũ\n" +
            "  2. Copy Canvas UI từ Chapter1_Tutorial\n" +
            "     (background tự load từ MapData lúc runtime)\n" +
            "  3. Tạo HUD mới (Slider) + wire toàn bộ BattleUI\n" +
            "  4. Tạo spawn anchors + TargetCursor → wire BattleManager\n" +
            "  5. Tạo BattleRunner, BattleSceneBootstrap,\n" +
            "     BattleBackgroundController, BattleDebugUI,\n" +
            "     EventSystem, Camera\n" +
            "  6. Tổ chức hierarchy\n\n" +
            "Mở BattleScene trước, hoặc tool tự mở.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        optCleanup      = EditorGUILayout.Toggle("1. Dọn rác cũ",                                 optCleanup);
        optCopyUI       = EditorGUILayout.Toggle("2. Copy Canvas UI từ Chapter1_Tutorial",         optCopyUI);
        optWireUI       = EditorGUILayout.Toggle("3. Tạo HUD + wire BattleUI",                    optWireUI);
        EditorGUILayout.HelpBox(
            "   HUD player/enemy tạo mới hoàn toàn — data nạp runtime từ MapData.",
            MessageType.None);
        optSpawnAnchors = EditorGUILayout.Toggle("4. Spawn anchors + TargetCursor + BattleManager", optSpawnAnchors);
        optSystems      = EditorGUILayout.Toggle("5. Tạo tất cả systems còn thiếu",               optSystems);
        optOrganize     = EditorGUILayout.Toggle("6. Tổ chức hierarchy",                           optOrganize);

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.4f, 0.85f, 0.4f);
        if (GUILayout.Button("Rebuild", GUILayout.Height(44)))
            Run();
        GUI.backgroundColor = Color.white;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PIPELINE
    // ═══════════════════════════════════════════════════════════════════════════

    void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene battle = EnsureBattleSceneOpen();
        if (!battle.IsValid()) { Debug.LogError("[Rebuild] Không mở được BattleScene."); return; }

        // ── 1. Dọn ──────────────────────────────────────────────────────────
        if (optCleanup)
        {
            Cleanup(battle);
            Debug.Log("[Rebuild] 1. Dọn xong.");
        }

        // ── 2. Copy UI từ Tutorial ───────────────────────────────────────────
        GameObject battleCanvas = null;
        if (optCopyUI)
        {
            battleCanvas = CopyTutorialUI(battle);
            Debug.Log(battleCanvas != null
                ? "[Rebuild] 2. Copy UI xong."
                : "[Rebuild] 2. Không tìm thấy Tutorial canvas — bỏ qua.");
        }
        if (battleCanvas == null)
            battleCanvas = FindRootCanvas(battle);

        // ── 3. Wire BattleUI ─────────────────────────────────────────────────
        if (optWireUI)
        {
            if (battleCanvas == null)
            {
                // Tạo canvas trắng nếu không có gì
                var go = new GameObject("BattleUI_Canvas", typeof(RectTransform), typeof(Canvas),
                                        typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(go, battle);
                var c = go.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 100;
                var cs = go.GetComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                battleCanvas = go;
            }
            WireBattleUI(battleCanvas, battle);
            Debug.Log("[Rebuild] 3. Wire BattleUI xong.");
        }

        // ── 4. BattleManager + Spawn anchors + Cursor ────────────────────────
        if (optSpawnAnchors)
        {
            EnsureBattleManager(battle);
            EnsureEnemyHUDPrefab(battle);
            Debug.Log("[Rebuild] 4. BattleManager + anchors + EnemyHUDPrefab xong.");
        }

        // ── 5. Systems ───────────────────────────────────────────────────────
        if (optSystems)
        {
            EnsureCamera(battle);
            EnsureEventSystem(battle);
            EnsureInputController(battle);
            EnsureBattleRunner(battle);
            EnsureBootstrap(battle);
            EnsureBackgroundController(battle);
            EnsureBattleDebugUI(battleCanvas, battle);
            Debug.Log("[Rebuild] 5. Systems xong.");
        }

        // ── 6. Hierarchy ─────────────────────────────────────────────────────
        if (optOrganize)
        {
            OrganizeHierarchy(battle);
            Debug.Log("[Rebuild] 6. Hierarchy xong.");
        }

        EditorSceneManager.MarkSceneDirty(battle);
        EditorSceneManager.SaveScene(battle);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Hoàn tất",
            "BattleScene đã được rebuild!\n\n" +
            "Việc còn lại:\n" +
            "• BattleSceneBootstrap → gán debugMapData (MapData asset)\n" +
            "• BattleManager → gán debug prefabs nếu muốn demo mode\n" +
            "• BattleBackgroundController → gán defaultBackgroundPrefab\n" +
            "• Kiểm tra BattleUI Inspector — field nào còn None cần wire thủ công",
            "OK");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STEP 1 — CLEANUP
    // ═══════════════════════════════════════════════════════════════════════════

    static void Cleanup(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects().ToList())
        {
            if (root == null) continue;

            // Xoá tutorial junk
            if (root.name.StartsWith("TutorialCopy_") ||
                root.name == "Chapter1_Tutorial" ||
                root.GetComponentInChildren<SimpleTutorialManager>(true) != null)
            {
                Undo.DestroyObjectImmediate(root);
                continue;
            }

            // Xoá Canvas bị tắt
            var c = root.GetComponent<Canvas>();
            if (c != null && (!root.activeSelf || !c.enabled))
                Undo.DestroyObjectImmediate(root);
        }

        // Xoá Canvas trùng — giữ lại 1
        Canvas kept = null;
        foreach (var root in scene.GetRootGameObjects().ToList())
        {
            if (root == null) continue;
            var c = root.GetComponent<Canvas>();
            if (c == null) continue;
            if (kept == null) { kept = c; continue; }
            Undo.DestroyObjectImmediate(root);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STEP 2 — COPY UI FROM TUTORIAL
    // ═══════════════════════════════════════════════════════════════════════════

    static GameObject CopyTutorialUI(Scene battle)
    {
        var tutorial = EditorSceneManager.OpenScene(TUTORIAL_SCENE, OpenSceneMode.Additive);
        if (!tutorial.IsValid()) return null;

        try
        {
            var tutCanvas = FindMainCanvas(tutorial);
            if (tutCanvas == null) return null;

            // Background KHÔNG copy từ Tutorial —
            // BattleBackgroundController tự load từ MapData.battleBackgroundPrefab lúc runtime.

            // Copy canvas
            var battleCanvas = Object.Instantiate(tutCanvas);
            battleCanvas.name = "BattleUI_Canvas";
            SceneManager.MoveGameObjectToScene(battleCanvas, battle);
            battleCanvas.transform.SetParent(null);
            battleCanvas.SetActive(true);
            var cv = battleCanvas.GetComponent<Canvas>();
            if (cv != null)
            {
                cv.enabled = true;
                cv.renderMode = RenderMode.ScreenSpaceOverlay;
                cv.sortingOrder = 100;
            }
            var cs = battleCanvas.GetComponent<CanvasScaler>();
            if (cs != null)
            {
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
            }
            Undo.RegisterCreatedObjectUndo(battleCanvas, "Copy Canvas");
            return battleCanvas;
        }
        finally
        {
            EditorSceneManager.CloseScene(tutorial, true);
        }
    }

    static GameObject FindMainCanvas(Scene scene)
    {
        Canvas best = null; int bestCount = -1;
        foreach (var root in scene.GetRootGameObjects())
        {
            var rc = root.GetComponent<Canvas>();
            if (rc != null && root.transform.childCount > bestCount)
            { bestCount = root.transform.childCount; best = rc; }
        }
        return best?.gameObject;
    }


    // ═══════════════════════════════════════════════════════════════════════════
    // STEP 3 — WIRE BATTLE UI
    // ═══════════════════════════════════════════════════════════════════════════

    static void WireBattleUI(GameObject canvasGO, Scene scene)
    {
        var ui = canvasGO.GetComponentInChildren<BattleUI>(true)
                 ?? canvasGO.AddComponent<BattleUI>();

        var so = new SerializedObject(ui);

        // Panels — Tutorial: Menu_Frame / Skill_Frame / Item_Frame
        WireGO(so, "actionMenuPanel", canvasGO, "Menu_Frame",  "Action_Frame", "ActionMenu",  "Main_Frame");
        WireGO(so, "skillMenuPanel",  canvasGO, "Skill_Frame", "SkillMenu",    "Skill_Menu",  "SkillFrame");
        WireGO(so, "itemMenuPanel",   canvasGO, "Item_Frame",  "ItemMenu",     "Item_Menu",   "ItemFrame");
        WireGO(so, "resultPanel",     canvasGO, "Result",      "ResultPanel",  "Win_Panel",   "BattleResult");

        // Action Menu buttons
        WireBtn(so, "btnAttack",    canvasGO, "Btn_Attack",  "Attack",   "BasicAttack");
        WireBtn(so, "btnSkill",     canvasGO, "Btn_Skills",  "Btn_Skill","Skill");
        WireBtn(so, "btnOpenItem",  canvasGO, "Btn_Items",   "Btn_Item", "Items");

        // Skill Menu buttons
        WireBtn(so, "btnSkillBack", canvasGO, "Btn_Back",    "Back",     "Btn_Cancel", "Btn_Cleanse");

        // Item Menu buttons — Btn_Heal=Parry, Btn_Energy=Back, Btn_Cleanse=Flee
        WireBtn(so, "btnParry",    canvasGO, "Btn_Heal",    "Btn_Parry",   "Parry");
        WireBtn(so, "btnItemBack", canvasGO, "Btn_Energy",  "Btn_Back",    "Back");
        WireBtn(so, "btnFlee",     canvasGO, "Btn_Cleanse", "Btn_Flee",    "Flee",   "Btn_Run");

        // Skill button list
        WireSkillButtons(so, canvasGO);

        // Dùng HUD có sẵn trong Tutorial canvas — Image.fillAmount style
        WireTutorialHUDs(so, canvasGO);

        // Elements còn thiếu — tạo mới
        EnsureTurnOrderPanel(so, canvasGO);
        EnsureTurnIndicator(so, canvasGO);
        EnsureBattleLog(so, canvasGO);
        EnsureStatusEffects(so, canvasGO);
        EnsureResultPanel(so, canvasGO);
        EnsureTargetNameText(so, canvasGO);
        EnsureSkillLabels(so, canvasGO);
        EnsureDamagePopupPrefab(so);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ui);
    }

    // ── Wire HUD từ Tutorial ─────────────────────────────────────────────────
    // Tutorial dùng Image.fillAmount (không phải Slider).
    // Player HUD: HUD_Background → HP_Fill, HP_Text_Values_Player, BP_Dot_*
    // Enemy HUD:  Enemy_HP_Canvas → Enemy_HP_Fill, HP_Text_Values_Enemy
    // Enemy HUD: WorldSpace, spawn runtime bởi BattleManager dưới chân mỗi model

    static void WireTutorialHUDs(SerializedObject so, GameObject canvasGO)
    {
        var playerProp = so.FindProperty("playerHUDs");
        var enemyProp  = so.FindProperty("enemyHUDs");

        // ── Player HUD ── gắn UnitHUD vào HUD_Background có sẵn
        var hudBg = DeepFind(canvasGO.transform, "HUD_Background");
        if (hudBg != null)
        {
            // Giữ nguyên màu nền Tutorial (không đổi)
            var bgImg = hudBg.GetComponent<Image>();

            var hud = hudBg.GetComponent<UnitHUD>() ?? hudBg.AddComponent<UnitHUD>();
            var hudSO = new SerializedObject(hud);

            var hpFill = DeepFind(hudBg.transform, "HP_Fill");
            var hpText = DeepFind(hudBg.transform, "HP_Text_Values_Player");

            // Highlight — tạo nếu chưa có, đặt ra sau (sibling 0) để không che HP bar
            var highlight = hudBg.transform.Find("Highlight");
            if (highlight == null)
            {
                var hGO = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
                hGO.transform.SetParent(hudBg.transform, false);
                hGO.transform.SetAsFirstSibling();
                Fill(hGO.GetComponent<RectTransform>());
                hGO.GetComponent<Image>().color = new Color(1f, 0.85f, 0.2f, 0f);
                highlight = hGO.transform;
            }
            else
            {
                highlight.SetAsFirstSibling();
            }

            // Giảm alpha highlight để không che khuất nội dung khi đến lượt player
            var hudSO2 = new SerializedObject(hudBg.GetComponent<UnitHUD>() ?? hudBg.AddComponent<UnitHUD>());
            var hlColorProp = hudSO2.FindProperty("highlightActiveColor");
            if (hlColorProp != null)
            {
                var col = hlColorProp.colorValue;
                col.a = 0.35f;
                hlColorProp.colorValue = col;
                hudSO2.ApplyModifiedPropertiesWithoutUndo();
            }

            // Tìm tất cả BP_Dot con trong HUD_Background (tên Unity: BP_Dot_1, BP_Dot_1 (1), (2)...)
            var dots = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in hudBg.transform)
                if (child.name.StartsWith("BP_Dot") || child.name.StartsWith("AP_Glow"))
                    dots.Add(child.gameObject);

            hudSO.FindProperty("hpFillImage").objectReferenceValue   = hpFill?.GetComponent<Image>();
            hudSO.FindProperty("hpText").objectReferenceValue         = hpText?.GetComponent<TextMeshProUGUI>();
            hudSO.FindProperty("backgroundImage").objectReferenceValue = bgImg;
            hudSO.FindProperty("highlightImage").objectReferenceValue  = highlight.GetComponent<Image>();

            var dotsProp = hudSO.FindProperty("apDots");
            dotsProp.arraySize = dots.Count;
            for (int i = 0; i < dots.Count; i++)
                dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
            Debug.Log($"[Rebuild] Wire apDots: {dots.Count} dots tìm thấy trong HUD_Background.");

            hudSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);

            playerProp.arraySize = 1;
            playerProp.GetArrayElementAtIndex(0).objectReferenceValue = hud;
            Debug.Log("[Rebuild] Wire PlayerHUD từ HUD_Background.");
        }
        else
        {
            Debug.LogWarning("[Rebuild] Không tìm thấy HUD_Background trong canvas.");
        }

        // ── Enemy HUD ── WorldSpace, spawn runtime dưới chân model bởi BattleManager
        // Xoá Enemy_HP_Canvas gốc (WorldSpace gắn vào BinhLinh_Combat, không còn dùng)
        var enemyHPCanvas = DeepFind(canvasGO.transform, "Enemy_HP_Canvas");
        if (enemyHPCanvas != null)
        {
            Undo.DestroyObjectImmediate(enemyHPCanvas);
            Debug.Log("[Rebuild] Xoá Enemy_HP_Canvas gốc — enemy HUD sẽ spawn runtime theo model.");
        }

        // Xoá EnemyHUD_* Screen Space cũ còn sót từ lần chạy tool trước
        foreach (Transform child in canvasGO.transform)
        {
            if (child != null && child.name.StartsWith("EnemyHUD_"))
                Undo.DestroyObjectImmediate(child.gameObject);
        }

        // enemyHUDs để trống — BattleManager.RegisterEnemyHUD() điền lúc Play
        enemyProp.arraySize = 0;
        Debug.Log("[Rebuild] Dọn EnemyHUD cũ. enemyHUDs sẽ được điền runtime bởi BattleManager.");
    }

    // ── Auto-created UI elements ──────────────────────────────────────────────

    static void EnsureTurnOrderPanel(SerializedObject so, GameObject root)
    {
        var panelP = so.FindProperty("turnOrderPanel");
        var textP  = so.FindProperty("turnOrderText");
        if (panelP.objectReferenceValue != null) return;

        var panel = NewGO(name: "TurnOrderPanel", root.transform, typeof(Image));
        panel.GetComponent<Image>().color = new Color(0,0,0,0.5f);
        Anchor(panel.GetComponent<RectTransform>(),
            new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0.5f,1f),
            new Vector2(0,-10f), new Vector2(380f,50f));
        panelP.objectReferenceValue = panel;

        var tmp = NewGO<TextMeshProUGUI>("TurnOrderText", panel.transform);
        tmp.fontSize = 22; tmp.alignment = TextAlignmentOptions.Center;
        Fill(tmp.GetComponent<RectTransform>());
        textP.objectReferenceValue = tmp;
    }

    static void EnsureTurnIndicator(SerializedObject so, GameObject root)
    {
        var p = so.FindProperty("turnIndicatorText");
        if (p.objectReferenceValue != null) return;

        var tmp = NewGO<TextMeshProUGUI>("TurnIndicatorText", root.transform);
        tmp.fontSize = 28; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.95f, 0.6f);
        Anchor(tmp.GetComponent<RectTransform>(),
            new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0.5f,1f),
            new Vector2(0,-68f), new Vector2(400f,40f));
        p.objectReferenceValue = tmp;
    }

    static void EnsureBattleLog(SerializedObject so, GameObject root)
    {
        var p = so.FindProperty("battleLogText");
        if (p.objectReferenceValue != null) return;

        var bg = NewGO("BattleLogPanel", root.transform, typeof(Image));
        bg.GetComponent<Image>().color = new Color(0,0,0,0.45f);
        Anchor(bg.GetComponent<RectTransform>(),
            new Vector2(0,0), new Vector2(0,0), new Vector2(0,0),
            new Vector2(16f,16f), new Vector2(380f,160f));

        var tmp = NewGO<TextMeshProUGUI>("BattleLogText", bg.transform);
        tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.BottomLeft;
        Fill(tmp.GetComponent<RectTransform>(), new Vector2(8,6));
        p.objectReferenceValue = tmp;
    }

    static void EnsureStatusEffects(SerializedObject so, GameObject root)
    {
        var pp = so.FindProperty("playerEffectsText");
        var ep = so.FindProperty("enemyEffectsText");

        if (pp.objectReferenceValue == null)
        {
            var tmp = NewGO<TextMeshProUGUI>("PlayerEffectsText", root.transform);
            tmp.fontSize = 13;
            Anchor(tmp.GetComponent<RectTransform>(),
                new Vector2(0,0), new Vector2(0,0), new Vector2(0,0),
                new Vector2(16f,185f), new Vector2(240f,80f));
            pp.objectReferenceValue = tmp;
        }
        if (ep.objectReferenceValue == null)
        {
            var tmp = NewGO<TextMeshProUGUI>("EnemyEffectsText", root.transform);
            tmp.fontSize = 13; tmp.alignment = TextAlignmentOptions.Right;
            Anchor(tmp.GetComponent<RectTransform>(),
                new Vector2(1,0), new Vector2(1,0), new Vector2(1,0),
                new Vector2(-16f,185f), new Vector2(240f,80f));
            ep.objectReferenceValue = tmp;
        }
    }

    static void EnsureResultPanel(SerializedObject so, GameObject root)
    {
        var panelP  = so.FindProperty("resultPanel");
        var resultP = so.FindProperty("resultText");
        var expP    = so.FindProperty("expText");

        if (panelP.objectReferenceValue == null)
        {
            var panel = NewGO("ResultPanel", root.transform, typeof(Image));
            panel.GetComponent<Image>().color = new Color(0,0,0,0.78f);
            Anchor(panel.GetComponent<RectTransform>(),
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                Vector2.zero, new Vector2(600f,300f));
            panel.SetActive(false);
            panelP.objectReferenceValue = panel;

            var rTMP = NewGO<TextMeshProUGUI>("ResultText", panel.transform);
            rTMP.fontSize = 60; rTMP.alignment = TextAlignmentOptions.Center;
            rTMP.color = new Color(1f, 0.9f, 0.2f);
            Anchor(rTMP.GetComponent<RectTransform>(),
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0,50f), new Vector2(560f,80f));
            resultP.objectReferenceValue = rTMP;

            var eTMP = NewGO<TextMeshProUGUI>("ExpText", panel.transform);
            eTMP.fontSize = 22; eTMP.alignment = TextAlignmentOptions.Center;
            Anchor(eTMP.GetComponent<RectTransform>(),
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0,-40f), new Vector2(540f,60f));
            expP.objectReferenceValue = eTMP;
        }
    }

    static void EnsureTargetNameText(SerializedObject so, GameObject root)
    {
        var p = so.FindProperty("targetNameText");
        if (p.objectReferenceValue != null) return;

        var tmp = NewGO<TextMeshProUGUI>("TargetNameText", root.transform);
        tmp.fontSize = 18; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 0.6f);
        Anchor(tmp.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(200f,0), new Vector2(200f,30f));
        p.objectReferenceValue = tmp;
    }

    static void EnsureSkillLabels(SerializedObject so, GameObject root)
    {
        var listP = so.FindProperty("skillLabels");
        if (listP.arraySize > 0) return;

        var btnsProp = so.FindProperty("skillButtons");
        if (btnsProp.arraySize == 0) return;

        var labels = new List<TextMeshProUGUI>();
        for (int i = 0; i < btnsProp.arraySize; i++)
        {
            var btn = btnsProp.GetArrayElementAtIndex(i).objectReferenceValue as Button;
            if (btn == null) continue;
            var existing = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (existing != null) { labels.Add(existing); continue; }

            var tmp = NewGO<TextMeshProUGUI>("SkillLabel", btn.transform);
            tmp.fontSize = 14; tmp.alignment = TextAlignmentOptions.Center;
            Fill(tmp.GetComponent<RectTransform>());
            labels.Add(tmp);
        }
        SetList(listP, labels.Cast<Object>().ToList());
    }

    static void EnsureDamagePopupPrefab(SerializedObject so)
    {
        var p = so.FindProperty("damagePopupPrefab");
        if (p.objectReferenceValue != null) return;

        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(DAMAGE_POPUP_PATH);
        if (asset == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

            var go  = new GameObject("DamagePopup", typeof(RectTransform), typeof(TextMeshPro));
            var tmp = go.GetComponent<TextMeshPro>();
            tmp.fontSize = 6; tmp.alignment = TextAlignmentOptions.Center;
            asset = PrefabUtility.SaveAsPrefabAsset(go, DAMAGE_POPUP_PATH);
            Object.DestroyImmediate(go);
            Debug.Log("[Rebuild] Tạo DamagePopup prefab.");
        }
        p.objectReferenceValue = asset;
    }

    // ── Wire helpers ──────────────────────────────────────────────────────────

    static void WireGO(SerializedObject so, string prop, GameObject root, params string[] names)
    {
        var p = so.FindProperty(prop);
        if (p == null) return;
        foreach (var n in names)
        {
            var f = DeepFind(root.transform, n);
            if (f != null) { p.objectReferenceValue = f; Debug.Log($"[Rebuild] Wire {prop} → {f.name}"); return; }
        }
        Debug.LogWarning($"[Rebuild] Không tìm thấy panel '{prop}' (tried: {string.Join(", ", names)})");
    }

    static void WireBtn(SerializedObject so, string prop, GameObject root, params string[] names)
    {
        var p = so.FindProperty(prop);
        if (p == null) return;
        foreach (var n in names)
        {
            var f = DeepFind(root.transform, n);
            var btn = f?.GetComponent<Button>();
            if (btn != null) { p.objectReferenceValue = btn; Debug.Log($"[Rebuild] Wire {prop} → {f.name}"); return; }
        }
        Debug.LogWarning($"[Rebuild] Không tìm thấy button '{prop}' (tried: {string.Join(", ", names)})");
    }

    static void WireSkillButtons(SerializedObject so, GameObject root)
    {
        var p = so.FindProperty("skillButtons");
        if (p == null || p.arraySize > 0) return;
        var found = new List<Button>();
        for (int i = 1; i <= 3; i++)
        {
            foreach (var n in new[] { $"Btn_Skill{i}", $"Skill{i}", $"SkillBtn{i}", $"Btn_Skill_{i}" })
            {
                var btn = DeepFind(root.transform, n)?.GetComponent<Button>();
                if (btn != null) { found.Add(btn); break; }
            }
        }
        SetList(p, found.Cast<Object>().ToList());
        Debug.Log($"[Rebuild] Wire skillButtons: {found.Count}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STEP 4 — BATTLE MANAGER + ANCHORS + CURSOR
    // ═══════════════════════════════════════════════════════════════════════════

    static void EnsureBattleManager(Scene scene)
    {
        var bm = Object.FindFirstObjectByType<BattleManager>();
        if (bm == null)
        {
            var go = new GameObject("BattleManager");
            SceneManager.MoveGameObjectToScene(go, scene);
            bm = go.AddComponent<BattleManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create BattleManager");
            Debug.Log("[Rebuild] Tạo BattleManager.");
        }

        var so = new SerializedObject(bm);

        // Player spawn anchor
        if (so.FindProperty("playerSpawnAnchor").objectReferenceValue == null)
        {
            var t = GameObject.Find("PlayerSpawnAnchor")?.transform
                    ?? CreateAnchor("PlayerSpawnAnchor", new Vector3(-4f, 0f, 0f), scene);
            so.FindProperty("playerSpawnAnchor").objectReferenceValue = t;
        }

        // Enemy spawn anchor
        if (so.FindProperty("enemySpawnAnchor").objectReferenceValue == null)
        {
            var t = GameObject.Find("EnemySpawnAnchor")?.transform
                    ?? CreateAnchor("EnemySpawnAnchor", new Vector3(4f, 0f, 0f), scene);
            so.FindProperty("enemySpawnAnchor").objectReferenceValue = t;
        }

        // Enemy target cursor
        if (so.FindProperty("targetCursor").objectReferenceValue == null)
        {
            var cursor = GameObject.Find("TargetCursor") ?? CreateCursorGO("TargetCursor", scene, new Color(1f, 0.9f, 0.1f, 0.85f));
            so.FindProperty("targetCursor").objectReferenceValue = cursor;
        }

        // Player turn cursor
        if (so.FindProperty("playerTurnCursor").objectReferenceValue == null)
        {
            var cursor = GameObject.Find("PlayerTurnCursor") ?? CreateCursorGO("PlayerTurnCursor", scene, new Color(0.4f, 0.8f, 1f, 0.85f));
            so.FindProperty("playerTurnCursor").objectReferenceValue = cursor;
        }

        // spawnSpacing mặc định
        var spacingP = so.FindProperty("spawnSpacing");
        if (spacingP.floatValue == 0f) spacingP.floatValue = 2f;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bm);
    }

    static void EnsureEnemyHUDPrefab(Scene scene)
    {
        const string PREFAB_PATH = "Assets/Prefabs/UI/EnemyHUD_World.prefab";

        var bm = Object.FindFirstObjectByType<BattleManager>();
        if (bm == null) return;

        var so = new SerializedObject(bm);
        var prop = so.FindProperty("enemyHUDPrefab");

        // Luôn tạo lại để đảm bảo đúng style Tutorial
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
            AssetDatabase.DeleteAsset(PREFAB_PATH);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))    AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        // Root: WorldSpace Canvas — kích thước giống Enemy_HP_Canvas trong Tutorial (3.74 x 0.25 world units)
        var root   = new GameObject("EnemyHUD_World");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.WorldSpace;
        canvas.sortingLayerName = "Player";
        canvas.sortingOrder   = 100;
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta     = new Vector2(2f, 0.2f); // world units, scale bù bởi enemyHUDCanvasScale
        rt.localScale    = Vector3.one;

        // Background đen mờ (giống Tutorial)
        var bgGO  = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(root.transform, false);
        bgGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.75f);
        var bgRt  = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        // HP Fill đỏ — y hệt Enemy_HP_Fill trong Tutorial (fillAmount Horizontal)
        var fillGO  = new GameObject("Enemy_HP_Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(root.transform, false);
        var fillImg       = fillGO.GetComponent<Image>();
        fillImg.color     = new Color(0.880f, 0.064f, 0.064f); // màu Tutorial
        fillImg.type      = Image.Type.Filled;
        fillImg.fillMethod   = Image.FillMethod.Horizontal;
        fillImg.fillOrigin   = 0;
        fillImg.fillAmount   = 1f;
        var fillRt = fillGO.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(1f, 1f); fillRt.offsetMax = new Vector2(-1f, -1f);

        // UnitHUD component — wire HP Fill
        var hud   = root.AddComponent<UnitHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("hpFillImage").objectReferenceValue = fillImg;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        var existing = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);
        Debug.Log("[Rebuild] Tạo EnemyHUD_World prefab (style Tutorial) tại " + PREFAB_PATH);

        prop.objectReferenceValue = existing;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bm);
    }

    static GameObject CreateCursorGO(string name, Scene scene, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = Vector3.zero;
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color  = color;
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 50;
        go.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        go.SetActive(false);
        Debug.Log($"[Rebuild] Tạo {name}.");
        return go;
    }

    static Transform CreateAnchor(string name, Vector3 pos, Scene scene)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create Anchor");
        Debug.Log($"[Rebuild] Tạo {name} tại {pos}.");
        return go.transform;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STEP 5 — SYSTEMS
    // ═══════════════════════════════════════════════════════════════════════════

    static void EnsureCamera(Scene scene)
    {
        if (Object.FindFirstObjectByType<Camera>() != null) return;
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        go.GetComponent<Camera>().backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create Camera");
        Debug.Log("[Rebuild] Tạo Main Camera.");
    }

    static void EnsureEventSystem(Scene scene)
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        Debug.Log("[Rebuild] Tạo EventSystem.");
    }

    static void EnsureInputController(Scene scene)
    {
        if (Object.FindFirstObjectByType<InputController>() != null) return;
        var go = new GameObject("InputController");
        go.AddComponent<InputController>();
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create InputController");
        Debug.Log("[Rebuild] Tạo InputController.");
    }

    static void EnsureBattleRunner(Scene scene)
    {
        if (Object.FindFirstObjectByType<BattleRunner>() != null) return;
        var go = new GameObject("BattleRunner");
        go.AddComponent<BattleRunner>();
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create BattleRunner");
        Debug.Log("[Rebuild] Tạo BattleRunner.");
    }

    static void EnsureBootstrap(Scene scene)
    {
        if (Object.FindFirstObjectByType<BattleSceneBootstrap>() != null) return;
        var go = new GameObject("BattleSceneBootstrap");
        go.AddComponent<BattleSceneBootstrap>();
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create Bootstrap");
        Debug.Log("[Rebuild] Tạo BattleSceneBootstrap — nhớ gán debugMapData.");
    }

    static void EnsureBackgroundController(Scene scene)
    {
        if (Object.FindFirstObjectByType<BattleBackgroundController>() != null) return;
        var go = new GameObject("BattleBackgroundController");
        go.AddComponent<BattleBackgroundController>();
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create BackgroundController");
        Debug.Log("[Rebuild] Tạo BattleBackgroundController.");
    }

    static void EnsureBattleDebugUI(GameObject canvasGO, Scene scene)
    {
        if (Object.FindFirstObjectByType<BattleDebugUI>() != null) return;

        Transform parent = canvasGO != null ? canvasGO.transform : null;
        var go = new GameObject("BattleDebugUI", typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        else SceneManager.MoveGameObjectToScene(go, scene);

        var debugUI = go.AddComponent<BattleDebugUI>();

        // Panel nền
        var panelImg = go.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.55f);
        Anchor(go.GetComponent<RectTransform>(),
            new Vector2(1,0), new Vector2(1,0), new Vector2(1,0),
            new Vector2(-16f, 16f), new Vector2(360f, 200f));

        // Debug text
        var textGO = NewGO<TextMeshProUGUI>("DebugText", go.transform);
        textGO.fontSize = 13; textGO.alignment = TextAlignmentOptions.BottomRight;
        Fill(textGO.GetComponent<RectTransform>(), new Vector2(6, 6));

        var debugSO = new SerializedObject(debugUI);
        debugSO.FindProperty("debugText").objectReferenceValue = textGO;
        debugSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(debugUI);

        if (parent == null) Undo.RegisterCreatedObjectUndo(go, "Create BattleDebugUI");
        Debug.Log("[Rebuild] Tạo BattleDebugUI.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STEP 6 — HIERARCHY
    // ═══════════════════════════════════════════════════════════════════════════

    static void OrganizeHierarchy(Scene scene)
    {
        var sys = EnsureGroup(scene, GRP_SYSTEMS);
        var cam = EnsureGroup(scene, GRP_CAMERA);
        var ui  = EnsureGroup(scene, GRP_UI);
        var bg  = EnsureGroup(scene, GRP_BACKGROUND);
        var sp  = EnsureGroup(scene, GRP_SPAWN);
        var fx  = EnsureGroup(scene, GRP_EFFECTS);

        foreach (var go in scene.GetRootGameObjects().ToList())
        {
            if (go == null || IsGroup(go)) continue;
            var target = Classify(go, sys, cam, ui, bg, sp, fx);
            if (target != null && target != go)
                Undo.SetTransformParent(go.transform, target.transform, "Organize");
        }

        foreach (var go in scene.GetRootGameObjects().ToList())
            if (go != null && IsGroup(go) && go.transform.childCount == 0)
                Undo.DestroyObjectImmediate(go);
    }

    static GameObject Classify(GameObject go,
        GameObject sys, GameObject cam, GameObject ui,
        GameObject bg, GameObject sp, GameObject fx)
    {
        if (go.GetComponent<Camera>() != null) return cam;
        if (go.GetComponent<Canvas>() != null) return ui;
        if (go.GetComponent<EventSystem>() != null) return ui;

        if (HasType(go, typeof(BattleManager), typeof(BattleRunner),
                        typeof(BattleSceneBootstrap), typeof(MapManager),
                        typeof(GameManager), typeof(InputController),
                        typeof(QuestManager), typeof(BattleBackgroundController)))
            return sys;

        string n = go.name.ToLower();
        if (n.Contains("anchor") || n.Contains("spawn")) return sp;
        if (n.Contains("cursor") || n.Contains("target")) return sp;
        if (n.StartsWith("tutorialcopy_") || n.Contains("background") ||
            n.Contains("battlefield") || n.Contains("scene_combat")) return bg;

        if (go.GetComponentInChildren<ParticleSystem>(true) != null) return fx;
        if (n.Contains("fx") || n.Contains("vfx") || n.Contains("effect")) return fx;

        if (go.GetComponentInChildren<Renderer>(true) != null ||
            go.GetComponentInChildren<Animator>(true) != null) return bg;

        return sys;
    }

    static GameObject EnsureGroup(Scene scene, string name)
    {
        foreach (var r in scene.GetRootGameObjects())
            if (r != null && r.name == name) return r;
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create Group");
        return go;
    }

    static bool IsGroup(GameObject go) =>
        go.name == GRP_SYSTEMS || go.name == GRP_CAMERA || go.name == GRP_UI ||
        go.name == GRP_BACKGROUND || go.name == GRP_SPAWN || go.name == GRP_EFFECTS;

    // ═══════════════════════════════════════════════════════════════════════════
    // PRIMITIVES
    // ═══════════════════════════════════════════════════════════════════════════

    static GameObject NewGO(string name, Transform parent, params System.Type[] extras)
    {
        var types = new System.Type[] { typeof(RectTransform) }.Concat(extras).ToArray();
        var go = new GameObject(name, types);
        go.transform.SetParent(parent, false);
        return go;
    }

    static T NewGO<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(T));
        go.transform.SetParent(parent, false);
        return go.GetComponent<T>();
    }

    static void Anchor(RectTransform rt, Vector2 amin, Vector2 amax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = amin; rt.anchorMax = amax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    static void Fill(RectTransform rt, Vector2 pad = default)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = pad; rt.offsetMax = -pad;
    }

    static void SetList(SerializedProperty prop, List<Object> items)
    {
        prop.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    static GameObject DeepFind(Transform root, string name)
    {
        string lo = name.ToLower();
        var q = new Queue<Transform>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur.name.ToLower() == lo) return cur.gameObject;
            foreach (Transform c in cur) q.Enqueue(c);
        }
        return null;
    }

    static bool HasType(GameObject go, params System.Type[] types)
    {
        foreach (var t in types)
            if (t != null && go.GetComponent(t) != null) return true;
        return false;
    }

    static Scene EnsureBattleSceneOpen()
    {
        var active = SceneManager.GetActiveScene();
        if (active.path == BATTLE_SCENE) return active;
        return EditorSceneManager.OpenScene(BATTLE_SCENE, OpenSceneMode.Single);
    }

    static GameObject FindRootCanvas(Scene scene)
    {
        foreach (var r in scene.GetRootGameObjects())
            if (r != null && r.GetComponent<Canvas>() != null) return r;
        return null;
    }
}
