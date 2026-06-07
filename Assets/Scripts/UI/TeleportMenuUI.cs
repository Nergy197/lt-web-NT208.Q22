using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Menu UI chọn điểm đến khi tương tác với TeleportPillar.
/// Ưu tiên hiển thị danh sách trụ teleport; fallback sang danh sách map.
/// Nhấn Esc hoặc nút Đóng để thoát menu.
///
/// Setup tự động: Tools/Teleport/1. Tạo Teleport Menu UI Tự Động
/// </summary>
public class TeleportMenuUI : MonoBehaviour
{
    public static TeleportMenuUI Instance;

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Content")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button closeButton;

    [Header("Button Template")]
    [Tooltip("Template nút destination. Sẽ bị tắt đi và dùng làm mẫu clone.")]
    [SerializeField] private GameObject buttonTemplate;

    [Header("Prefab layout (tùy chọn)")]
    [Tooltip("Prefab có TeleportMenuBindings. Dùng khi không gán prefab trên TeleportPillar; áp dụng lần đầu mở menu.")]
    [SerializeField] private GameObject menuLayoutPrefab;

    [Header("Teleport Settings")]
    [Tooltip("Thời gian delay nhỏ trước khi teleport (giây). 0 = tức thì.")]
    public float teleportDelay = 0.15f;

    [Header("Map List")]
    [Tooltip("Các map hiển thị khi mở trụ teleport. Một phần tử → một nút.")]
    public List<Mapdata> availableMaps = new List<Mapdata>();

    private TeleportPillar currentPillar;
    private string mapMenuFooterDescription = "";
    private List<GameObject> spawnedButtons = new List<GameObject>();

    // Cooldown chống teleport lặp
    private static float lastTeleportTime = 0f;

    private GameObject _bakPanel;
    private GameObject _bakButtonTemplate;
    private TMP_Text _bakTitleText;
    private TMP_Text _bakDescriptionText;
    private Transform _bakButtonContainer;
    private Button _bakCloseButton;
    private GameObject _layoutInstanceRoot;
    private GameObject _boundLayoutPrefab;
    private bool _defaultsCaptured;

    // ================= INIT =================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!_defaultsCaptured)
        {
            CaptureUiDefaults();
            _defaultsCaptured = true;
        }

        if (buttonTemplate != null) buttonTemplate.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    private void CaptureUiDefaults()
    {
        _bakPanel = panel;
        _bakTitleText = titleText;
        _bakDescriptionText = descriptionText;
        _bakButtonContainer = buttonContainer;
        _bakCloseButton = closeButton;
        _bakButtonTemplate = buttonTemplate;
    }

    private void RestoreUiDefaults()
    {
        panel = _bakPanel;
        titleText = _bakTitleText;
        descriptionText = _bakDescriptionText;
        buttonContainer = _bakButtonContainer;
        closeButton = _bakCloseButton;
        buttonTemplate = _bakButtonTemplate;
    }

    private void ClearLayoutInstance()
    {
        if (_layoutInstanceRoot != null)
        {
            Destroy(_layoutInstanceRoot);
            _layoutInstanceRoot = null;
        }
        _boundLayoutPrefab = null;
        RestoreUiDefaults();
    }

    /// <summary>Prefab map UI: ưu tiên trên TeleportPillar, không thì dùng menuLayoutPrefab trên singleton.</summary>
    private void ResolveMenuLayout(TeleportPillar sourcePillar)
    {
        GameObject want = null;
        if (sourcePillar != null && sourcePillar.MenuLayoutPrefab != null)
            want = sourcePillar.MenuLayoutPrefab;
        else if (menuLayoutPrefab != null)
            want = menuLayoutPrefab;

        if (want == null)
        {
            if (_layoutInstanceRoot != null)
                ClearLayoutInstance();
            return;
        }

        if (_boundLayoutPrefab == want && _layoutInstanceRoot != null)
            return;

        ClearLayoutInstance();

        _layoutInstanceRoot = Instantiate(want, transform, false);
        _layoutInstanceRoot.name = want.name + "(Instance)";
        _boundLayoutPrefab = want;

        var bindings = _layoutInstanceRoot.GetComponent<TeleportMenuBindings>();
        if (bindings == null)
            bindings = _layoutInstanceRoot.GetComponentInChildren<TeleportMenuBindings>(true);
        if (bindings != null)
            ApplyBindings(bindings);
        else
        {
            Debug.LogError("[TeleportMenuUI] Prefab layout cần TeleportMenuBindings (root hoặc con).");
            Destroy(_layoutInstanceRoot);
            _layoutInstanceRoot = null;
            _boundLayoutPrefab = null;
            RestoreUiDefaults();
        }
    }

    private void ApplyBindings(TeleportMenuBindings b)
    {
        if (b.panel != null) panel = b.panel;
        if (b.titleText != null) titleText = b.titleText;
        if (b.descriptionText != null) descriptionText = b.descriptionText;
        if (b.buttonContainer != null) buttonContainer = b.buttonContainer;
        if (b.closeButton != null) closeButton = b.closeButton;
        if (b.buttonTemplate != null) buttonTemplate = b.buttonTemplate;
    }

    // ================= OPEN (PILLAR LIST) =================

    /// <summary>
    /// Mở menu danh sách trụ đích (loại trừ trụ hiện tại).
    /// </summary>
    public void Open(TeleportPillar sourcePillar, List<TeleportPillar> destinations)
    {
        ResolveMenuLayout(sourcePillar);

        if (panel == null)
        {
            Debug.LogError("[TeleportMenuUI] Panel chưa được gán! Gán trên TeleportMenuBindings trong prefab hoặc hierarchy do tool tạo.");
            return;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (buttonTemplate != null) buttonTemplate.SetActive(false);

        currentPillar = sourcePillar;

        if (titleText != null)
            titleText.text = sourcePillar != null && !string.IsNullOrWhiteSpace(sourcePillar.pillarName)
                ? sourcePillar.pillarName
                : "CHỌN TRỤ DỊCH CHUYỂN";

        mapMenuFooterDescription = "Chọn trụ đích để dịch chuyển.";
        if (descriptionText != null)
            descriptionText.text = mapMenuFooterDescription;

        ClearButtons();

        if (destinations == null || destinations.Count == 0)
        {
            CreateInfoButton("Chưa có trụ đích", "Hãy tạo thêm trụ teleport khác trong scene.");
        }
        else
        {
            for (int i = 0; i < destinations.Count; i++)
            {
                if (destinations[i] != null)
                    CreateDestinationButton(destinations[i], i);
            }
        }

        panel.SetActive(true);
        SFXManager.Instance?.PlayPanelOpen();
        InputController.Instance?.SetMode(InputMode.UI);

        Debug.Log($"[TeleportMenuUI] Mở pillar menu: {(sourcePillar != null ? sourcePillar.pillarName : "?")} ({destinations?.Count ?? 0} trụ)");
    }

    // ================= OPEN (MAP LIST) =================

    /// <summary>
    /// Mở menu danh sách map (availableMaps). Một map → một nút.
    /// </summary>
    public void OpenMapMenu(TeleportPillar sourcePillar)
    {
        ResolveMenuLayout(sourcePillar);

        if (panel == null)
        {
            Debug.LogError("[TeleportMenuUI] Panel chưa được gán! Gán trên TeleportMenuBindings trong prefab hoặc hierarchy do tool tạo.");
            return;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (buttonTemplate != null) buttonTemplate.SetActive(false);

        currentPillar = sourcePillar;

        int mapCount = 0;
        if (availableMaps != null)
        {
            for (int i = 0; i < availableMaps.Count; i++)
            {
                if (availableMaps[i] != null) mapCount++;
            }
        }

        if (titleText != null)
        {
            string pname = sourcePillar != null && !string.IsNullOrWhiteSpace(sourcePillar.pillarName)
                ? sourcePillar.pillarName
                : "CHỌN MAP";
            titleText.text = pname;
        }

        if (mapCount == 0)
            mapMenuFooterDescription = "Chưa cấu hình map trong TeleportMenuUI.";
        else if (mapCount == 1)
            mapMenuFooterDescription = "Khu vực hiện có — chọn để dịch chuyển.";
        else
            mapMenuFooterDescription = "Chọn khu vực bạn muốn dịch chuyển tới.";

        if (descriptionText != null)
            descriptionText.text = mapMenuFooterDescription;

        // Xóa nút cũ
        ClearButtons();

        // Nếu không có map nào trong danh sách → hiển thị thông báo
        if (availableMaps == null || availableMaps.Count == 0)
        {
            CreateInfoButton("Chưa có khu vực khả dụng", "Hãy thêm Mapdata vào danh sách availableMaps trong TeleportMenuUI.");
        }
        else
        {
            // Sinh nút cho từng map
            for (int i = 0; i < availableMaps.Count; i++)
            {
                if (availableMaps[i] != null)
                {
                    CreateMapButton(availableMaps[i], i);
                }
            }
        }

        // Mở panel + chặn di chuyển
        panel.SetActive(true);
        SFXManager.Instance?.PlayPanelOpen();
        InputController.Instance?.SetMode(InputMode.UI);

        Debug.Log($"[TeleportMenuUI] Mở map menu từ trụ: {(sourcePillar != null ? sourcePillar.pillarName : "?")} ({mapCount} map)");
    }

    // ================= CLOSE =================

    public void Close()
    {
        SFXManager.Instance?.PlayPanelClose();

        if (panel != null) panel.SetActive(false);
        ClearButtons();
        currentPillar = null;

        InputController.Instance?.SetMode(InputMode.Map);
        Debug.Log("[TeleportMenuUI] Đóng menu.");
    }

    // ================= TELEPORT (ĐẾN TRỤ) =================

    private void TeleportToPillar(TeleportPillar targetPillar)
    {
        if (Time.time - lastTeleportTime < 0.5f) return;

        if (targetPillar == null)
        {
            Debug.LogWarning("[TeleportMenuUI] Trụ đích = null!");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[TeleportMenuUI] Không tìm thấy Player!");
            return;
        }

        Close();

        if (teleportDelay > 0)
            StartCoroutine(TeleportAfterDelay(player.transform, targetPillar));
        else
            ExecuteTeleport(player.transform, targetPillar);
    }

    private System.Collections.IEnumerator TeleportAfterDelay(Transform playerTransform, TeleportPillar targetPillar)
    {
        yield return new WaitForSeconds(teleportDelay);
        ExecuteTeleport(playerTransform, targetPillar);
    }

    private void ExecuteTeleport(Transform playerTransform, TeleportPillar targetPillar)
    {
        // Dời Player tới vị trí spawn của trụ đích
        playerTransform.position = targetPillar.SpawnPosition;
        lastTeleportTime = Time.time;

        Debug.Log($"[TeleportMenuUI] Dịch chuyển tới: {targetPillar.pillarName} ({targetPillar.SpawnPosition})");

        // Cập nhật MapData nếu trụ đích có gán Mapdata
        if (targetPillar.mapData != null && MapManager.Instance != null)
        {
            if (MapManager.Instance.currentMap != targetPillar.mapData)
            {
                MapManager.Instance.SetMap(targetPillar.mapData);
            }
        }
    }

    // ================= TELEPORT (MAP) =================

    private void TeleportToMap(Mapdata targetMap)
    {
        if (Time.time - lastTeleportTime < 0.5f) return;

        if (targetMap == null)
        {
            Debug.LogWarning("[TeleportMenuUI] Map đích = null!");
            return;
        }

        // Kiểm tra xem map đích có phải map hiện tại không
        if (MapManager.Instance != null && MapManager.Instance.currentMap == targetMap)
        {
            if (descriptionText != null)
                descriptionText.text = "Bạn đang ở khu vực này rồi!";
            Debug.Log($"[TeleportMenuUI] Đã ở map: {targetMap.mapName}");
            return;
        }

        // Tìm trụ tương ứng với map đích (nếu có)
        TeleportPillar targetPillar = FindPillarForMap(targetMap);

        if (targetPillar != null)
        {
            // Có trụ → dịch chuyển tới trụ đó
            Close();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (teleportDelay > 0)
                    StartCoroutine(TeleportAfterDelay(player.transform, targetPillar));
                else
                    ExecuteTeleport(player.transform, targetPillar);
            }
        }
        else
        {
            // Không có trụ → chỉ đổi MapData
            Close();
            if (MapManager.Instance != null)
            {
                MapManager.Instance.SetMap(targetMap);
                Debug.Log($"[TeleportMenuUI] Đổi map sang: {targetMap.mapName} (không có trụ đích)");
            }
        }

        lastTeleportTime = Time.time;
    }

    /// <summary>
    /// Tìm trụ teleport gắn với map chỉ định.
    /// </summary>
    private TeleportPillar FindPillarForMap(Mapdata map)
    {
        foreach (var pillar in TeleportPillar.GetAllPillars())
        {
            if (pillar != null && pillar != currentPillar && pillar.mapData == map)
                return pillar;
        }
        return null;
    }

    // ================= UI GENERATION =================

    private void CreateDestinationButton(TeleportPillar targetPillar, int index)
    {
        if (buttonTemplate == null || buttonContainer == null) return;

        GameObject btnObj = Instantiate(buttonTemplate, buttonContainer);
        btnObj.name = $"Btn_Pillar_{index}_{targetPillar.pillarName}";
        btnObj.SetActive(true);

        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            string mapLabel = targetPillar.mapData != null ? targetPillar.mapData.mapName : "Map chưa gán";
            string label = $"{targetPillar.pillarName}  <size=80%><color=#aaa>({mapLabel})</color></size>";
            btnText.text = label;
        }

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            TeleportPillar captured = targetPillar;
            btn.onClick.AddListener(() => TeleportToPillar(captured));

            var trigger = btnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((_) =>
            {
                if (descriptionText != null)
                {
                    string desc = !string.IsNullOrWhiteSpace(captured.description) ? captured.description : $"Trụ: {captured.pillarName}";
                    if (captured.mapData != null) desc += $"\nMap: {captured.mapData.mapName}";
                    descriptionText.text = desc;
                }
            });
            trigger.triggers.Add(pointerEnter);

            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((_) =>
            {
                if (descriptionText != null)
                    descriptionText.text = mapMenuFooterDescription;
            });
            trigger.triggers.Add(pointerExit);
        }

        spawnedButtons.Add(btnObj);
    }

    /// <summary>
    /// Tạo nút cho một map trong danh sách.
    /// </summary>
    private void CreateMapButton(Mapdata mapData, int index)
    {
        if (buttonTemplate == null || buttonContainer == null) return;

        GameObject btnObj = Instantiate(buttonTemplate, buttonContainer);
        btnObj.name = $"Btn_Map_{index}_{mapData.mapName}";
        btnObj.SetActive(true);

        // Text
        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            string label = mapData.mapName;
            label += $"  <size=80%><color=#aaa>(Lv.{mapData.enemyLevel})</color></size>";

            // Đánh dấu map hiện tại
            if (MapManager.Instance != null && MapManager.Instance.currentMap == mapData)
            {
                label += $"  <size=70%><color=#6f6>[Đang ở đây]</color></size>";
            }

            btnText.text = label;
        }

        // Click
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            Mapdata captured = mapData;
            btn.onClick.AddListener(() => TeleportToMap(captured));

            // Hover → hiện mô tả
            var trigger = btnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((_) =>
            {
                if (descriptionText != null)
                {
                    string desc = $"Khu vực: {captured.mapName}";
                    desc += $"\nCấp độ quái: {captured.enemyLevel}";
                    if (captured.possibleEnemies != null && captured.possibleEnemies.Count > 0)
                        desc += $"\nSố loại quái: {captured.possibleEnemies.Count}";
                    descriptionText.text = desc;
                }
            });
            trigger.triggers.Add(pointerEnter);

            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((_) =>
            {
                if (descriptionText != null)
                    descriptionText.text = mapMenuFooterDescription;
            });
            trigger.triggers.Add(pointerExit);
        }

        spawnedButtons.Add(btnObj);
    }

    /// <summary>
    /// Tạo nút thông tin (không có chức năng teleport, chỉ hiển thị text).
    /// </summary>
    private void CreateInfoButton(string label, string hoverDescription)
    {
        if (buttonTemplate == null || buttonContainer == null) return;

        GameObject btnObj = Instantiate(buttonTemplate, buttonContainer);
        btnObj.name = "Btn_Info";
        btnObj.SetActive(true);

        // Text
        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            btnText.text = $"<color=#888>{label}</color>";
        }

        // Disable button interaction (chỉ hiển thị, không bấm được)
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = false;

            // Hover → hiện mô tả
            var trigger = btnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((_) =>
            {
                if (descriptionText != null)
                    descriptionText.text = hoverDescription;
            });
            trigger.triggers.Add(pointerEnter);
        }

        spawnedButtons.Add(btnObj);
    }

    private void ClearButtons()
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        spawnedButtons.Clear();
    }

    // ================= INPUT =================

    void Update()
    {
        if (panel != null && panel.activeSelf)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[Key.Escape].wasPressedThisFrame)
            {
                Close();
            }
        }
    }
}
