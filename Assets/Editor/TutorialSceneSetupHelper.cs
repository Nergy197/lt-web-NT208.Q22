using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialSceneSetupHelper : EditorWindow
{
    private const string BATTLE_SCENE_PATH = "Assets/Scenes/BattleScene.unity";
    private const string TUTORIAL_SCENE_PATH = "Assets/Scenes/Chapter1_Tutorial.unity";
    private const string PLAYER_DATA_PATH = "Assets/Data/Characters/Data_TranQuocTuan.asset";
    private const string ENEMY_ATTACK_PATH = "Assets/Data/Tutorial/TutorialEnemyAttack.asset";

    [MenuItem("Tools/Tutorial/Setup Tutorial Scene")]
    public static void Open()
    {
        var window = GetWindow<TutorialSceneSetupHelper>("Setup Tutorial");
        window.minSize = new Vector2(400, 200);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Tutorial Scene Automator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Cong cu nay se tu dong thuc hien:\n" +
            "1. Copy nguyen ban BattleScene.unity sang Chapter1_Tutorial.unity.\n" +
            "2. Mo scene huong dan moi tao.\n" +
            "3. Cau hinh BattleManager dung TutorialPlayerData va TutorialEnemyAttack o che do Demo.\n" +
            "4. Them component TutorialController va TutorialPromptUI.\n" +
            "5. Tu dong thiet lap Canvas UI cho prompt huong dan.",
            MessageType.Info);

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("Bat dau Setup", GUILayout.Height(40)))
        {
            SetupTutorial();
        }
        GUI.backgroundColor = Color.white;
    }

    private void SetupTutorial()
    {
        if (!File.Exists(BATTLE_SCENE_PATH))
        {
            EditorUtility.DisplayDialog("Loi", "Khong tim thay file BattleScene.unity goc tai " + BATTLE_SCENE_PATH, "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        File.Copy(BATTLE_SCENE_PATH, TUTORIAL_SCENE_PATH, true);
        AssetDatabase.Refresh();
        Debug.Log("[TUTORIAL SETUP] Da copy BattleScene sang Chapter1_Tutorial.");

        var scene = EditorSceneManager.OpenScene(TUTORIAL_SCENE_PATH, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Loi", "Khong the mo scene " + TUTORIAL_SCENE_PATH, "OK");
            return;
        }

        var bm = Object.FindFirstObjectByType<BattleManager>();
        if (bm == null)
        {
            EditorUtility.DisplayDialog("Loi", "Khong tim thay BattleManager trong scene!", "OK");
            return;
        }

        var bmSO = new SerializedObject(bm);
        bmSO.FindProperty("useDemoModeIfMissingManager").boolValue = true;

        var playerData = AssetDatabase.LoadAssetAtPath<PlayerData>(PLAYER_DATA_PATH);
        var enemyAttack = AssetDatabase.LoadAssetAtPath<EnemyAttackData>(ENEMY_ATTACK_PATH);

        if (playerData != null)
        {
            bmSO.FindProperty("debugPlayerData").objectReferenceValue = playerData;
            Debug.Log("[TUTORIAL SETUP] Da gan debugPlayerData = " + playerData.name);
        }
        else
        {
            Debug.LogWarning("[TUTORIAL SETUP] Khong tim thay PlayerData tai " + PLAYER_DATA_PATH);
        }

        if (enemyAttack != null)
        {
            bmSO.FindProperty("debugEnemyAttack").objectReferenceValue = enemyAttack;
            Debug.Log("[TUTORIAL SETUP] Da gan debugEnemyAttack = " + enemyAttack.name);
        }
        else
        {
            Debug.LogWarning("[TUTORIAL SETUP] Khong tim thay EnemyAttack tai " + ENEMY_ATTACK_PATH);
        }

        bmSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bm);

        var canvas = GameObject.Find("BattleUI_Canvas");
        if (canvas == null)
        {
            var canvasComp = Object.FindFirstObjectByType<Canvas>();
            if (canvasComp != null) canvas = canvasComp.gameObject;
        }

        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Loi", "Khong tim thay BattleUI_Canvas trong scene!", "OK");
            return;
        }

        var oldPrompt = canvas.transform.Find("TutorialPromptPanel");
        if (oldPrompt != null)
        {
            DestroyImmediate(oldPrompt.gameObject);
        }

        var oldController = GameObject.Find("Tutorial");
        if (oldController != null)
        {
            DestroyImmediate(oldController);
        }

        var promptPanel = new GameObject("TutorialPromptPanel", typeof(RectTransform), typeof(Image));
        promptPanel.transform.SetParent(canvas.transform, false);
        
        var panelRect = promptPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(700f, 90f);
        panelRect.anchoredPosition = new Vector2(0f, 150f);

        var panelImg = promptPanel.GetComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);

        var textGO = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(promptPanel.transform, false);

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 10f);
        textRect.offsetMax = new Vector2(-10f, -10f);

        var textComp = textGO.GetComponent<TextMeshProUGUI>();
        textComp.fontSize = 24f;
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.color = Color.white;
        textComp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        var promptUI = promptPanel.AddComponent<TutorialPromptUI>();
        var promptSO = new SerializedObject(promptUI);
        promptSO.FindProperty("promptText").objectReferenceValue = textComp;
        promptSO.FindProperty("panelObject").objectReferenceValue = promptPanel;
        promptSO.FindProperty("panelBackground").objectReferenceValue = panelImg;
        promptSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(promptUI);

        var tutorialGO = new GameObject("Tutorial");
        var controller = tutorialGO.AddComponent<TutorialController>();
        var controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("promptUI").objectReferenceValue = promptUI;
        controllerSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[TUTORIAL SETUP] Da setup hoan tat Chapter1_Tutorial scene.");
        EditorUtility.DisplayDialog("Thanh Cong", "Da setup Chapter1_Tutorial hoan tat!\n\n" +
            "- BattleManager da duoc cau hinh dung.\n" +
            "- TutorialController va TutorialPromptUI da duoc tao va wire thanh cong.", "OK");
    }
}
