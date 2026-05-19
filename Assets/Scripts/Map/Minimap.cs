using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

/// <summary>
/// Minimap hiển thị ở góc trên bên phải màn hình.
/// Camera riêng chụp xuống map từ trên cao, theo dõi Player, render vào RawImage trên Canvas.
/// 
/// ★ QUAN TRỌNG: Trong URP, MinimapCamera LUÔN bị disable.
/// Minimap render thủ công qua Camera.Render() để tránh xung đột với Main Camera.
/// 
/// Setup tự động qua menu: Tools/Minimap/1. Tạo Minimap Tự Động
/// </summary>
public class Minimap : MonoBehaviour
{
    public static Minimap Instance;

    [Header("References")]
    [Tooltip("Camera chuyên dụng cho minimap (orthographic, nhìn xuống map).")]
    public Camera minimapCamera;

    [Tooltip("RawImage trên Canvas để hiển thị minimap.")]
    public RawImage minimapDisplay;

    [Header("Player Marker")]
    [Tooltip("GameObject đánh dấu vị trí Player trên minimap (mũi tên/chấm). Tự tạo nếu để trống.")]
    public RectTransform playerMarker;

    [Header("Settings")]
    [Tooltip("Độ zoom (orthographicSize) của minimap camera. Nhỏ hơn = zoom gần hơn.")]
    [Range(1f, 50f)]
    public float zoomLevel = 15f;

    [Tooltip("Cho phép Player cuộn chuột để zoom minimap trong lúc chơi.")]
    public bool allowScrollZoom = true;

    [Tooltip("Tốc độ mượt khi camera minimap bám theo Player.")]
    [Range(1f, 20f)]
    public float followSmoothness = 8f;

    [Tooltip("Đồng bộ theo Main Camera (góc nhìn ngoài) thay vì bám thẳng Player.")]
    public bool syncWithMainCamera = true;

    [Tooltip("Bật để camera minimap bám mượt (có trễ nhẹ). Tắt để bám tức thì.")]
    public bool smoothFollow = false;

    [Header("World Bounds")]
    [Tooltip("Tự động chặn camera minimap trong phạm vi map để tránh lộ màu nền ở rìa.")]
    public bool clampToWorldBounds = true;
    [Tooltip("Padding mép trong world bounds (đơn vị world).")]
    [Range(0f, 3f)]
    public float boundsPadding = 0.2f;

    [Header("Visibility")]

    [Tooltip("Minimap có hiện khi bắt đầu không.")]
    public bool visibleOnStart = true;

    [Header("Map Name Display")]
    [Tooltip("(Tùy chọn) Text hiển thị tên khu vực đang đứng.")]
    public TMPro.TMP_Text mapNameText;

    [Header("Performance")]
    [Tooltip("Số frame giữa mỗi lần render minimap. 3 = render mỗi 3 frame (~20fps). Cao hơn = nhẹ hơn.")]
    [Range(1, 10)]
    public int renderInterval = 3;

    private Transform playerTransform;
    private bool isVisible = true;
    private CanvasGroup canvasGroup;
    private int frameCounter = 0;
    private Bounds worldBounds;
    private bool hasWorldBounds = false;

    // ================= INIT =================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private const string MINIMAP_ICON_LAYER = "MinimapIcon";

    void Start()
    {
        // Tìm Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[Minimap] Không tìm thấy Player (tag 'Player'). Minimap sẽ không bám theo nhân vật.");
        }

        // ★ Setup camera: TẮT camera.enabled để URP không tự render nó
        // Ta sẽ gọi Camera.Render() thủ công thay vì để URP pipeline xử lý
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = zoomLevel;
            minimapCamera.enabled = false; // ★ CRITICAL: Tắt để tránh xung đột URP
            Debug.Log("[Minimap] MinimapCamera.enabled = false (render thủ công để tránh xung đột URP).");
        }

        // CanvasGroup cho fade in/out
        if (minimapDisplay != null)
        {
            canvasGroup = minimapDisplay.GetComponentInParent<CanvasGroup>();
        }

        // Cấu hình layer culling: MinimapIcon chỉ hiện trên minimap, ẩn khỏi main camera
        ConfigureLayerCulling();
        CollectWorldBounds();
        zoomLevel = ClampZoomByBounds(zoomLevel);

        // Hiện/ẩn ban đầu
        isVisible = visibleOnStart;
        SetVisibility(isVisible);

        // Cập nhật tên map
        UpdateMapName();

        // Không dùng marker đỏ nữa
        if (playerMarker != null)
        {
            Destroy(playerMarker.gameObject);
            playerMarker = null;
        }

        // Render 1 frame minimap ngay lập tức
        RenderMinimapManually();
    }

    /// <summary>
    /// Tự động cấu hình camera layers:
    /// - Main Camera: ẩn layer MinimapIcon
    /// - Minimap Camera: hiện tất cả (bao gồm MinimapIcon)
    /// </summary>
    void ConfigureLayerCulling()
    {
        int iconLayer = LayerMask.NameToLayer(MINIMAP_ICON_LAYER);
        if (iconLayer == -1)
        {
            Debug.LogWarning("[Minimap] Layer 'MinimapIcon' chưa tồn tại. Icon trụ dịch chuyển sẽ hiện trên cả 2 camera.");
            return;
        }

        int iconLayerMask = 1 << iconLayer;

        // Main Camera: loại bỏ MinimapIcon layer
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.cullingMask &= ~iconLayerMask;
            Debug.Log("[Minimap] Main Camera: đã ẩn layer MinimapIcon.");
        }

        // Minimap Camera: đảm bảo bao gồm MinimapIcon layer
        if (minimapCamera != null)
        {
            minimapCamera.cullingMask |= iconLayerMask;
            Debug.Log("[Minimap] Minimap Camera: đã bật layer MinimapIcon.");
        }
    }

    /// <summary>
    /// Render minimap thủ công, không qua URP pipeline.
    /// Camera.Render() sẽ chụp ngay lập tức vào targetTexture mà không gây xung đột.
    /// </summary>
    void RenderMinimapManually()
    {
        if (minimapCamera == null || minimapCamera.targetTexture == null) return;
        if (!isVisible) return;

        // Camera.Render() hoạt động ngay cả khi camera.enabled = false
        minimapCamera.Render();
    }

    // ================= UPDATE =================

    void LateUpdate()
    {
        // Tìm lại Player nếu chưa có (phòng trường hợp Player spawn muộn)
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else return;
        }

        // Bám theo Player
        if (minimapCamera != null)
        {
            Vector3 targetPos = playerTransform.position;
            Camera mainCam = Camera.main;
            if (syncWithMainCamera && mainCam != null)
            {
                targetPos = mainCam.transform.position;
            }

            if (clampToWorldBounds && hasWorldBounds)
            {
                targetPos = ClampTargetToWorldBounds(targetPos);
            }
            targetPos.z = minimapCamera.transform.position.z;

            if (smoothFollow)
            {
                minimapCamera.transform.position = Vector3.Lerp(
                    minimapCamera.transform.position,
                    targetPos,
                    followSmoothness * Time.deltaTime
                );
            }
            else
            {
                minimapCamera.transform.position = targetPos;
            }

            minimapCamera.orthographicSize = zoomLevel;
        }

        // Render minimap thủ công mỗi N frame (tiết kiệm performance)
        if (isVisible)
        {
            frameCounter++;
            if (frameCounter >= renderInterval)
            {
                frameCounter = 0;
                RenderMinimapManually();
            }
        }

        // Scroll zoom
        if (allowScrollZoom && isVisible)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    zoomLevel = ClampZoomByBounds(zoomLevel - scroll * 0.01f);
                }
            }
        }

        // Toggle phím M
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[Key.M].wasPressedThisFrame)
        {
            isVisible = !isVisible;
            SetVisibility(isVisible);
        }
    }

    // ================= HELPERS =================

    void SetVisibility(bool visible)
    {
        if (minimapDisplay != null)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = visible;
            }
            else
            {
                minimapDisplay.gameObject.SetActive(visible);
            }
        }

        // ★ KHÔNG bật/tắt minimapCamera.enabled!
        // Camera luôn disabled, render thủ công qua Camera.Render()
    }

    /// <summary>Cập nhật tên map hiển thị trên minimap (gọi từ MapManager.SetMap).</summary>
    public void UpdateMapName()
    {
        if (mapNameText == null) return;

        if (MapManager.Instance != null && MapManager.Instance.currentMap != null)
        {
            mapNameText.text = MapManager.Instance.currentMap.mapName;
        }
        else
        {
            mapNameText.text = "";
        }
    }

    /// <summary>Gọi để thay đổi zoom từ code khác (vd: khi vào dungeon thì zoom gần hơn).</summary>
    public void SetZoom(float newZoom)
    {
        zoomLevel = ClampZoomByBounds(newZoom);
    }

    /// <summary>Bật/tắt minimap từ code.</summary>
    public void Toggle(bool show)
    {
        isVisible = show;
        SetVisibility(isVisible);
    }

    private float GetMinimapAspect()
    {
        if (minimapCamera != null && minimapCamera.targetTexture != null)
        {
            return (float)minimapCamera.targetTexture.width / minimapCamera.targetTexture.height;
        }
        if (minimapCamera != null) return minimapCamera.aspect;
        return 1f;
    }

    private Vector3 ClampTargetToWorldBounds(Vector3 targetPos)
    {
        float aspect = GetMinimapAspect();
        float halfH = zoomLevel;
        float halfW = zoomLevel * aspect;

        float minX = worldBounds.min.x + halfW + boundsPadding;
        float maxX = worldBounds.max.x - halfW - boundsPadding;
        float minY = worldBounds.min.y + halfH + boundsPadding;
        float maxY = worldBounds.max.y - halfH - boundsPadding;

        if (minX > maxX) targetPos.x = worldBounds.center.x;
        else targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);

        if (minY > maxY) targetPos.y = worldBounds.center.y;
        else targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        return targetPos;
    }

    private float GetMaxZoomByBounds()
    {
        if (!hasWorldBounds) return 50f;
        float aspect = GetMinimapAspect();
        float halfHeightByMap = Mathf.Max(0.1f, (worldBounds.size.y * 0.5f) - boundsPadding);
        float halfWidthByMap = Mathf.Max(0.1f, (worldBounds.size.x * 0.5f) - boundsPadding);
        float halfHeightByWidth = halfWidthByMap / Mathf.Max(0.0001f, aspect);
        return Mathf.Min(halfHeightByMap, halfHeightByWidth);
    }

    private float ClampZoomByBounds(float value)
    {
        float maxByBounds = GetMaxZoomByBounds();
        float safeMax = Mathf.Clamp(maxByBounds, 1f, 50f);
        return Mathf.Clamp(value, 1f, safeMax);
    }

    private void CollectWorldBounds()
    {
        hasWorldBounds = false;

        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tm = tilemaps[i];
            if (tm == null || !tm.gameObject.activeInHierarchy) continue;
            if (tm.cellBounds.size == Vector3Int.zero) continue;

            tm.CompressBounds();
            BoundsInt cb = tm.cellBounds;
            Vector3Int minCell = cb.min;
            Vector3Int maxCell = cb.max;

            Vector3 worldMin = tm.CellToWorld(minCell);
            Vector3 worldMax = tm.CellToWorld(maxCell);
            Bounds b = new Bounds();
            b.SetMinMax(
                Vector3.Min(worldMin, worldMax),
                Vector3.Max(worldMin, worldMax)
            );

            if (!hasWorldBounds)
            {
                worldBounds = b;
                hasWorldBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(b);
            }
        }

        if (!hasWorldBounds)
        {
            SpriteRenderer[] spriteRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (!spriteRenderers[i].enabled || !spriteRenderers[i].gameObject.activeInHierarchy) continue;
                if (!hasWorldBounds)
                {
                    worldBounds = spriteRenderers[i].bounds;
                    hasWorldBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(spriteRenderers[i].bounds);
                }
            }
        }

        if (hasWorldBounds)
        {
            Debug.Log($"[Minimap] World bounds: center={worldBounds.center}, size={worldBounds.size}");
        }
        else
        {
            Debug.LogWarning("[Minimap] Không tìm thấy bounds map để clamp camera minimap.");
        }
    }

}
