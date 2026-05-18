using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class BattleUISetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Battle Info Dialog")]
    public static void SetupBattleInfoDialog()
    {
        // Kiem tra xem dang o BattleScene chua
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.name != "BattleScene")
        {
            if (EditorUtility.DisplayDialog("Chuyen Scene", "Ban dang khong o BattleScene. Ban co muon mo BattleScene bay gio khong?", "Co", "Khong"))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
            }
            else
            {
                return;
            }
        }

        // Tim Canvas cua BattleUI
        BattleUI battleUI = Object.FindFirstObjectByType<BattleUI>();
        if (battleUI == null)
        {
            EditorUtility.DisplayDialog("Loi", "Khong tim thay BattleUI trong Scene! Kiem tra lai BattleScene.", "OK");
            return;
        }

        Canvas canvas = battleUI.GetComponent<Canvas>();
        if (canvas == null) canvas = battleUI.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Loi", "Khong tim thay Canvas cua BattleUI!", "OK");
            return;
        }

        // Kiem tra neu da ton tai Panel
        BattleInfoDialogUI existingDialog = Object.FindFirstObjectByType<BattleInfoDialogUI>();
        if (existingDialog != null)
        {
            EditorUtility.DisplayDialog("Thong bao", "BattleInfoDialogUI da ton tai trong Scene roi!", "OK");
            return;
        }

        // Tao Panel chinh
        GameObject panelObj = new GameObject("BattleInfoDialogPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(BattleInfoDialogUI));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(20, 20);
        panelRect.sizeDelta = new Vector2(350, 0); // Height tu dong do ContentSizeFitter

        // Config Image (Nen den mo)
        Image bg = panelObj.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        // Config VerticalLayoutGroup
        VerticalLayoutGroup layout = panelObj.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 5;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        // Config ContentSizeFitter de panel tu gian theo noi dung
        ContentSizeFitter fitter = panelObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Tao 3 Text con
        TextMeshProUGUI buffText = CreateTextChild(panelObj.transform, "BuffText", "Buffs: None", new Color(0.2f, 1f, 0.2f));
        TextMeshProUGUI debuffText = CreateTextChild(panelObj.transform, "DebuffText", "Debuffs: None", new Color(1f, 0.2f, 0.2f));
        TextMeshProUGUI comboText = CreateTextChild(panelObj.transform, "EnemyComboText", "Sắp tới: ???", new Color(1f, 0.6f, 0.2f));

        // Gan reference vao script
        BattleInfoDialogUI dialogScript = panelObj.GetComponent<BattleInfoDialogUI>();
        
        // Dung SerializedObject de gan gia tri cho private SerializeField
        SerializedObject so = new SerializedObject(dialogScript);
        so.FindProperty("buffText").objectReferenceValue = buffText;
        so.FindProperty("debuffText").objectReferenceValue = debuffText;
        so.FindProperty("enemyComboText").objectReferenceValue = comboText;
        so.ApplyModifiedProperties();

        // Danh dau Scene thay doi
        EditorUtility.SetDirty(panelObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Thanh cong", "Da tao va thiet lap BattleInfoDialogUI thanh cong o goc trai duoi man hinh!", "OK");
    }

    private static TextMeshProUGUI CreateTextChild(Transform parent, string name, string defaultText, Color color)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
        txt.text = defaultText;
        txt.fontSize = 22;
        txt.color = Color.white; // Mac dinh chu trang, dung rich text cho mau sau
        txt.alignment = TextAlignmentOptions.Left;
        txt.enableWordWrapping = true;
        
        // Cố gắng tìm font mặc định nếu có thể
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null)
        {
            txt.font = defaultFont;
        }

        return txt;
    }
}
