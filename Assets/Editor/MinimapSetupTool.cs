using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.IO;

/// <summary>
/// Editor Tool tự động tạo toàn bộ hệ thống Minimap trong Scene hiện tại.
/// Truy cập: Tools/Minimap/...
/// </summary>
public class MinimapSetupTool
{
    // ================================================================
    //  TOOL 1: TẠO MINIMAP TỰ ĐỘNG (TOÀN BỘ)
    // ================================================================
    [MenuItem("Tools/Minimap/1. Tạo Minimap Tự Động (Đầy Đủ)")]
    public static void CreateFullMinimap()
    {
        // ---- 1. Tạo RenderTexture ----
        string rtPath = "Assets/Data/MinimapRT.renderTexture";

        RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (rt == null)
        {
            rt = new RenderTexture(512, 512, 16);
            rt.name = "MinimapRT";
            rt.filterMode = FilterMode.Bilinear;

            // Tạo thư mục nếu chưa có
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            AssetDatabase.CreateAsset(rt, rtPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MinimapSetupTool] Đã tạo RenderTexture tại {rtPath}");
        }

        // ---- 2. Tạo Minimap Camera (tương thích URP) ----
        GameObject camObj = GameObject.Find("MinimapCamera");
        Camera minimapCam;
        if (camObj == null)
        {
            camObj = new GameObject("MinimapCamera");
            minimapCam = camObj.AddComponent<Camera>();
            Undo.RegisterCreatedObjectUndo(camObj, "Create Minimap Camera");
        }
        else
        {
            minimapCam = camObj.GetComponent<Camera>();
            if (minimapCam == null) minimapCam = camObj.AddComponent<Camera>();
        }

        minimapCam.orthographic = true;
        minimapCam.orthographicSize = 15f;
        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        minimapCam.targetTexture = rt;
        minimapCam.depth = -10; // Thấp hơn Main Camera (-1) để không tranh chấp
        minimapCam.cullingMask = ~0; // Render tất cả layer

        // ★ QUAN TRỌNG: Thêm UniversalAdditionalCameraData cho URP ★
        // Nếu thiếu component này, URP sẽ coi camera là Base camera thứ 2
        // và ghi đè render output của Main Camera → gây mất ground!
        UniversalAdditionalCameraData urpData = camObj.GetComponent<UniversalAdditionalCameraData>();
        if (urpData == null)
        {
            urpData = camObj.AddComponent<UniversalAdditionalCameraData>();
        }
        // Giữ renderType = Base vì camera này render vào RenderTexture riêng (không ra màn hình)
        urpData.renderType = CameraRenderType.Base;
        urpData.renderPostProcessing = false;
        urpData.requiresDepthTexture = false;
        urpData.requiresColorTexture = false;

        // Đặt camera ở vị trí nhìn xuống (2D orthographic nên z negative)
        camObj.transform.position = new Vector3(0, 0, -10);
        camObj.transform.rotation = Quaternion.identity;

        // Xóa AudioListener nếu bị auto-thêm
        AudioListener al = camObj.GetComponent<AudioListener>();
        if (al != null) Object.DestroyImmediate(al);

        Debug.Log("[MinimapSetupTool] Đã tạo MinimapCamera (URP compatible).");

        // ---- 3. Tìm hoặc Tạo Canvas ----
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            Debug.Log("[MinimapSetupTool] Đã tạo Canvas mới.");
        }
        else
        {
            canvasObj = canvas.gameObject;
        }

        // Đảm bảo Canvas có CanvasScaler đúng
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // ---- 4. Tạo Minimap Container (góc trên bên phải) ----
        // Kiểm tra đã tồn tại chưa
        Transform existingContainer = canvasObj.transform.Find("MinimapContainer");
        GameObject container;
        if (existingContainer != null)
        {
            container = existingContainer.gameObject;
            Debug.Log("[MinimapSetupTool] MinimapContainer đã tồn tại, cập nhật lại.");
        }
        else
        {
            container = new GameObject("MinimapContainer");
            container.transform.SetParent(canvasObj.transform, false);
            Undo.RegisterCreatedObjectUndo(container, "Create Minimap Container");
        }

        RectTransform containerRect = container.GetComponent<RectTransform>();
        if (containerRect == null) containerRect = container.AddComponent<RectTransform>();

        // Đặt góc trên bên phải
        containerRect.anchorMin = new Vector2(1, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(1, 1);
        containerRect.anchoredPosition = new Vector2(-20, -20); // Cách mép 20px
        containerRect.sizeDelta = new Vector2(250, 250);

        // CanvasGroup cho toggle/fade
        CanvasGroup cg = container.GetComponent<CanvasGroup>();
        if (cg == null) cg = container.AddComponent<CanvasGroup>();

        // ---- 5. Tạo viền + mask minimap hình tròn ----
        Sprite circleMaskSprite = GetOrCreateMinimapCircleSprite();

        // Border tròn
        GameObject borderObj = GetOrCreateChild(container, "MinimapBorder");
        RectTransform borderRect = SetupRect(borderObj, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image borderImg = borderObj.GetComponent<Image>();
        if (borderImg == null) borderImg = borderObj.AddComponent<Image>();
        borderImg.sprite = circleMaskSprite;
        borderImg.type = Image.Type.Simple;
        borderImg.preserveAspect = true;
        borderImg.color = new Color(0.15f, 0.18f, 0.25f, 0.95f); // Viền xanh đen
        borderImg.raycastTarget = false;

        // Vùng mask tròn (ẩn phần góc của minimap)
        GameObject maskObj = GetOrCreateChild(borderObj, "MinimapMask");
        RectTransform maskRect = SetupRect(maskObj, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10, -10));
        Image maskImg = maskObj.GetComponent<Image>();
        if (maskImg == null) maskImg = maskObj.AddComponent<Image>();
        maskImg.sprite = circleMaskSprite;
        maskImg.type = Image.Type.Simple;
        maskImg.preserveAspect = true;
        maskImg.color = Color.white;
        maskImg.raycastTarget = false;
        Mask circleMask = maskObj.GetComponent<Mask>();
        if (circleMask == null) circleMask = maskObj.AddComponent<Mask>();
        circleMask.showMaskGraphic = false;

        // Nếu có cấu trúc cũ: chuyển MinimapImage vào trong MinimapMask
        Transform legacyRaw = container.transform.Find("MinimapImage");
        if (legacyRaw != null && legacyRaw.parent != maskObj.transform)
        {
            legacyRaw.SetParent(maskObj.transform, false);
        }

        // ---- 6. Tạo RawImage hiển thị minimap ----
        GameObject rawImgObj = GetOrCreateChild(maskObj, "MinimapImage");
        RectTransform rawRect = SetupRect(rawImgObj, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6, -6)); // padding 3px mỗi bên
        RawImage rawImg = rawImgObj.GetComponent<RawImage>();
        if (rawImg == null) rawImg = rawImgObj.AddComponent<RawImage>();
        rawImg.texture = rt;
        rawImg.raycastTarget = false;

        // Không dùng PlayerMarker đỏ nữa (player đã hiển thị trực tiếp trên minimap)
        Transform legacyMarker = rawImgObj.transform.Find("PlayerMarker");
        if (legacyMarker != null)
        {
            Undo.DestroyObjectImmediate(legacyMarker.gameObject);
        }

        // ---- 7. Tạo Label tên map ----
        GameObject labelObj = GetOrCreateChild(container, "MapNameLabel");
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        if (labelRect == null) labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 0);
        labelRect.pivot = new Vector2(0.5f, 0);
        labelRect.anchoredPosition = new Vector2(0, 4);
        labelRect.sizeDelta = new Vector2(0, 24);

        TMPro.TMP_Text mapLabel = labelObj.GetComponent<TMPro.TextMeshProUGUI>();
        if (mapLabel == null) mapLabel = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        mapLabel.text = "???";
        mapLabel.fontSize = 14;
        mapLabel.alignment = TMPro.TextAlignmentOptions.Center;
        mapLabel.color = new Color(0.85f, 0.9f, 1f, 0.9f); // Trắng xanh nhẹ
        mapLabel.raycastTarget = false;

        // ---- 8. Tạo Label phím tắt nhỏ ----
        GameObject hintObj = GetOrCreateChild(container, "ToggleHint");
        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        if (hintRect == null) hintRect = hintObj.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1, 0);
        hintRect.anchorMax = new Vector2(1, 0);
        hintRect.pivot = new Vector2(1, 0);
        hintRect.anchoredPosition = new Vector2(-4, 4);
        hintRect.sizeDelta = new Vector2(40, 16);

        TMPro.TMP_Text hintLabel = hintObj.GetComponent<TMPro.TextMeshProUGUI>();
        if (hintLabel == null) hintLabel = hintObj.AddComponent<TMPro.TextMeshProUGUI>();
        hintLabel.text = "[M]";
        hintLabel.fontSize = 10;
        hintLabel.alignment = TMPro.TextAlignmentOptions.BottomRight;
        hintLabel.color = new Color(0.6f, 0.65f, 0.7f, 0.6f);
        hintLabel.raycastTarget = false;

        // ---- 9. Gắn Minimap script vào Container ----
        Minimap minimap = container.GetComponent<Minimap>();
        if (minimap == null) minimap = container.AddComponent<Minimap>();

        minimap.minimapCamera = minimapCam;
        minimap.minimapDisplay = rawImg;
        minimap.playerMarker = null;
        minimap.mapNameText = mapLabel;
        minimap.zoomLevel = 15f;
        minimap.visibleOnStart = true;

        // ---- Done ----
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = container;

        Debug.Log("========================================");
        Debug.Log("[MinimapSetupTool] ✅ Minimap đã được tạo hoàn chỉnh!");
        Debug.Log("  • MinimapCamera: orthographic, zoom 15, render to texture");
        Debug.Log("  • MinimapContainer: góc trên bên phải, 250x250px (mask hình tròn)");
        Debug.Log("  • Player Marker: đã tắt");
        Debug.Log("  • Map Name Label: hiện tên khu vực ở dưới minimap");
        Debug.Log("  • Phím M: bật/tắt minimap");
        Debug.Log("  • Scroll chuột: zoom in/out minimap");
        Debug.Log("========================================");
    }

    // ================================================================
    //  TOOL 2: XÓA MINIMAP KHỎI SCENE
    // ================================================================
    [MenuItem("Tools/Minimap/2. Xóa Minimap Khỏi Scene")]
    public static void RemoveMinimap()
    {
        // Xóa Camera
        GameObject camObj = GameObject.Find("MinimapCamera");
        if (camObj != null)
        {
            Undo.DestroyObjectImmediate(camObj);
            Debug.Log("[MinimapSetupTool] Đã xóa MinimapCamera.");
        }

        // Xóa Container
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform container = canvas.transform.Find("MinimapContainer");
            if (container != null)
            {
                Undo.DestroyObjectImmediate(container.gameObject);
                Debug.Log("[MinimapSetupTool] Đã xóa MinimapContainer.");
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[MinimapSetupTool] ✅ Minimap đã được xóa khỏi scene.");
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private static GameObject GetOrCreateChild(GameObject parent, string childName)
    {
        Transform existing = parent.transform.Find(childName);
        if (existing != null) return existing.gameObject;

        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent.transform, false);
        Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
        return child;
    }

    private static RectTransform SetupRect(GameObject obj,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        // Khi anchor là stretch (0,0 → 1,1): sizeDelta = padding (negative = inset)
        if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
        {
            rect.offsetMin = new Vector2(-sizeDelta.x / 2f, -sizeDelta.y / 2f); // left, bottom
            rect.offsetMax = new Vector2(sizeDelta.x / 2f, sizeDelta.y / 2f);   // right, top

            // Nếu sizeDelta negative → inset padding
            if (sizeDelta.x < 0 || sizeDelta.y < 0)
            {
                float pad = Mathf.Abs(sizeDelta.x) / 2f;
                rect.offsetMin = new Vector2(pad, pad);
                rect.offsetMax = new Vector2(-pad, -pad);
            }
        }

        return rect;
    }

    private static Sprite GetOrCreateMinimapCircleSprite()
    {
        const string spritePath = "Assets/Data/MinimapCircleMask.png";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
        if (texture == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            const int size = 256;
            Texture2D generated = new Texture2D(size, size, TextureFormat.RGBA32, false);
            generated.filterMode = FilterMode.Bilinear;

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float radius = size * 0.5f - 2f;
            float feather = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((radius - dist) / feather);
                    generated.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            generated.Apply();
            string fullPath = Path.Combine(Application.dataPath, "Data", "MinimapCircleMask.png");
            File.WriteAllBytes(fullPath, generated.EncodeToPNG());
            Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
        }

        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }
}
