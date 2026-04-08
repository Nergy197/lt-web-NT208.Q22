using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Editor Tools cho hệ thống Quest.
/// Truy cập: Tools/Quest System/...
///
/// Tool 1: Tạo Quest UI tự động (Quest Panel + Branch Panel)
/// Tool 2: Tạo NPC có Quest Actions
/// Tool 3: Tạo Quest Zone Trigger
/// Tool 4: Kiểm tra lỗi hệ thống Quest
/// Tool 5: Liệt kê tất cả QuestSO assets
/// </summary>
public class QuestSetupTool
{
    // ================================================================
    //  TOOL 1: TẠO QUEST UI TỰ ĐỘNG
    // ================================================================
    [MenuItem("Tools/Quest System/1. Tạo Quest UI Tự Động")]
    public static void CreateQuestUI()
    {
        // Kiểm tra đã có
        QuestUI existingUI = Object.FindFirstObjectByType<QuestUI>();
        if (existingUI != null)
        {
            Debug.Log("[QuestSetup] QuestUI đã tồn tại! Chọn object trong Hierarchy.");
            Selection.activeGameObject = existingUI.gameObject;
            return;
        }

        // Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }

        // ---- Quest Panel (góc trái trên) ----
        GameObject questPanel = new GameObject("QuestPanel");
        questPanel.transform.SetParent(canvas.transform, false);
        var panelImg = questPanel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.05f, 0.07f, 0.12f, 0.85f);
        panelImg.raycastTarget = false;
        RectTransform panelRect = questPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(15, -15);
        panelRect.sizeDelta = new Vector2(340, 160);

        var panelOutline = questPanel.AddComponent<UnityEngine.UI.Outline>();
        panelOutline.effectColor = new Color(0.4f, 0.6f, 0.9f, 0.4f);
        panelOutline.effectDistance = new Vector2(1.5f, 1.5f);

        // Title
        GameObject titleObj = new GameObject("QuestTitle");
        titleObj.transform.SetParent(questPanel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "⚔ Quest Title";
        titleText.fontSize = 20;
        titleText.fontStyle = TMPro.FontStyles.Bold;
        titleText.color = new Color(0.7f, 0.9f, 1f);
        titleText.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -8);
        titleRect.sizeDelta = new Vector2(-20, 30);

        // Separator
        GameObject sepObj = new GameObject("Separator");
        sepObj.transform.SetParent(questPanel.transform, false);
        var sepImg = sepObj.AddComponent<UnityEngine.UI.Image>();
        sepImg.color = new Color(0.4f, 0.6f, 0.9f, 0.3f);
        sepImg.raycastTarget = false;
        RectTransform sepRect = sepObj.GetComponent<RectTransform>();
        sepRect.anchorMin = new Vector2(0.05f, 1);
        sepRect.anchorMax = new Vector2(0.95f, 1);
        sepRect.pivot = new Vector2(0.5f, 1);
        sepRect.anchoredPosition = new Vector2(0, -40);
        sepRect.sizeDelta = new Vector2(0, 1.5f);

        // Objectives text
        GameObject objTextObj = new GameObject("ObjectivesText");
        objTextObj.transform.SetParent(questPanel.transform, false);
        TextMeshProUGUI objText = objTextObj.AddComponent<TextMeshProUGUI>();
        objText.text = "◻ Objective 1\n◻ Objective 2";
        objText.fontSize = 16;
        objText.color = new Color(0.8f, 0.85f, 0.9f);
        objText.raycastTarget = false;
        RectTransform objRect = objTextObj.GetComponent<RectTransform>();
        objRect.anchorMin = new Vector2(0, 0);
        objRect.anchorMax = new Vector2(1, 1);
        objRect.offsetMin = new Vector2(12, 10);
        objRect.offsetMax = new Vector2(-12, -45);

        // ---- Branch Panel (giữa màn hình) ----
        GameObject branchPanel = new GameObject("BranchPanel");
        branchPanel.transform.SetParent(canvas.transform, false);
        var branchBg = branchPanel.AddComponent<UnityEngine.UI.Image>();
        branchBg.color = new Color(0.04f, 0.06f, 0.1f, 0.93f);
        RectTransform branchRect = branchPanel.GetComponent<RectTransform>();
        branchRect.anchorMin = new Vector2(0.5f, 0.5f);
        branchRect.anchorMax = new Vector2(0.5f, 0.5f);
        branchRect.pivot = new Vector2(0.5f, 0.5f);
        branchRect.sizeDelta = new Vector2(500, 300);

        var branchOutline = branchPanel.AddComponent<UnityEngine.UI.Outline>();
        branchOutline.effectColor = new Color(0.8f, 0.6f, 0.2f, 0.5f);
        branchOutline.effectDistance = new Vector2(2, 2);

        // Branch Title
        GameObject branchTitle = new GameObject("BranchTitle");
        branchTitle.transform.SetParent(branchPanel.transform, false);
        TextMeshProUGUI branchTitleText = branchTitle.AddComponent<TextMeshProUGUI>();
        branchTitleText.text = "Chọn hướng đi";
        branchTitleText.fontSize = 24;
        branchTitleText.fontStyle = TMPro.FontStyles.Bold;
        branchTitleText.alignment = TextAlignmentOptions.Center;
        branchTitleText.color = new Color(1f, 0.85f, 0.4f);
        branchTitleText.raycastTarget = false;
        RectTransform btRect = branchTitle.GetComponent<RectTransform>();
        btRect.anchorMin = new Vector2(0, 1);
        btRect.anchorMax = new Vector2(1, 1);
        btRect.pivot = new Vector2(0.5f, 1);
        btRect.anchoredPosition = new Vector2(0, -10);
        btRect.sizeDelta = new Vector2(0, 35);

        // Branch Button Container
        GameObject btnContainer = new GameObject("BranchButtonContainer");
        btnContainer.transform.SetParent(branchPanel.transform, false);
        RectTransform containerRect = btnContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.offsetMin = new Vector2(30, 20);
        containerRect.offsetMax = new Vector2(-30, -55);

        var vlg = btnContainer.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Branch Button Prefab (template)
        GameObject btnTemplate = new GameObject("BranchButtonTemplate");
        btnTemplate.transform.SetParent(btnContainer.transform, false);
        var btnBg = btnTemplate.AddComponent<UnityEngine.UI.Image>();
        btnBg.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
        var btn = btnTemplate.AddComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.2f, 0.25f, 0.35f, 0.9f);
        cb.highlightedColor = new Color(0.35f, 0.5f, 0.7f, 1f);
        cb.pressedColor = new Color(0.15f, 0.3f, 0.5f, 1f);
        btn.colors = cb;

        var btnLE = btnTemplate.AddComponent<UnityEngine.UI.LayoutElement>();
        btnLE.minHeight = 45;
        btnLE.preferredHeight = 45;

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnTemplate.transform, false);
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "Lựa chọn";
        btnText.fontSize = 18;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = new Color(0.9f, 0.92f, 1f);
        btnText.raycastTarget = false;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = new Vector2(10, 0);
        btnTextRect.offsetMax = new Vector2(-10, 0);

        btnTemplate.SetActive(false);
        branchPanel.SetActive(false);

        // ---- QuestUI Component ----
        GameObject questUIObj = new GameObject("QuestUISystem");
        questUIObj.transform.SetParent(canvas.transform, false);
        QuestUI questUI = questUIObj.AddComponent<QuestUI>();

        // Wire references via SerializedObject
        SerializedObject so = new SerializedObject(questUI);
        so.Update();
        so.FindProperty("questPanel").objectReferenceValue = questPanel;
        so.FindProperty("questTitleText").objectReferenceValue = titleText;
        so.FindProperty("objectivesText").objectReferenceValue = objText;
        so.FindProperty("branchPanel").objectReferenceValue = branchPanel;
        so.FindProperty("branchButtonPrefab").objectReferenceValue = btnTemplate;
        so.FindProperty("branchButtonContainer").objectReferenceValue = btnContainer;
        so.ApplyModifiedProperties();

        questPanel.SetActive(false); // Ẩn ban đầu, QuestUI sẽ tự hiện khi có quest

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = questUIObj;

        Debug.Log("✅ [QuestSetup] Quest UI đã được tạo thành công!" +
                  "\n  → QuestPanel (góc trái trên): hiển thị quest đang active" +
                  "\n  → BranchPanel (giữa): hiển thị lựa chọn rẽ nhánh" +
                  "\n  → BranchButtonTemplate: mẫu nút lựa chọn");
    }

    // ================================================================
    //  TOOL 2: TẠO NPC CÓ QUEST
    // ================================================================
    [MenuItem("Tools/Quest System/2. Tạo NPC Có Quest Actions")]
    public static void CreateNpcWithQuest()
    {
        GameObject npcObj = new GameObject("NPC_New");
        Undo.RegisterCreatedObjectUndo(npcObj, "Create NPC");

        // Vị trí SceneView
        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 pos = SceneView.lastActiveSceneView.camera.transform.position;
            pos.z = 0;
            npcObj.transform.position = pos;
        }

        // Visual placeholder
        SpriteRenderer sr = npcObj.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.2f, 0.8f, 0.4f, 0.9f);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 1;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        // Trigger collider
        BoxCollider2D col = npcObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 2f);

        // NpcTrigger script
        npcObj.AddComponent<NpcTrigger>();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = npcObj;

        Debug.Log("[QuestSetup] Đã tạo NPC." +
                  "\n  1. Tạo NpcData: Chuột phải Project → Create → NPC → NPC Data" +
                  "\n  2. Kéo NpcData vào field 'Npc Data'" +
                  "\n  3. Thêm Quest Actions: kéo QuestSO vào, chọn TriggerOn = OnDialogueEnd" +
                  "\n  4. (Tùy chọn) Tạo Dialogue UI Panel và kéo vào fields");
    }

    // ================================================================
    //  TOOL 3: TẠO QUEST ZONE TRIGGER
    // ================================================================
    [MenuItem("Tools/Quest System/3. Tạo Quest Zone Trigger")]
    public static void CreateQuestZone()
    {
        GameObject zoneObj = new GameObject("QuestZone_New");
        Undo.RegisterCreatedObjectUndo(zoneObj, "Create Quest Zone");

        // Vị trí SceneView
        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 pos = SceneView.lastActiveSceneView.camera.transform.position;
            pos.z = 0;
            zoneObj.transform.position = pos;
        }

        // Collider trigger
        BoxCollider2D col = zoneObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(5f, 5f);

        // QuestZoneTrigger script
        zoneObj.AddComponent<QuestZoneTrigger>();

        // Visual indicator (chỉ hiện trong edit mode)
        SpriteRenderer sr = zoneObj.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.8f, 0.2f, 0.15f);
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        zoneObj.transform.localScale = new Vector3(5f, 5f, 1f);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = zoneObj;

        Debug.Log("[QuestSetup] Đã tạo Quest Zone Trigger." +
                  "\n  1. Kéo resize collider theo vùng mong muốn" +
                  "\n  2. Thêm Quest Actions: kéo QuestSO, chọn TriggerOn = OnEnterZone / OnExitZone" +
                  "\n  3. Bật/tắt 'Trigger Once' tùy nhu cầu");
    }

    // ================================================================
    //  TOOL 4: KIỂM TRA LỖI HỆ THỐNG QUEST
    // ================================================================
    [MenuItem("Tools/Quest System/4. Kiểm Tra Lỗi Hệ Thống Quest")]
    public static void ValidateQuestSystem()
    {
        var report = new List<string>();
        int errors = 0, warnings = 0;

        // --- QuestManager ---
        QuestManager qm = Object.FindFirstObjectByType<QuestManager>();
        if (qm == null)
        {
            report.Add("❌ LỖI: Không tìm thấy QuestManager trong scene!");
            errors++;
        }
        else
        {
            report.Add($"✅ QuestManager: OK ({qm.AllQuests.Count} quests, {qm.StartingQuests.Count} starting)");
            
            if (qm.StartingQuests.Count == 0)
            {
                report.Add("⚠️ CẢNH BÁO: StartingQuests trống — game sẽ không có quest nào khi bắt đầu!");
                warnings++;
            }

            // Kiểm tra trùng ID
            HashSet<string> seenIds = new HashSet<string>();
            foreach (var q in qm.AllQuests)
            {
                if (q == null) { report.Add("❌ LỖI: Có slot null trong AllQuests!"); errors++; continue; }
                if (string.IsNullOrEmpty(q.Id)) { report.Add($"❌ LỖI: Quest '{q.name}' thiếu Id!"); errors++; continue; }
                if (seenIds.Contains(q.Id)) { report.Add($"❌ LỖI: Trùng Id '{q.Id}'!"); errors++; }
                seenIds.Add(q.Id);

                // Kiểm tra objectives
                if (q.Objectives.Count == 0)
                {
                    report.Add($"⚠️ [{q.Id}] Không có Objectives — quest sẽ không bao giờ complete.");
                    warnings++;
                }

                foreach (var obj in q.Objectives)
                {
                    if (string.IsNullOrEmpty(obj.Id))
                    {
                        report.Add($"❌ [{q.Id}] Objective thiếu Id!");
                        errors++;
                    }
                }

                // Kiểm tra NextQuests reference
                foreach (var next in q.NextQuests)
                {
                    if (next == null)
                    {
                        report.Add($"⚠️ [{q.Id}] NextQuests có slot null!");
                        warnings++;
                    }
                }
            }
        }

        // --- QuestUI ---
        QuestUI questUI = Object.FindFirstObjectByType<QuestUI>();
        if (questUI == null)
        {
            report.Add("⚠️ CẢNH BÁO: Thiếu QuestUI — quest sẽ không hiển thị trên UI. Chạy Tool 1.");
            warnings++;
        }
        else
        {
            report.Add("✅ QuestUI: OK");
            if (questUI.questPanel == null) { report.Add("  ❌ questPanel chưa gán!"); errors++; }
            if (questUI.questTitleText == null) { report.Add("  ❌ questTitleText chưa gán!"); errors++; }
            if (questUI.objectivesText == null) { report.Add("  ❌ objectivesText chưa gán!"); errors++; }
        }

        // --- NPC Triggers ---
        NpcTrigger[] npcs = Object.FindObjectsByType<NpcTrigger>(FindObjectsSortMode.None);
        report.Add($"\n=== {npcs.Length} NpcTrigger ===");
        foreach (var npc in npcs)
        {
            string name = npc.gameObject.name;
            if (npc.npcData == null) { report.Add($"  ⚠️ [{name}] NpcData chưa gán!"); warnings++; }
            if (npc.questActions.Count == 0) { report.Add($"  ⚠️ [{name}] Không có Quest Actions."); warnings++; }
            else report.Add($"  ✅ [{name}] {npc.questActions.Count} actions");
        }

        // --- Quest Zone Triggers ---
        QuestZoneTrigger[] zones = Object.FindObjectsByType<QuestZoneTrigger>(FindObjectsSortMode.None);
        report.Add($"\n=== {zones.Length} QuestZoneTrigger ===");
        foreach (var zone in zones)
        {
            string name = zone.gameObject.name;
            if (zone.questActions.Count == 0) { report.Add($"  ⚠️ [{name}] Không có Quest Actions."); warnings++; }
            else report.Add($"  ✅ [{name}] {zone.questActions.Count} actions");

            Collider2D col = zone.GetComponent<Collider2D>();
            if (col == null || !col.isTrigger)
            {
                report.Add($"  ❌ [{name}] Collider2D thiếu hoặc chưa bật Is Trigger!");
                errors++;
            }
        }

        // --- Tổng kết ---
        report.Add($"\n=== TỔNG KẾT: {errors} lỗi, {warnings} cảnh báo ===");
        if (errors == 0 && warnings == 0)
            report.Add("🎉 Tất cả đều hợp lệ!");

        Debug.Log(string.Join("\n", report));
    }

    // ================================================================
    //  TOOL 5: LIỆT KÊ TẤT CẢ QUEST ASSETS
    // ================================================================
    [MenuItem("Tools/Quest System/5. Liệt Kê Tất Cả QuestSO Assets")]
    public static void ListAllQuestAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:QuestSO");
        var report = new List<string>();

        report.Add($"=== Tìm thấy {guids.Length} QuestSO assets ===\n");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            QuestSO quest = AssetDatabase.LoadAssetAtPath<QuestSO>(path);
            if (quest == null) continue;

            string mainTag = quest.IsMainQuest ? " [MAIN]" : "";
            string objCount = $"{quest.Objectives.Count} objectives";
            string nextCount = quest.NextQuests.Count > 0 ? $" → {quest.NextQuests.Count} next" : "";
            string branchCount = quest.BranchChoices.Count > 0 ? $" | {quest.BranchChoices.Count} branches" : "";

            report.Add($"  {quest.Id}{mainTag}: {quest.Title}");
            report.Add($"    {objCount}{nextCount}{branchCount}");
            report.Add($"    Path: {path}\n");
        }

        if (guids.Length == 0)
        {
            report.Add("  Chưa có quest nào! Tạo: Chuột phải Project → Create → Quests → New Quest");
        }

        Debug.Log(string.Join("\n", report));
    }
}
