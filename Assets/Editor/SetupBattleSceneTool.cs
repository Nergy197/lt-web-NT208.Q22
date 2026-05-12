using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Editor tool tu dong dung toan bo Canvas hierarchy cho BattleScene
/// va wire vao BattleUI component.
///
/// Menu: Tools/Battle UI/Setup Battle Scene (Auto-Build)
/// </summary>
public class SetupBattleSceneTool : EditorWindow
{
    private const string ROOT_CANVAS_NAME = "BattleUI_Canvas";

    private bool overwriteExisting = true;
    private bool createSpawnAnchors = true;

    [MenuItem("Tools/Battle UI/Setup Battle Scene (Auto-Build)")]
    public static void OpenWindow()
    {
        var w = GetWindow<SetupBattleSceneTool>("Setup Battle Scene");
        w.minSize = new Vector2(400, 260);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Setup BattleScene UI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tool nay se:\n" +
            "1. Tao Canvas + EventSystem (neu chua co)\n" +
            "2. Dung 2 Player HUD + 3 Enemy HUD (UnitHUD components)\n" +
            "3. Tao Turn Indicator, Action Menu, Skill Menu, Battle Log, Result Panel\n" +
            "4. Tu dong wire BattleUI Inspector\n" +
            "5. Tao spawn anchors va gan vao BattleManager (neu chon)",
            MessageType.Info);

        EditorGUILayout.Space(8);
        overwriteExisting = EditorGUILayout.Toggle("Xoa Canvas cu va tao lai", overwriteExisting);
        createSpawnAnchors = EditorGUILayout.Toggle("Tao Spawn Anchors va wire BattleManager", createSpawnAnchors);

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Mo BattleScene truoc khi chay tool nay.",
            MessageType.Warning);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Dung Canvas & Wire BattleUI", GUILayout.Height(40)))
            Run();
    }

    private void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Loi", "Khong co scene nao dang mo!", "OK");
            return;
        }

        // 1. Xoa Canvas cu neu duoc yeu cau
        if (overwriteExisting)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == ROOT_CANVAS_NAME)
                {
                    Undo.DestroyObjectImmediate(root);
                    Debug.Log($"[SetupBattle] Removed existing '{ROOT_CANVAS_NAME}'");
                }
            }
        }

        // 2. Tao EventSystem neu chua co
        EnsureEventSystem();

        // 3. Tao root Canvas
        GameObject canvasGO = CreateCanvas();

        // 4. Dung hierarchy
        var playerHUDs = BuildPlayerHUDs(canvasGO.transform);
        var enemyHUDs = BuildEnemyHUDs(canvasGO.transform);
        var turnIndicator = BuildTurnIndicator(canvasGO.transform);
        var actionMenu = BuildActionMenu(canvasGO.transform);
        var skillMenu = BuildSkillMenu(canvasGO.transform);
        var battleLog = BuildBattleLog(canvasGO.transform);
        var resultPanel = BuildResultPanel(canvasGO.transform);

        // 5. Add BattleUI component & wire
        var battleUI = canvasGO.GetComponent<BattleUI>();
        if (battleUI == null) battleUI = canvasGO.AddComponent<BattleUI>();

        WireBattleUI(battleUI, playerHUDs, enemyHUDs, turnIndicator, actionMenu, skillMenu, battleLog, resultPanel);

        // 6. Tao BattleDebugUI (optional — tao ngay tren battleLog text)
        EnsureBattleDebugUI(canvasGO, battleLog.logText);

        // 7. Tao spawn anchors va wire vao BattleManager neu duoc yeu cau
        if (createSpawnAnchors)
            EnsureSpawnAnchorsAndBattleManager();

        // 8. Mark dirty + save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Hoan Tat",
            "BattleScene UI da duoc dung va wire xong!\n\n" +
            "- 2 Player HUD (top-left)\n" +
            "- 3 Enemy HUD (top-right)\n" +
            "- Turn Indicator (top-center)\n" +
            "- Action Menu + Skill Menu (bottom)\n" +
            "- Battle Log (bottom-right)\n" +
            "- Result Panel (center)\n\n" +
            "Mo BattleUI Inspector de kiem tra cac slot.",
            "OK");
    }

    // ============================ INFRA ============================

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        Debug.Log("[SetupBattle] Created EventSystem");
    }

    private static GameObject CreateCanvas()
    {
        GameObject go = new GameObject(ROOT_CANVAS_NAME);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(go, "Create BattleUI Canvas");
        return go;
    }

    // ============================ PLAYER HUDS ============================

    private static List<UnitHUD> BuildPlayerHUDs(Transform canvasRoot)
    {
        // Container top-left
        var container = CreatePanel("PlayerHUDs_Container", canvasRoot);
        var rt = container.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -20);
        rt.sizeDelta = new Vector2(380, 240);
        container.GetComponent<Image>().color = new Color(0, 0, 0, 0); // transparent container

        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var huds = new List<UnitHUD>();
        for (int i = 0; i < 2; i++)
        {
            var hud = CreatePlayerHUD($"PlayerHUD_{i}", container.transform);
            huds.Add(hud);
        }
        return huds;
    }

    private static UnitHUD CreatePlayerHUD(string name, Transform parent)
    {
        GameObject root = CreatePanel(name, parent);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380, 110);

        var le = root.AddComponent<LayoutElement>();
        le.preferredHeight = 110;

        var bg = root.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.2f, 0.4f, 0.7f);

        // Highlight border (Image component as outline, initially transparent)
        var highlight = CreateImage("Highlight", root.transform);
        var hrt = highlight.GetComponent<RectTransform>();
        hrt.anchorMin = Vector2.zero;
        hrt.anchorMax = Vector2.one;
        hrt.offsetMin = new Vector2(-3, -3);
        hrt.offsetMax = new Vector2(3, 3);
        var himg = highlight.GetComponent<Image>();
        himg.color = new Color(1f, 0.85f, 0.2f, 0f); // hidden initially
        highlight.transform.SetAsFirstSibling();

        // Name text
        var nameText = CreateText("NameText", root.transform, "Player Name", 22, TextAlignmentOptions.Left);
        var nrt = nameText.rectTransform;
        nrt.anchorMin = new Vector2(0, 1);
        nrt.anchorMax = new Vector2(1, 1);
        nrt.pivot = new Vector2(0, 1);
        nrt.anchoredPosition = new Vector2(10, -5);
        nrt.sizeDelta = new Vector2(-20, 30);
        nameText.color = Color.white;

        // HP slider
        var hpSlider = CreateSlider("HPSlider", root.transform, new Color(0.85f, 0.15f, 0.15f));
        var hrt2 = hpSlider.GetComponent<RectTransform>();
        hrt2.anchorMin = new Vector2(0, 1);
        hrt2.anchorMax = new Vector2(1, 1);
        hrt2.pivot = new Vector2(0, 1);
        hrt2.anchoredPosition = new Vector2(10, -38);
        hrt2.sizeDelta = new Vector2(-20, 18);

        var hpText = CreateText("HPText", root.transform, "100/100", 14, TextAlignmentOptions.Center);
        var hpTextRt = hpText.rectTransform;
        hpTextRt.anchorMin = new Vector2(0, 1);
        hpTextRt.anchorMax = new Vector2(1, 1);
        hpTextRt.pivot = new Vector2(0.5f, 1);
        hpTextRt.anchoredPosition = new Vector2(0, -38);
        hpTextRt.sizeDelta = new Vector2(0, 18);
        hpText.color = Color.white;

        // AP slider
        var apSlider = CreateSlider("APSlider", root.transform, new Color(0.2f, 0.6f, 1f));
        var aprt = apSlider.GetComponent<RectTransform>();
        aprt.anchorMin = new Vector2(0, 1);
        aprt.anchorMax = new Vector2(1, 1);
        aprt.pivot = new Vector2(0, 1);
        aprt.anchoredPosition = new Vector2(10, -62);
        aprt.sizeDelta = new Vector2(-20, 14);

        var apText = CreateText("APText", root.transform, "AP: 100/100", 12, TextAlignmentOptions.Center);
        var apTextRt = apText.rectTransform;
        apTextRt.anchorMin = new Vector2(0, 1);
        apTextRt.anchorMax = new Vector2(1, 1);
        apTextRt.pivot = new Vector2(0.5f, 1);
        apTextRt.anchoredPosition = new Vector2(0, -62);
        apTextRt.sizeDelta = new Vector2(0, 14);
        apText.color = Color.white;

        // UnitHUD component + wire
        var hud = root.AddComponent<UnitHUD>();
        var so = new SerializedObject(hud);
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("hpSlider").objectReferenceValue = hpSlider;
        so.FindProperty("hpText").objectReferenceValue = hpText;
        so.FindProperty("apSlider").objectReferenceValue = apSlider;
        so.FindProperty("apText").objectReferenceValue = apText;

        var bgProp = so.FindProperty("backgroundImage");
        if (bgProp != null) bgProp.objectReferenceValue = bg;

        var hlProp = so.FindProperty("highlightImage");
        if (hlProp != null) hlProp.objectReferenceValue = himg;

        so.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    // ============================ ENEMY HUDS ============================

    private static List<UnitHUD> BuildEnemyHUDs(Transform canvasRoot)
    {
        var container = CreatePanel("EnemyHUDs_Container", canvasRoot);
        var rt = container.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta = new Vector2(360, 270);
        container.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childAlignment = TextAnchor.UpperRight;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var huds = new List<UnitHUD>();
        for (int i = 0; i < 3; i++)
        {
            var hud = CreateEnemyHUD($"EnemyHUD_{i}", container.transform);
            huds.Add(hud);
        }
        return huds;
    }

    private static UnitHUD CreateEnemyHUD(string name, Transform parent)
    {
        GameObject root = CreatePanel(name, parent);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 80);

        var le = root.AddComponent<LayoutElement>();
        le.preferredHeight = 80;

        var bg = root.GetComponent<Image>();
        bg.color = new Color(0.4f, 0.1f, 0.1f, 0.7f);

        var nameText = CreateText("NameText", root.transform, "Enemy Name", 18, TextAlignmentOptions.Left);
        var nrt = nameText.rectTransform;
        nrt.anchorMin = new Vector2(0, 1);
        nrt.anchorMax = new Vector2(1, 1);
        nrt.pivot = new Vector2(0, 1);
        nrt.anchoredPosition = new Vector2(10, -5);
        nrt.sizeDelta = new Vector2(-20, 26);
        nameText.color = Color.white;

        var hpSlider = CreateSlider("HPSlider", root.transform, new Color(0.85f, 0.15f, 0.15f));
        var hpRt = hpSlider.GetComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(0, 1);
        hpRt.anchorMax = new Vector2(1, 1);
        hpRt.pivot = new Vector2(0, 1);
        hpRt.anchoredPosition = new Vector2(10, -36);
        hpRt.sizeDelta = new Vector2(-20, 18);

        var hpText = CreateText("HPText", root.transform, "100/100", 14, TextAlignmentOptions.Center);
        var hpTextRt = hpText.rectTransform;
        hpTextRt.anchorMin = new Vector2(0, 1);
        hpTextRt.anchorMax = new Vector2(1, 1);
        hpTextRt.pivot = new Vector2(0.5f, 1);
        hpTextRt.anchoredPosition = new Vector2(0, -36);
        hpTextRt.sizeDelta = new Vector2(0, 18);
        hpText.color = Color.white;

        var hud = root.AddComponent<UnitHUD>();
        var so = new SerializedObject(hud);
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("hpSlider").objectReferenceValue = hpSlider;
        so.FindProperty("hpText").objectReferenceValue = hpText;

        var bgProp = so.FindProperty("backgroundImage");
        if (bgProp != null) bgProp.objectReferenceValue = bg;

        so.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    // ============================ TURN INDICATOR ============================

    private static TextMeshProUGUI BuildTurnIndicator(Transform canvasRoot)
    {
        var go = CreatePanel("TurnIndicator", canvasRoot);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -20);
        rt.sizeDelta = new Vector2(420, 60);
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);

        var text = CreateText("Label", go.transform, "Luot: --", 26, TextAlignmentOptions.Center);
        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        text.color = new Color(1f, 0.95f, 0.6f);
        return text;
    }

    // ============================ ACTION MENU ============================

    private class ActionMenuRefs
    {
        public GameObject panel;
        public Button btnAttack, btnSkill, btnFlee, btnParry;
    }

    private static ActionMenuRefs BuildActionMenu(Transform canvasRoot)
    {
        var go = CreatePanel("ActionMenu", canvasRoot);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 30);
        rt.sizeDelta = new Vector2(640, 90);
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.padding = new RectOffset(15, 15, 12, 12);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;

        var refs = new ActionMenuRefs { panel = go };
        refs.btnAttack = CreateButton("Btn_Attack", go.transform, "Tan Cong");
        refs.btnSkill  = CreateButton("Btn_Skill",  go.transform, "Ky Nang");
        refs.btnParry  = CreateButton("Btn_Parry",  go.transform, "Don Do");
        refs.btnFlee   = CreateButton("Btn_Flee",   go.transform, "Bo Chay");
        return refs;
    }

    // ============================ SKILL MENU ============================

    private class SkillMenuRefs
    {
        public GameObject panel;
        public Button btnBack;
        public List<Button> skillButtons = new List<Button>();
        public List<TextMeshProUGUI> skillLabels = new List<TextMeshProUGUI>();
    }

    private static SkillMenuRefs BuildSkillMenu(Transform canvasRoot)
    {
        var go = CreatePanel("SkillMenu", canvasRoot);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 30);
        rt.sizeDelta = new Vector2(640, 90);
        go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.15f, 0.7f);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.padding = new RectOffset(15, 15, 12, 12);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;

        var refs = new SkillMenuRefs { panel = go };

        for (int i = 0; i < 3; i++)
        {
            var btn = CreateButton($"Btn_Skill{i + 1}", go.transform, $"Skill {i + 1}");
            refs.skillButtons.Add(btn);

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            refs.skillLabels.Add(label);
        }

        refs.btnBack = CreateButton("Btn_Back", go.transform, "Tro Lai");
        // Make Back button slightly narrower so skill buttons get more space
        var beLayout = refs.btnBack.gameObject.AddComponent<LayoutElement>();
        beLayout.flexibleWidth = 0.5f;

        // start hidden
        go.SetActive(false);
        return refs;
    }

    // ============================ BATTLE LOG ============================

    private class BattleLogRefs
    {
        public GameObject panel;
        public TextMeshProUGUI logText;
        public TextMeshProUGUI playerEffectsText;
        public TextMeshProUGUI enemyEffectsText;
    }

    private static BattleLogRefs BuildBattleLog(Transform canvasRoot)
    {
        var refs = new BattleLogRefs();

        var go = CreatePanel("BattleLog", canvasRoot);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-20, 140);
        rt.sizeDelta = new Vector2(420, 220);
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);

        refs.panel = go;

        var logText = CreateText("LogText", go.transform, "", 14, TextAlignmentOptions.BottomLeft);
        var lrt = logText.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(8, 8);
        lrt.offsetMax = new Vector2(-8, -8);
        logText.enableWordWrapping = true;
        logText.color = new Color(0.95f, 0.95f, 0.95f);
        refs.logText = logText;

        // Status effects panel (left of battle log)
        var effectsGO = CreatePanel("StatusEffects", canvasRoot);
        var ert = effectsGO.GetComponent<RectTransform>();
        ert.anchorMin = new Vector2(0, 0);
        ert.anchorMax = new Vector2(0, 0);
        ert.pivot = new Vector2(0, 0);
        ert.anchoredPosition = new Vector2(20, 140);
        ert.sizeDelta = new Vector2(400, 220);
        effectsGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.4f);

        var pTitle = CreateText("PlayerEffectsTitle", effectsGO.transform, "Hieu ung Player:", 14, TextAlignmentOptions.TopLeft);
        var ptr = pTitle.rectTransform;
        ptr.anchorMin = new Vector2(0, 1);
        ptr.anchorMax = new Vector2(1, 1);
        ptr.pivot = new Vector2(0, 1);
        ptr.anchoredPosition = new Vector2(8, -6);
        ptr.sizeDelta = new Vector2(-16, 20);
        pTitle.color = new Color(0.6f, 0.85f, 1f);

        var pEffects = CreateText("PlayerEffectsText", effectsGO.transform, "", 13, TextAlignmentOptions.TopLeft);
        var prt = pEffects.rectTransform;
        prt.anchorMin = new Vector2(0, 0);
        prt.anchorMax = new Vector2(1, 1);
        prt.offsetMin = new Vector2(8, 8);
        prt.offsetMax = new Vector2(-8, -28);
        pEffects.color = Color.white;
        refs.playerEffectsText = pEffects;

        return refs;
    }

    // ============================ RESULT PANEL ============================

    private class ResultPanelRefs
    {
        public GameObject panel;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI expText;
    }

    private static ResultPanelRefs BuildResultPanel(Transform canvasRoot)
    {
        var go = CreatePanel("ResultPanel", canvasRoot);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600, 320);
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);

        var resultText = CreateText("ResultText", go.transform, "THANG!", 56, TextAlignmentOptions.Center);
        var rrt = resultText.rectTransform;
        rrt.anchorMin = new Vector2(0, 0.55f);
        rrt.anchorMax = new Vector2(1, 1);
        rrt.offsetMin = Vector2.zero;
        rrt.offsetMax = Vector2.zero;
        resultText.color = new Color(1f, 0.85f, 0.3f);

        var expText = CreateText("ExpText", go.transform, "", 26, TextAlignmentOptions.Center);
        var ert = expText.rectTransform;
        ert.anchorMin = new Vector2(0, 0);
        ert.anchorMax = new Vector2(1, 0.55f);
        ert.offsetMin = new Vector2(20, 20);
        ert.offsetMax = new Vector2(-20, -20);
        expText.color = Color.white;

        go.SetActive(false);
        return new ResultPanelRefs { panel = go, resultText = resultText, expText = expText };
    }

    // ============================ WIRING ============================

    private static void WireBattleUI(
        BattleUI battleUI,
        List<UnitHUD> playerHUDs,
        List<UnitHUD> enemyHUDs,
        TextMeshProUGUI turnIndicator,
        ActionMenuRefs action,
        SkillMenuRefs skill,
        BattleLogRefs log,
        ResultPanelRefs result)
    {
        var so = new SerializedObject(battleUI);

        // Player HUDs
        var playerHUDsProp = so.FindProperty("playerHUDs");
        playerHUDsProp.arraySize = playerHUDs.Count;
        for (int i = 0; i < playerHUDs.Count; i++)
            playerHUDsProp.GetArrayElementAtIndex(i).objectReferenceValue = playerHUDs[i];

        // Enemy HUDs
        var enemyHUDsProp = so.FindProperty("enemyHUDs");
        enemyHUDsProp.arraySize = enemyHUDs.Count;
        for (int i = 0; i < enemyHUDs.Count; i++)
            enemyHUDsProp.GetArrayElementAtIndex(i).objectReferenceValue = enemyHUDs[i];

        // Action menu
        SetRef(so, "actionMenuPanel", action.panel);
        SetRef(so, "btnAttack", action.btnAttack);
        SetRef(so, "btnSkill",  action.btnSkill);
        SetRef(so, "btnFlee",   action.btnFlee);
        SetRef(so, "btnParry",  action.btnParry);

        // Skill menu
        SetRef(so, "skillMenuPanel", skill.panel);
        SetRef(so, "btnSkillBack",   skill.btnBack);

        var skillBtnsProp = so.FindProperty("skillButtons");
        skillBtnsProp.arraySize = skill.skillButtons.Count;
        for (int i = 0; i < skill.skillButtons.Count; i++)
            skillBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = skill.skillButtons[i];

        var skillLabelsProp = so.FindProperty("skillLabels");
        skillLabelsProp.arraySize = skill.skillLabels.Count;
        for (int i = 0; i < skill.skillLabels.Count; i++)
            skillLabelsProp.GetArrayElementAtIndex(i).objectReferenceValue = skill.skillLabels[i];

        // Battle log
        SetRef(so, "battleLogText",       log.logText);
        SetRef(so, "playerEffectsText",   log.playerEffectsText);
        SetRef(so, "enemyEffectsText",    log.enemyEffectsText);

        // Turn indicator
        SetRef(so, "turnOrderPanel",      turnIndicator.transform.parent.gameObject);
        SetRef(so, "turnOrderText",       turnIndicator);
        SetRef(so, "turnIndicatorText",   turnIndicator);

        // Result
        SetRef(so, "resultPanel", result.panel);
        SetRef(so, "resultText",  result.resultText);
        SetRef(so, "expText",     result.expText);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(battleUI);
        Debug.Log("[SetupBattle] BattleUI wired successfully.");
    }

    private static void SetRef(SerializedObject so, string propName, Object value)
    {
        var p = so.FindProperty(propName);
        if (p != null) p.objectReferenceValue = value;
    }

    // ============================ DEBUG UI ============================

    private static void EnsureBattleDebugUI(GameObject canvasGO, TextMeshProUGUI logText)
    {
        var existing = Object.FindFirstObjectByType<BattleDebugUI>();
        if (existing != null) return;

        var go = new GameObject("BattleDebugUI");
        go.transform.SetParent(canvasGO.transform, false);
        var debug = go.AddComponent<BattleDebugUI>();
        var so = new SerializedObject(debug);
        so.FindProperty("debugText").objectReferenceValue = logText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ============================ SPAWN ANCHORS ============================

    private static void EnsureSpawnAnchorsAndBattleManager()
    {
        var bm = Object.FindFirstObjectByType<BattleManager>();
        if (bm == null)
        {
            var bmGO = new GameObject("BattleManager");
            bm = bmGO.AddComponent<BattleManager>();
            Undo.RegisterCreatedObjectUndo(bmGO, "Create BattleManager");
            Debug.Log("[SetupBattle] Created BattleManager");
        }

        var so = new SerializedObject(bm);
        var pAnchor = so.FindProperty("playerSpawnAnchor");
        var eAnchor = so.FindProperty("enemySpawnAnchor");

        if (pAnchor.objectReferenceValue == null)
        {
            var t = GameObject.Find("PlayerSpawnAnchor")?.transform ?? CreateAnchor("PlayerSpawnAnchor", new Vector3(-4f, 0f, 0f));
            pAnchor.objectReferenceValue = t;
        }
        if (eAnchor.objectReferenceValue == null)
        {
            var t = GameObject.Find("EnemySpawnAnchor")?.transform ?? CreateAnchor("EnemySpawnAnchor", new Vector3(4f, 0f, 0f));
            eAnchor.objectReferenceValue = t;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bm);
    }

    private static Transform CreateAnchor(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        Undo.RegisterCreatedObjectUndo(go, "Create Spawn Anchor");
        return go.transform;
    }

    // ============================ PRIMITIVES ============================

    private static GameObject CreatePanel(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.5f);
        return go;
    }

    private static GameObject CreateImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>();
        return go;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string content, int size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        return t;
    }

    private static Slider CreateSlider(string name, Transform parent, Color fillColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var slider = go.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        // Background
        var bg = CreateImage("Background", go.transform);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        // Fill area + Fill
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0);
        faRt.anchorMax = new Vector2(1, 1);
        faRt.offsetMin = new Vector2(2, 2);
        faRt.offsetMax = new Vector2(-2, -2);

        var fill = CreateImage("Fill", fillArea.transform);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.color = fillColor;

        slider.fillRect = fillRt;
        slider.targetGraphic = fillImg;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.25f, 0.4f, 0.95f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(1f, 0.95f, 0.5f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        btn.colors = colors;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var t = labelGO.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 22;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        var rt = labelGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return btn;
    }
}
