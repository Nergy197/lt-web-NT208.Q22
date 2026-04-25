using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Editor Tools cho hệ thống Teleport Pillar (Fast-Travel).
/// Mỗi trụ vừa là spawn point vừa là nơi tương tác.
/// Truy cập: Tools/Teleport Menu/...
/// </summary>
public class TeleportMenuSetupTool
{
    // ================================================================
    //  TOOL 1: TẠO TELEPORT MENU UI TỰ ĐỘNG
    // ================================================================
    [MenuItem("Tools/Teleport/1. Tạo Teleport Menu UI Tự Động")]
    public static void CreateTeleportMenuUI()
    {
        TeleportMenuUI existingUI = Object.FindFirstObjectByType<TeleportMenuUI>();
        if (existingUI != null)
        {
            Debug.Log("[TeleportMenuSetupTool] TeleportMenuUI đã tồn tại, bỏ qua. Xóa cái cũ trước nếu muốn tạo lại.");
            Selection.activeGameObject = existingUI.gameObject;
            return;
        }

        // Tạo Canvas RIÊNG cho Teleport Menu (không dùng chung với Minimap hay Canvas khác)
        Canvas canvas = null;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.gameObject.name == "TeleportCanvas")
            {
                canvas = c;
                break;
            }
        }

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TeleportCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // Cao hơn Minimap (100) và Quest (90)
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create TeleportCanvas");
            Debug.Log("[TeleportMenuSetupTool] Đã tạo TeleportCanvas riêng (sortingOrder=200).");
        }

        // EventSystem
        UnityEngine.EventSystems.EventSystem es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }

        // System Root
        GameObject systemObj = new GameObject("TeleportMenuSystem");
        systemObj.transform.SetParent(canvas.transform, false);
        TeleportMenuUI menuUI = systemObj.AddComponent<TeleportMenuUI>();
        Undo.RegisterCreatedObjectUndo(systemObj, "Create TeleportMenuSystem");

        // Panel
        GameObject panelObj = new GameObject("TeleportPanel");
        panelObj.transform.SetParent(systemObj.transform, false);
        UnityEngine.UI.Image panelImg = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.06f, 0.08f, 0.14f, 0.92f);
        panelImg.raycastTarget = true;
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460, 400);
        panelRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Outline panelOutline = panelObj.AddComponent<UnityEngine.UI.Outline>();
        panelOutline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.6f);
        panelOutline.effectDistance = new Vector2(2, 2);

        // Title
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform, false);
        TMPro.TextMeshProUGUI titleText = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.text = "TRỤ DỊCH CHUYỂN";
        titleText.fontSize = 28;
        titleText.fontStyle = TMPro.FontStyles.Bold;
        titleText.alignment = TMPro.TextAlignmentOptions.Center;
        titleText.color = new Color(0.6f, 0.85f, 1f);
        titleText.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -12);
        titleRect.sizeDelta = new Vector2(0, 44);

        // Separator
        GameObject sepObj = new GameObject("Separator");
        sepObj.transform.SetParent(panelObj.transform, false);
        UnityEngine.UI.Image sepImg = sepObj.AddComponent<UnityEngine.UI.Image>();
        sepImg.color = new Color(0.3f, 0.5f, 0.8f, 0.4f);
        sepImg.raycastTarget = false;
        RectTransform sepRect = sepObj.GetComponent<RectTransform>();
        sepRect.anchorMin = new Vector2(0.1f, 1);
        sepRect.anchorMax = new Vector2(0.9f, 1);
        sepRect.pivot = new Vector2(0.5f, 1);
        sepRect.anchoredPosition = new Vector2(0, -58);
        sepRect.sizeDelta = new Vector2(0, 2);

        // ScrollArea
        GameObject scrollObj = new GameObject("ScrollArea");
        scrollObj.transform.SetParent(panelObj.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(20, 80);
        scrollRect.offsetMax = new Vector2(-20, -65);

        UnityEngine.UI.ScrollRect scroll = scrollObj.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        UnityEngine.UI.Image scrollBg = scrollObj.AddComponent<UnityEngine.UI.Image>();
        scrollBg.color = new Color(0, 0, 0, 0.01f);
        UnityEngine.UI.Mask mask = scrollObj.AddComponent<UnityEngine.UI.Mask>();
        mask.showMaskGraphic = false;

        // Content (button container)
        GameObject contentObj = new GameObject("ButtonContainer");
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        UnityEngine.UI.VerticalLayoutGroup vlg = contentObj.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        UnityEngine.UI.ContentSizeFitter csf = contentObj.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;

        // Button Template
        GameObject templateObj = new GameObject("ButtonTemplate");
        templateObj.transform.SetParent(contentObj.transform, false);
        UnityEngine.UI.Image templateBg = templateObj.AddComponent<UnityEngine.UI.Image>();
        templateBg.color = new Color(0.15f, 0.2f, 0.3f, 0.85f);

        UnityEngine.UI.Button templateBtn = templateObj.AddComponent<UnityEngine.UI.Button>();
        ColorBlock cb = templateBtn.colors;
        cb.normalColor = new Color(0.15f, 0.2f, 0.3f, 0.85f);
        cb.highlightedColor = new Color(0.25f, 0.4f, 0.6f, 1f);
        cb.pressedColor = new Color(0.1f, 0.3f, 0.5f, 1f);
        cb.selectedColor = new Color(0.2f, 0.35f, 0.55f, 1f);
        templateBtn.colors = cb;

        RectTransform templateRect = templateObj.GetComponent<RectTransform>();
        templateRect.sizeDelta = new Vector2(0, 50);

        UnityEngine.UI.LayoutElement templateLE = templateObj.AddComponent<UnityEngine.UI.LayoutElement>();
        templateLE.minHeight = 50;
        templateLE.preferredHeight = 50;

        UnityEngine.UI.Outline templateOutline = templateObj.AddComponent<UnityEngine.UI.Outline>();
        templateOutline.effectColor = new Color(0.3f, 0.5f, 0.7f, 0.3f);
        templateOutline.effectDistance = new Vector2(1, 1);

        GameObject templateTextObj = new GameObject("Text");
        templateTextObj.transform.SetParent(templateObj.transform, false);
        TMPro.TextMeshProUGUI templateText = templateTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        templateText.text = "Destination";
        templateText.fontSize = 20;
        templateText.alignment = TMPro.TextAlignmentOptions.Center;
        templateText.color = new Color(0.85f, 0.9f, 1f);
        templateText.raycastTarget = false;
        RectTransform templateTextRect = templateTextObj.GetComponent<RectTransform>();
        templateTextRect.anchorMin = Vector2.zero;
        templateTextRect.anchorMax = Vector2.one;
        templateTextRect.offsetMin = new Vector2(10, 0);
        templateTextRect.offsetMax = new Vector2(-10, 0);

        templateObj.SetActive(false);

        // Description
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(panelObj.transform, false);
        TMPro.TextMeshProUGUI descText = descObj.AddComponent<TMPro.TextMeshProUGUI>();
        descText.text = "Chọn điểm đến để dịch chuyển.";
        descText.fontSize = 16;
        descText.fontStyle = TMPro.FontStyles.Italic;
        descText.alignment = TMPro.TextAlignmentOptions.Center;
        descText.color = new Color(0.6f, 0.65f, 0.7f, 0.8f);
        descText.raycastTarget = false;
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 0);
        descRect.pivot = new Vector2(0.5f, 0);
        descRect.anchoredPosition = new Vector2(0, 42);
        descRect.sizeDelta = new Vector2(-40, 30);

        // Close Button
        GameObject closeBtnObj = new GameObject("Btn_Close");
        closeBtnObj.transform.SetParent(panelObj.transform, false);
        UnityEngine.UI.Image closeBg = closeBtnObj.AddComponent<UnityEngine.UI.Image>();
        closeBg.color = new Color(0.5f, 0.15f, 0.15f, 0.85f);
        UnityEngine.UI.Button closeBtn = closeBtnObj.AddComponent<UnityEngine.UI.Button>();
        ColorBlock closeCb = closeBtn.colors;
        closeCb.normalColor = new Color(0.5f, 0.15f, 0.15f, 0.85f);
        closeCb.highlightedColor = new Color(0.7f, 0.2f, 0.2f, 1f);
        closeCb.pressedColor = new Color(0.4f, 0.1f, 0.1f, 1f);
        closeBtn.colors = closeCb;
        RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0);
        closeRect.anchorMax = new Vector2(0.5f, 0);
        closeRect.pivot = new Vector2(0.5f, 0);
        closeRect.anchoredPosition = new Vector2(0, 10);
        closeRect.sizeDelta = new Vector2(180, 36);

        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);
        TMPro.TextMeshProUGUI closeText = closeTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        closeText.text = "Đóng (Esc)";
        closeText.fontSize = 18;
        closeText.alignment = TMPro.TextAlignmentOptions.Center;
        closeText.color = new Color(1f, 0.8f, 0.8f);
        closeText.raycastTarget = false;
        RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        // Wire references
        SerializedObject so = new SerializedObject(menuUI);
        so.Update();
        so.FindProperty("panel").objectReferenceValue = panelObj;
        so.FindProperty("titleText").objectReferenceValue = titleText;
        so.FindProperty("descriptionText").objectReferenceValue = descText;
        so.FindProperty("buttonContainer").objectReferenceValue = contentObj;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.FindProperty("buttonTemplate").objectReferenceValue = templateObj;

        // Tự động tìm tất cả Mapdata assets và gán vào availableMaps
        string[] mapGuids = AssetDatabase.FindAssets("t:Mapdata");
        SerializedProperty mapListProp = so.FindProperty("availableMaps");
        if (mapListProp != null)
        {
            mapListProp.ClearArray();
            for (int i = 0; i < mapGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(mapGuids[i]);
                Mapdata mapAsset = AssetDatabase.LoadAssetAtPath<Mapdata>(path);
                if (mapAsset != null)
                {
                    mapListProp.InsertArrayElementAtIndex(i);
                    mapListProp.GetArrayElementAtIndex(i).objectReferenceValue = mapAsset;
                }
            }
            Debug.Log($"[TeleportMenuSetupTool] Đã gán {mapGuids.Length} Mapdata vào availableMaps.");
        }

        so.ApplyModifiedProperties();

        panelObj.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = systemObj;

        Debug.Log("✅ [TeleportMenuSetupTool] Teleport Menu UI đã được tạo thành công!");
    }

    // ================================================================
    //  TOOL 2: TẠO TRỤ MỚI (THAY THẾ CŨ)
    //  Xóa tất cả trụ cũ trong scene trước khi tạo trụ mới.
    //  Đảm bảo mỗi scene chỉ có đúng 1 trụ teleport.
    // ================================================================
    [MenuItem("Tools/Teleport/2. Tạo Trụ Mới (Thay Thế Cũ)")]
    public static void CreateTeleportPillar()
    {
        // Xóa tất cả trụ cũ trong scene
        TeleportPillar[] existingPillars = Object.FindObjectsByType<TeleportPillar>(FindObjectsSortMode.None);
        if (existingPillars.Length > 0)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Xác nhận thay thế trụ",
                $"Hiện có {existingPillars.Length} trụ teleport trong scene.\n" +
                "Tất cả sẽ bị XÓA trước khi tạo trụ mới.\n\n" +
                "Bạn có chắc muốn tiếp tục?",
                "Xóa và tạo mới",
                "Hủy"
            );

            if (!confirm) return;

            foreach (var old in existingPillars)
            {
                Debug.Log($"[TeleportMenuSetupTool] Xóa trụ cũ: '{old.pillarName}' ({old.gameObject.name})");
                Undo.DestroyObjectImmediate(old.gameObject);
            }
        }

        // Tạo trụ mới
        GameObject pillarObj = new GameObject("TeleportPillar");
        Undo.RegisterCreatedObjectUndo(pillarObj, "Create Teleport Pillar");

        // Vị trí: SceneView camera
        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 pos = SceneView.lastActiveSceneView.camera.transform.position;
            pos.z = 0;
            pillarObj.transform.position = pos;
        }

        // Visual
        SpriteRenderer sr = pillarObj.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.4f, 0.3f, 0.8f, 0.8f);
        sr.sortingOrder = 5;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        pillarObj.transform.localScale = new Vector3(1f, 2f, 1f);

        // Collider trigger
        BoxCollider2D col = pillarObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 1.5f);

        // Script
        TeleportPillar pillar = pillarObj.AddComponent<TeleportPillar>();
        pillar.pillarName = "Trụ Mới";

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = pillarObj;

        Debug.Log("[TeleportMenuSetupTool] ✅ Đã tạo Trụ Dịch Chuyển mới (đã xóa trụ cũ nếu có)." +
                  "\n  → Đổi tên trong 'Pillar Name'." +
                  "\n  → Kéo tới vị trí mong muốn." +
                  "\n  → Gán Mapdata nếu cần." +
                  "\n  → Mỗi map nên chỉ có 1 trụ duy nhất!");
    }

    // ================================================================
    //  TOOL 3: KIỂM TRA LỖI
    // ================================================================
    [MenuItem("Tools/Teleport/3. Kiểm Tra Lỗi Hệ Thống Teleport")]
    public static void ValidateSystem()
    {
        var report = new List<string>();
        int errors = 0, warnings = 0;

        // Check UI
        TeleportMenuUI menuUI = Object.FindFirstObjectByType<TeleportMenuUI>();
        if (menuUI == null)
        {
            report.Add("❌ LỖI: Không tìm thấy TeleportMenuUI! Chạy Tool 1 để tạo.");
            errors++;
        }
        else
        {
            report.Add("✅ TeleportMenuUI: OK");
        }

        // Check Pillars
        TeleportPillar[] pillars = Object.FindObjectsByType<TeleportPillar>(FindObjectsSortMode.None);
        report.Add($"\n=== Tìm thấy {pillars.Length} TeleportPillar ===");

        if (pillars.Length == 0)
        {
            report.Add("⚠️ CẢNH BÁO: Chưa có trụ nào! Chạy Tool 2 để tạo.");
            warnings++;
        }
        else if (pillars.Length > 1)
        {
            report.Add("⚠️ CẢNH BÁO: Có nhiều hơn 1 trụ trong scene! Nên chỉ có 1 trụ mỗi map.");
            report.Add("   → Dùng Tool 4 để xóa sạch, rồi Tool 2 để tạo lại 1 trụ duy nhất.");
            warnings++;
        }

        foreach (var p in pillars)
        {
            string name = p.gameObject.name;

            // Collider
            BoxCollider2D col = p.GetComponent<BoxCollider2D>();
            if (col == null || !col.isTrigger)
            {
                report.Add($"❌ LỖI [{name}]: BoxCollider2D thiếu hoặc chưa bật Is Trigger!");
                errors++;
            }

            // Layer
            if (p.gameObject.layer != 0)
            {
                report.Add($"⚠️ [{name}]: Layer = {LayerMask.LayerToName(p.gameObject.layer)} (nên là Default)");
                warnings++;
            }

            // Info
            report.Add($"✅ [{name}]: pillarName='{p.pillarName}'" +
                       (p.mapData != null ? $", map='{p.mapData.mapName}'" : "") +
                       $", spawnOffset={p.spawnOffset}");
        }

        // Kiểm tra trùng tên
        HashSet<string> seenNames = new HashSet<string>();
        foreach (var p in pillars)
        {
            if (seenNames.Contains(p.pillarName))
            {
                report.Add($"⚠️ CẢNH BÁO: Trùng tên '{p.pillarName}'! Sẽ gây nhầm lẫn trong menu.");
                warnings++;
            }
            seenNames.Add(p.pillarName);
        }

        report.Add($"\n=== TỔNG KẾT: {errors} lỗi, {warnings} cảnh báo ===");
        if (errors == 0 && warnings == 0)
            report.Add("🎉 Tất cả đều hợp lệ!");

        Debug.Log(string.Join("\n", report));
    }

    // ================================================================
    //  TOOL 4: XÓA TOÀN BỘ HỆ THỐNG TELEPORT
    //  Xóa tất cả TeleportPillar và TeleportMenuUI trong scene.
    // ================================================================
    [MenuItem("Tools/Teleport/4. Xóa Toàn Bộ Hệ Thống Teleport")]
    public static void DeleteAllTeleportSystem()
    {
        TeleportPillar[] pillars = Object.FindObjectsByType<TeleportPillar>(FindObjectsSortMode.None);
        TeleportMenuUI[] menus = Object.FindObjectsByType<TeleportMenuUI>(FindObjectsSortMode.None);

        int totalCount = pillars.Length + menus.Length;

        if (totalCount == 0)
        {
            EditorUtility.DisplayDialog(
                "Không có gì để xóa",
                "Không tìm thấy TeleportPillar hoặc TeleportMenuUI nào trong scene.",
                "OK"
            );
            return;
        }

        // Tạo danh sách chi tiết
        var details = new List<string>();
        foreach (var p in pillars)
            details.Add($"  • Trụ: '{p.pillarName}' ({p.gameObject.name})");
        foreach (var m in menus)
            details.Add($"  • Menu UI: {m.gameObject.name}");

        bool confirm = EditorUtility.DisplayDialog(
            "Xác nhận xóa toàn bộ",
            $"Sẽ xóa {totalCount} đối tượng teleport:\n\n" +
            string.Join("\n", details) +
            "\n\nHành động này có thể Undo (Ctrl+Z).\nBạn có chắc muốn xóa?",
            "Xóa tất cả",
            "Hủy"
        );

        if (!confirm) return;

        int deleted = 0;

        // Xóa tất cả TeleportPillar
        foreach (var p in pillars)
        {
            if (p != null && p.gameObject != null)
            {
                Debug.Log($"[TeleportMenuSetupTool] Xóa trụ: '{p.pillarName}' ({p.gameObject.name})");
                Undo.DestroyObjectImmediate(p.gameObject);
                deleted++;
            }
        }

        // Xóa tất cả TeleportMenuUI (bao gồm TeleportMenuSystem parent)
        foreach (var m in menus)
        {
            if (m != null && m.gameObject != null)
            {
                // Xóa root parent nếu tên là TeleportMenuSystem
                GameObject toDelete = m.gameObject;
                if (toDelete.name == "TeleportMenuSystem" || toDelete.transform.parent == null)
                {
                    // Xóa cả system root
                }
                else if (toDelete.transform.parent != null &&
                         toDelete.transform.parent.name == "TeleportMenuSystem")
                {
                    toDelete = toDelete.transform.parent.gameObject;
                }

                Debug.Log($"[TeleportMenuSetupTool] Xóa menu UI: {toDelete.name}");
                Undo.DestroyObjectImmediate(toDelete);
                deleted++;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"🗑️ [TeleportMenuSetupTool] Đã xóa {deleted} đối tượng teleport." +
                  "\n  → Dùng Ctrl+Z nếu muốn hoàn tác." +
                  "\n  → Chạy Tool 1 + Tool 2 để tạo lại hệ thống mới.");
    }
}
