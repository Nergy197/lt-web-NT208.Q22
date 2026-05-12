using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

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
    [Range(5f, 50f)]
    public float zoomLevel = 15f;

    [Tooltip("Cho phép Player cuộn chuột để zoom minimap trong lúc chơi.")]
    public bool allowScrollZoom = true;

    [Tooltip("Tốc độ mượt khi camera minimap bám theo Player.")]
    [Range(1f, 20f)]
    public float followSmoothness = 8f;

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

        // Hiện/ẩn ban đầu
        isVisible = visibleOnStart;
        SetVisibility(isVisible);

        // Cập nhật tên map
        UpdateMapName();

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
            targetPos.z = minimapCamera.transform.position.z;

            minimapCamera.transform.position = Vector3.Lerp(
                minimapCamera.transform.position,
                targetPos,
                followSmoothness * Time.deltaTime
            );

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
                    zoomLevel = Mathf.Clamp(zoomLevel - scroll * 0.01f, 5f, 50f);
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
        zoomLevel = Mathf.Clamp(newZoom, 5f, 50f);
    }

    /// <summary>Bật/tắt minimap từ code.</summary>
    public void Toggle(bool show)
    {
        isVisible = show;
        SetVisibility(isVisible);
    }
}
