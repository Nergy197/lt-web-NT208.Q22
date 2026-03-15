using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StartMenuUI : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject saveSlotsPanel; // Panel chứa danh sách 4 slot kiểu Hollow Knight

    [Header("Main Menu Buttons")]
    public Button startGameButton;
    // Bỏ nút LoadGame vì Hollow Knight gộp chung vào màn hình Save Slots
    public Button quitButton;

    [Header("Save Slots Settings")]
    public Button closeSlotsPanelButton;
    public Transform slotsContainer;
    public GameObject slotButtonPrefab;

    void Start()
    {
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (closeSlotsPanelButton != null)
            closeSlotsPanelButton.onClick.AddListener(OnCloseSlotsPanel);

        // Khởi tạo trạng thái ban đầu
        mainMenuPanel.SetActive(true);
        if (saveSlotsPanel != null) saveSlotsPanel.SetActive(false);
    }

    // Mở hòm save như lúc ấn nút "Start Game" của Hollow Knight
    void OnStartGameClicked()
    {
        mainMenuPanel.SetActive(false);
        saveSlotsPanel.SetActive(true);
        
        // Ẩn Title nếu nó nằm ngoài MainMenuPanel
        Transform titleObj = transform.Find("Title");
        if (titleObj != null) titleObj.gameObject.SetActive(false);

        PopulateSaveSlots();
    }

    void OnCloseSlotsPanel()
    {
        mainMenuPanel.SetActive(true);
        saveSlotsPanel.SetActive(false);

        // Hiện lại Title
        Transform titleObj = transform.Find("Title");
        if (titleObj != null) titleObj.gameObject.SetActive(true);
    }

    void OnQuitClicked()
    {
        Debug.Log("Quit Game...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void PopulateSaveSlots()
    {
        PlayerSave[] slots = GameManager.Instance.GetAllSaveSlotsMetadata();

        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }

        // Hollow Knight thường để 4 slot bự
        for (int i = 0; i < 4; i++)
        {
            GameObject btnObj = Instantiate(slotButtonPrefab, slotsContainer);
            btnObj.SetActive(true);
            Button btn = btnObj.GetComponent<Button>();
            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();

            int slotIndex = i;
            var saveData = slots[i];

            if (saveData != null)
            {
                // Slot đã có lưu game -> Ấn vô là Load
                string time = string.IsNullOrEmpty(saveData.saveTime) ? "Không có mốc thời gian" : saveData.saveTime;
                string location = string.IsNullOrEmpty(saveData.lastSavePointId) ? "Khu vực khởi đầu" : saveData.lastSavePointId;
                
                string info = "Lv 1";
                if (saveData.party != null && saveData.party.Count > 0)
                {
                    info = $"{saveData.party[0].entityName} (Lv {saveData.party[0].level})";
                }

                // Phong cách tối giản: Tên bự, bên dưới là thông tin nhỏ
                txt.text = $"<size=120%><b>SLOT {slotIndex + 1}</b></size>\n<color=#dddddd>{location} - {info}</color>\n<size=80%><color=#aaaaaa>{time}</color></size>";
                
                btn.onClick.AddListener(() => OnSlotSelected(slotIndex, false));
            }
            else
            {
                // Slot trống -> Ấn vô là Start New Game
                txt.text = $"<size=120%><b>SLOT {slotIndex + 1}</b></size>\n<color=#bbbbbb>Bắt đầu hành trình mới</color>";
                btn.onClick.AddListener(() => OnSlotSelected(slotIndex, true));
            }
        }
    }

    void OnSlotSelected(int slotIndex, bool isNewGame)
    {
        GameManager.Instance.currentSaveSlot = slotIndex;

        if (isNewGame)
        {
            Debug.Log($"[StartMenu] Bắt đầu New Game ở slot thứ {slotIndex}!");
            GameManager.Instance.PrepareNewGame();
        }
        else
        {
            Debug.Log($"[StartMenu] Load lại game cũ ở slot thứ {slotIndex}!");
        }

        GameManager.Instance.LoadAndStartGame();
    }
}
