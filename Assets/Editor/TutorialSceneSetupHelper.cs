using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Copy BattleScene → Chapter1_Tutorial, cấu hình BattleManager cho tutorial,
/// rồi thêm TutorialController + TutorialPromptUI.
/// Menu: Tools/Tutorial/Setup Tutorial Scene
/// </summary>
public class TutorialSceneSetupHelper : EditorWindow
{
    const string BATTLE_SCENE   = "Assets/Scenes/BattleScene.unity";
    const string TUTORIAL_SCENE = "Assets/Scenes/Chapter1_Tutorial.unity";
    const string PLAYER_DATA    = "Assets/Data/Characters/Data_TranQuocTuan.asset";
    const string ENEMY_ATTACK   = "Assets/Data/Tutorial/TutorialEnemyAttack.asset";
    const string ENEMY_PREFAB   = "Assets/Prefabs/BinhLinh_Combat.prefab";
    const string FONT_PATH      = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/Tutorial/Setup Tutorial Scene")]
    static void Open()
    {
        var w = GetWindow<TutorialSceneSetupHelper>("Setup Tutorial");
        w.minSize = new Vector2(440, 260);
        w.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Tutorial Scene Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tool này sẽ:\n" +
            "1. Copy nguyên bản BattleScene → Chapter1_Tutorial (xóa bản cũ).\n" +
            "2. Gán BattleManager: BinhLinh_Combat, TutorialEnemyAttack, TranQuocTuan data.\n" +
            "3. Xóa BattleSceneBootstrap.debugMapData (buộc dùng demo mode).\n" +
            "4. Thêm Tutorial root object với TutorialController.\n" +
            "5. Thêm TutorialPromptPanel vào canvas, wire TutorialPromptUI.",
            MessageType.Info);

        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(0.3f, 0.75f, 0.3f);
        if (GUILayout.Button("Bắt đầu Setup", GUILayout.Height(44)))
            Run();
        GUI.backgroundColor = Color.white;
    }

    void Run()
    {
        // ── Guard ────────────────────────────────────────────────────────────
        if (!File.Exists(BATTLE_SCENE))
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy:\n{BATTLE_SCENE}", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // ── 1. Copy BattleScene → Tutorial ───────────────────────────────────
        File.Copy(BATTLE_SCENE, TUTORIAL_SCENE, overwrite: true);
        AssetDatabase.Refresh();
        Log("Copy BattleScene → Chapter1_Tutorial.");

        var scene = EditorSceneManager.OpenScene(TUTORIAL_SCENE, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Lỗi", "Không thể mở scene sau khi copy.", "OK");
            return;
        }

        // ── 2. Cấu hình BattleManager ─────────────────────────────────────────
        var bm = Object.FindFirstObjectByType<BattleManager>();
        if (bm == null) { Error("Không tìm thấy BattleManager!"); return; }

        var bmSO = new SerializedObject(bm);
        bmSO.FindProperty("useDemoModeIfMissingManager").boolValue = true;

        SetRef(bmSO, "debugPlayerData",  AssetDatabase.LoadAssetAtPath<PlayerData>(PLAYER_DATA),
               "debugPlayerData", PLAYER_DATA);
        SetRef(bmSO, "debugEnemyAttack", AssetDatabase.LoadAssetAtPath<EnemyAttackData>(ENEMY_ATTACK),
               "debugEnemyAttack", ENEMY_ATTACK);
        SetRef(bmSO, "debugEnemyPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB),
               "debugEnemyPrefab", ENEMY_PREFAB);

        bmSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bm);
        Log("BattleManager đã được cấu hình.");

        // ── 3. BattleSceneBootstrap: xóa debugMapData ────────────────────────
        var bootstrap = Object.FindFirstObjectByType<BattleSceneBootstrap>();
        if (bootstrap != null)
        {
            var bsSO = new SerializedObject(bootstrap);
            bsSO.FindProperty("debugMapData").objectReferenceValue = null;
            bsSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);
            Log("BattleSceneBootstrap.debugMapData = null (dùng demo mode).");
        }

        // ── 4. Tìm BattleUI_Canvas ────────────────────────────────────────────
        Canvas uiCanvas = null;
        var battleUI = Object.FindFirstObjectByType<BattleUI>();
        if (battleUI != null)
            uiCanvas = battleUI.GetComponent<Canvas>() ?? battleUI.GetComponentInParent<Canvas>();
        if (uiCanvas == null)
            uiCanvas = Object.FindFirstObjectByType<Canvas>();
        if (uiCanvas == null) { Error("Không tìm thấy Canvas!"); return; }

        // ── 5. Xóa Tutorial cũ nếu có ─────────────────────────────────────────
        var oldCtrl = GameObject.Find("Tutorial");
        if (oldCtrl != null) { Object.DestroyImmediate(oldCtrl); Log("Xóa Tutorial cũ."); }

        var oldPanel = uiCanvas.transform.Find("TutorialPromptPanel");
        if (oldPanel != null) { Object.DestroyImmediate(oldPanel.gameObject); Log("Xóa TutorialPromptPanel cũ."); }

        // ── 6. Tạo TutorialPromptPanel ────────────────────────────────────────
        var panel = new GameObject("TutorialPromptPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(uiCanvas.transform, false);

        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 0f);
        panelRT.anchorMax        = new Vector2(0.5f, 0f);
        panelRT.pivot            = new Vector2(0.5f, 0f);
        panelRT.sizeDelta        = new Vector2(720f, 90f);
        panelRT.anchoredPosition = new Vector2(0f, 160f);

        var panelImg = panel.GetComponent<Image>();
        panelImg.color = new Color(0.06f, 0.06f, 0.10f, 0.90f);

        // Text
        var textGO = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(panel.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(16f, 8f);
        textRT.offsetMax = new Vector2(-16f, -8f);

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.fontSize  = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.enableWordWrapping = true;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font != null) tmp.font = font;
        else Debug.LogWarning("[TutorialSetup] Không tìm thấy font tại " + FONT_PATH);

        // TutorialPromptUI component
        var promptUI = panel.AddComponent<TutorialPromptUI>();
        var pSO = new SerializedObject(promptUI);
        pSO.FindProperty("promptText").objectReferenceValue      = tmp;
        pSO.FindProperty("panelObject").objectReferenceValue     = panel;
        pSO.FindProperty("panelBackground").objectReferenceValue = panelImg;
        pSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(promptUI);
        Log("TutorialPromptPanel đã tạo và wire.");

        // ── 7. Tạo Tutorial root object + TutorialController ──────────────────
        var tutGO = new GameObject("Tutorial");
        var ctrl  = tutGO.AddComponent<TutorialController>();
        var ctrlSO = new SerializedObject(ctrl);
        ctrlSO.FindProperty("promptUI").objectReferenceValue = promptUI;
        ctrlSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ctrl);
        Log("TutorialController đã tạo và wire.");

        // ── 8. Save ───────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Log("Scene đã lưu.");

        EditorUtility.DisplayDialog("Hoàn tất",
            "Chapter1_Tutorial đã được setup từ BattleScene!\n\n" +
            "✓ BattleManager: BinhLinh + TutorialEnemyAttack + TranQuocTuan\n" +
            "✓ TutorialController + TutorialPromptUI đã wire\n" +
            "✓ Demo mode bật (không cần MapManager)",
            "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void SetRef(SerializedObject so, string prop, Object value, string label, string path)
    {
        if (value != null)
        {
            so.FindProperty(prop).objectReferenceValue = value;
            Log($"Gán {label} = {value.name}");
        }
        else
            Debug.LogWarning($"[TutorialSetup] Không tìm thấy asset tại {path}");
    }

    static void Log(string msg)   => Debug.Log($"[TutorialSetup] {msg}");
    static void Error(string msg) => EditorUtility.DisplayDialog("Lỗi", msg, "OK");
}
