using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Menu UI chọn điểm đến khi tương tác với TeleportPillar.
/// Tự động sinh nút bấm dựa trên danh sách các trụ khác trong scene.
/// Nhấn Esc hoặc nút Đóng để thoát menu.
///
/// Setup tự động: Tools/Teleport Menu/1. Tạo Teleport Menu UI Tự Động
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

    [Header("Teleport Settings")]
    [Tooltip("Thời gian delay nhỏ trước khi teleport (giây). 0 = tức thì.")]
    public float teleportDelay = 0.15f;

    private TeleportPillar currentPillar;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    // Cooldown chống teleport lặp
    private static float lastTeleportTime = 0f;

    // ================= INIT =================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        closeButton?.onClick.AddListener(Close);

        if (buttonTemplate != null) buttonTemplate.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    // ================= OPEN =================

    /// <summary>
    /// Mở menu với danh sách các trụ đích (đã loại bỏ trụ hiện tại).
    /// </summary>
    public void Open(TeleportPillar sourcePillar, List<TeleportPillar> destinations)
    {
        if (panel == null)
        {
            Debug.LogError("[TeleportMenuUI] Panel chưa được gán!");
            return;
        }

        currentPillar = sourcePillar;

        // Tiêu đề
        if (titleText != null)
            titleText.text = sourcePillar.pillarName;

        // Mô tả mặc định
        if (descriptionText != null)
            descriptionText.text = "Chọn điểm đến để dịch chuyển.";

        // Xóa nút cũ
        ClearButtons();

        // Sinh nút cho từng trụ đích
        for (int i = 0; i < destinations.Count; i++)
        {
            CreateDestinationButton(destinations[i], i);
        }

        // Mở panel + chặn di chuyển
        panel.SetActive(true);
        InputController.Instance?.SetMode(InputMode.UI);

        Debug.Log($"[TeleportMenuUI] Mở menu: {sourcePillar.pillarName} ({destinations.Count} điểm đến)");
    }

    // ================= CLOSE =================

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        ClearButtons();
        currentPillar = null;

        InputController.Instance?.SetMode(InputMode.Map);
        Debug.Log("[TeleportMenuUI] Đóng menu.");
    }

    // ================= TELEPORT =================

    private void TeleportTo(TeleportPillar targetPillar)
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

        // Đóng menu trước
        Close();

        // Dịch chuyển
        if (teleportDelay > 0)
        {
            StartCoroutine(TeleportAfterDelay(player.transform, targetPillar));
        }
        else
        {
            ExecuteTeleport(player.transform, targetPillar);
        }
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

    // ================= UI GENERATION =================

    private void CreateDestinationButton(TeleportPillar targetPillar, int index)
    {
        if (buttonTemplate == null || buttonContainer == null) return;

        GameObject btnObj = Instantiate(buttonTemplate, buttonContainer);
        btnObj.name = $"Btn_Dest_{index}_{targetPillar.pillarName}";
        btnObj.SetActive(true);

        // Text
        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            string label = targetPillar.pillarName;

            if (targetPillar.mapData != null)
            {
                label += $"  <size=80%><color=#aaa>(Lv.{targetPillar.mapData.enemyLevel})</color></size>";
            }

            btnText.text = label;
        }

        // Click
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            TeleportPillar captured = targetPillar;
            btn.onClick.AddListener(() => TeleportTo(captured));

            // Hover → hiện mô tả
            var trigger = btnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((_) =>
            {
                if (descriptionText != null && !string.IsNullOrEmpty(captured.description))
                    descriptionText.text = captured.description;
            });
            trigger.triggers.Add(pointerEnter);

            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((_) =>
            {
                if (descriptionText != null)
                    descriptionText.text = "Chọn điểm đến để dịch chuyển.";
            });
            trigger.triggers.Add(pointerExit);
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
