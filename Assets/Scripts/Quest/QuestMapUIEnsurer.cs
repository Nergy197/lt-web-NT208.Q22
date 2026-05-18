using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tạo QuestTrackerUnderMinimapUI trên Canvas nếu scene chưa có (MapScene).
/// Thay thế việc bắt buộc chạy Tools/Quest System/6 thủ công mỗi lần.
/// </summary>
public static class QuestMapUIEnsurer
{
    public static void EnsureTrackerUnderMinimap()
    {
        if (Object.FindAnyObjectByType<QuestTrackerUnderMinimapUI>() != null)
            return;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[QuestUI] Không có Canvas — bỏ qua tạo quest tracker.");
            return;
        }

        float posY = -285f;
        Transform minimap = canvas.transform.Find("MinimapContainer");
        if (minimap is RectTransform miniRect)
            posY = miniRect.anchoredPosition.y - miniRect.sizeDelta.y - 12f;

        var panelObj = new GameObject("QuestTrackerPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.07f, 0.12f, 0.92f);
        panelImage.raycastTarget = false;

        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, posY);
        panelRect.sizeDelta = new Vector2(320, 140);

        var outline = panelObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.45f);
        outline.effectDistance = new Vector2(1.5f, 1.5f);

        var titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "NHIEM VU";
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.72f, 0.9f, 1f, 1f);
        titleText.raycastTarget = false;
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -8);
        titleRect.sizeDelta = new Vector2(-16, 28);

        var objectivesObj = new GameObject("ObjectivesText");
        objectivesObj.transform.SetParent(panelObj.transform, false);
        var objectivesText = objectivesObj.AddComponent<TextMeshProUGUI>();
        objectivesText.text = "[ ] Objective";
        objectivesText.fontSize = 14;
        objectivesText.color = new Color(0.85f, 0.9f, 0.96f, 0.98f);
        objectivesText.raycastTarget = false;
        objectivesText.enableWordWrapping = true;
        var objRect = objectivesObj.GetComponent<RectTransform>();
        objRect.anchorMin = Vector2.zero;
        objRect.anchorMax = Vector2.one;
        objRect.offsetMin = new Vector2(10, 8);
        objRect.offsetMax = new Vector2(-10, -34);

        var tracker = panelObj.AddComponent<QuestTrackerUnderMinimapUI>();
        tracker.panel = panelObj;
        tracker.questTitleText = titleText;
        tracker.objectivesText = objectivesText;

        panelObj.SetActive(false);
        Debug.Log("[QuestUI] Đã tạo QuestTrackerUnderMinimapUI trên MapScene.");
    }
}
