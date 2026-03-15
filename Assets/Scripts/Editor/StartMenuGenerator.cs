using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class StartMenuGenerator
{
    [MenuItem("Tools/S_RPG/Generate Hollow Knight UI")]
    public static void CreateStartMenu()
    {
        // 1. Tạo Canvas
        GameObject canvasObj = new GameObject("StartMenu_HollowKnight");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. BG (Hollow Knight vibe: ngả đen xa hoặc xám tối dải màu gradient, mình dùng đen đục cho đơn giản)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.08f, 1f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // --- TITLE ---
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "<font-weight=400>HIỆP KHÁCH</font>\n<font-weight=900>TRẦN TRIỀU</font>"; // Game Title demo
        titleTxt.fontSize = 75;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = new Color(0.9f, 0.9f, 0.9f);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -100);
        titleRect.sizeDelta = new Vector2(800, 200);

        // --- MAIN MENU PANEL ---
        GameObject mainPanel = new GameObject("MainMenuPanel");
        mainPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainRect.pivot = new Vector2(0.5f, 0.5f);
        mainRect.anchoredPosition = new Vector2(0, -100);
        mainRect.sizeDelta = new Vector2(400, 250);

        VerticalLayoutGroup mainLayout = mainPanel.AddComponent<VerticalLayoutGroup>();
        mainLayout.childAlignment = TextAnchor.MiddleCenter;
        mainLayout.spacing = 15;
        mainLayout.childControlWidth = true;

        Button startBtn = CreateHKButton("StartGameBtn", "START GAME", mainPanel.transform);
        Button optionsBtn = CreateHKButton("OptionsBtn", "OPTIONS", mainPanel.transform); // Nút để đẹp, hiện tại rỗng
        Button quitBtn = CreateHKButton("QuitBtn", "QUIT", mainPanel.transform);

        // --- SAVE SLOTS PANEL ---
        GameObject savePanel = new GameObject("SaveSlotsPanel");
        savePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform saveRect = savePanel.AddComponent<RectTransform>();
        saveRect.anchorMin = new Vector2(0.5f, 0.5f);
        saveRect.anchorMax = new Vector2(0.5f, 0.5f);
        saveRect.pivot = new Vector2(0.5f, 0.5f);
        saveRect.anchoredPosition = new Vector2(0, -60);
        saveRect.sizeDelta = new Vector2(800, 600);

        // Vùng chứa slots
        GameObject slotsContainer = new GameObject("SlotsContainer");
        slotsContainer.transform.SetParent(savePanel.transform, false);
        RectTransform slotsRect = slotsContainer.AddComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0f, 0f); // Stretch
        slotsRect.anchorMax = new Vector2(1f, 1f);
        slotsRect.sizeDelta = new Vector2(0, -100); // Trừ hao lại 100 pixel để đặt nút Back bên dưới
        slotsRect.anchoredPosition = new Vector2(0, 50);

        VerticalLayoutGroup slotsLayout = slotsContainer.AddComponent<VerticalLayoutGroup>();
        slotsLayout.childAlignment = TextAnchor.MiddleCenter;
        slotsLayout.spacing = 20;
        slotsLayout.childControlHeight = false;

        // Nút Back (Nằm dưới cùng bên dưới DS Slot)
        Button backBtn = CreateHKButton("BackBtn", "BACK", savePanel.transform);
        RectTransform backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0);
        backRect.anchorMax = new Vector2(0.5f, 0);
        backRect.pivot = new Vector2(0.5f, 0);
        backRect.anchoredPosition = new Vector2(0, 20);

        // Prefab Slot (Phong cách to bản của Hollow Knight)
        Button slotPrefab = CreateHKSlotPrefab("SlotPrefab", null);
        slotPrefab.gameObject.SetActive(false); // Giấu đi để dùng làm clone
        slotPrefab.transform.SetParent(canvasObj.transform, false);

        // --- GẮN SCRIPT VÀO UI ---
        StartMenuUI uiScript = canvasObj.AddComponent<StartMenuUI>();
        uiScript.mainMenuPanel = mainPanel;
        uiScript.saveSlotsPanel = savePanel; // Link lại cái cũ là loadGamePanel
        uiScript.startGameButton = startBtn;
        uiScript.quitButton = quitBtn;
        uiScript.closeSlotsPanelButton = backBtn;
        uiScript.slotsContainer = slotsContainer.transform;
        uiScript.slotButtonPrefab = slotPrefab.gameObject;

        Selection.activeGameObject = canvasObj;
        Debug.Log("==== ĐÃ TỰ ĐỘNG TẠO GIAO DIỆN PHONG CÁCH HOLLOW KNIGHT ====");
        Debug.Log("Vào thẻ Tools -> S_RPG -> Generate Hollow Knight UI mỗi khi lỡ tay xóa mất nha!");
    }

    private static Button CreateHKButton(string name, string text, Transform parent)
    {
        GameObject btnObj = new GameObject(name);
        if (parent != null) btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 50); // Nút ốm, dài vừa phải

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // Background tàng hình, lấy form để bắt event click

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 32;
        tmp.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        // Setup Hover Color cho Text
        btn.targetGraphic = tmp;
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.6f, 0.6f, 0.6f, 1f); // Xám xám lạnh lùng
        colors.highlightedColor = Color.white;                // Di chuột vô là trắng sướt
        colors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        btn.colors = colors;

        return btn;
    }

    private static Button CreateHKSlotPrefab(string name, Transform parent)
    {
        GameObject btnObj = new GameObject(name);
        if (parent != null) btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(450, 110); // Cục Save vừa vặn

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.12f, 0.9f); // Nền xám đen nhạt

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Viền mờ bao quanh như thẻ bài

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "<size=120%><b>SLOT 1</b></size>\n<color=#bbbbbb>Hành trình mới</color>";
        tmp.fontSize = 20;
        tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        tmp.alignment = TextAlignmentOptions.Left;

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(20, 10);
        txtRect.offsetMax = new Vector2(-20, -10);

        // Hover Effect vô cái Panel chứ hổng phải Text
        btn.targetGraphic = img;
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        colors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.pressedColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        btn.colors = colors;

        return btn;
    }
}
