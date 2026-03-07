using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class CreateSavePointSystem
{
    [MenuItem("Tools/Auto-Create Save Point System (Full)")]
    public static void CreateFullSystem()
    {
        // ----------------- PHẦN 1: TẠO UI MENU -----------------
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MapCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Kiểm tra xem đã có UI chưa, tránh tạo 2 cái đè lên nhau khiến lỗi
        SavePointUI existingUI = Object.FindFirstObjectByType<SavePointUI>();
        if (existingUI == null)
        {
            GameObject systemObj = new GameObject("SavePointMenuSystem");
            systemObj.transform.SetParent(canvas.transform, false);
            SavePointUI logic = systemObj.AddComponent<SavePointUI>();

            GameObject panelObj = new GameObject("SavePointPanel");
            panelObj.transform.SetParent(systemObj.transform, false);
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400, 300);

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "SAVE POINT";
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.yellow;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 100);
            titleRect.sizeDelta = new Vector2(300, 50);

            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "Sẵn sàng...";
            statusText.fontSize = 20;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchoredPosition = new Vector2(0, 40);
            statusRect.sizeDelta = new Vector2(350, 40);

            Button healBtn = CreateButton(panelObj.transform, "Btn_Heal", "Hồi Máu Toàn Đội (H)", new Vector2(0, -20));
            Button saveBtn = CreateButton(panelObj.transform, "Btn_Save", "Lưu Game (F)", new Vector2(0, -70));
            Button closeBtn = CreateButton(panelObj.transform, "Btn_Close", "Đóng (Esc)", new Vector2(0, -120));

            SerializedObject serializedSystem = new SerializedObject(logic);
            serializedSystem.Update();
            // Gắn sẵn các Node cho Script UI
            serializedSystem.FindProperty("panel").objectReferenceValue = panelObj;
            serializedSystem.FindProperty("healButton").objectReferenceValue = healBtn;
            serializedSystem.FindProperty("saveButton").objectReferenceValue = saveBtn;
            serializedSystem.FindProperty("closeButton").objectReferenceValue = closeBtn;
            serializedSystem.FindProperty("statusText").objectReferenceValue = statusText;
            serializedSystem.ApplyModifiedProperties();

            // Nhớ TẮT panel mặc định đi
            panelObj.SetActive(false);
            Debug.Log("Đã tạo UI SavePointMenuSystem thành công.");
        }
        else
        {
            Debug.Log("Phát hiện SavePointMenuSystem đã tồn tại, bỏ qua tạo UI.");
        }

        // ----------------- PHẦN 2: TẠO OBJECT VÀNG TRÊN MAP -----------------
        GameObject saveObj = new GameObject("SavePoint_Auto");

        if (SceneView.lastActiveSceneView != null)
        {
            saveObj.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
            Vector3 pos = saveObj.transform.position;
            pos.z = 0; // Kéo về cùng layer 2D player
            saveObj.transform.position = pos;
        }

        SpriteRenderer sr = saveObj.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.yellow;
        tex.SetPixels(colors);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        BoxCollider2D collider = saveObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.5f, 1.5f);

        SavePoint sp = saveObj.AddComponent<SavePoint>();
        // Auto random ID đuôi 3 số cho khỏi trùng
        sp.pointId = "save_point_" + Random.Range(100, 999);

        Selection.activeGameObject = saveObj;

        Debug.Log("Đã hoàn tất! Hệ thống Save Point sẵn sàng! Lưu Scene lại (Ctrl+S) nhé!");
    }

    private static Button CreateButton(Transform parent, string name, string textStr, Vector2 pos)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image bgImage = btnObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(250, 40);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = textStr;
        tmpText.fontSize = 20;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return btn;
    }
}
