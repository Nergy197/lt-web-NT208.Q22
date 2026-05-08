using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component UI cho moi unit: hien thi ten, HP bar, AP bar.
/// Attach vao moi unit HUD prefab/panel.
/// </summary>
public class UnitHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider apSlider;
    [SerializeField] private TextMeshProUGUI apText;

    [Header("Visual")]
    [Tooltip("Anh nen panel (de phan biet player vs enemy hoac doi mau khi can).")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Anh vien highlight, an mac dinh; sang len khi unit den luot.")]
    [SerializeField] private Image highlightImage;

    [Tooltip("Mau highlight khi unit den luot.")]
    [SerializeField] private Color highlightActiveColor = new Color(1f, 0.85f, 0.2f, 1f);

    void Awake()
    {
        // Đảm bảo HUD luôn xuất hiện ở trạng thái rỗng cho tới khi BattleManager
        // gắn dữ liệu runtime (party/enemy có thể khác nhau theo map).
        SetEmpty();
    }

    /// <summary>
    /// Reset HUD về trạng thái rỗng (khung còn, dữ liệu rỗng).
    /// Dùng trước khi runtime data sẵn sàng để tránh hiển thị placeholder
    /// như tên cố định "Trần Quốc Tuấn" trong scene file.
    /// </summary>
    public void SetEmpty()
    {
        if (nameText != null) nameText.text = string.Empty;

        if (hpSlider != null)
        {
            hpSlider.maxValue = 1f;
            hpSlider.value = 0f;
        }
        if (hpText != null) hpText.text = string.Empty;

        if (apSlider != null)
        {
            apSlider.maxValue = 1f;
            apSlider.value = 0f;
            apSlider.gameObject.SetActive(false);
        }
        if (apText != null)
        {
            apText.text = string.Empty;
            apText.gameObject.SetActive(false);
        }

        SetHighlight(false);
    }

    public void UpdateHUD(string unitName, int currentHP, int maxHP, int currentAP = -1, int maxAP = -1)
    {
        if (nameText != null) nameText.text = unitName;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        if (hpText != null) hpText.text = $"{currentHP}/{maxHP}";

        // AP bar (chi hien cho player)
        if (currentAP >= 0 && maxAP > 0)
        {
            if (apSlider != null)
            {
                apSlider.gameObject.SetActive(true);
                apSlider.maxValue = maxAP;
                apSlider.value = currentAP;
            }
            if (apText != null)
            {
                apText.gameObject.SetActive(true);
                apText.text = $"AP: {currentAP}/{maxAP}";
            }
        }
        else
        {
            if (apSlider != null) apSlider.gameObject.SetActive(false);
            if (apText != null) apText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Bat/tat highlight (vi du khi unit den luot).
    /// </summary>
    public void SetHighlight(bool active)
    {
        if (highlightImage == null) return;
        Color c = highlightActiveColor;
        c.a = active ? highlightActiveColor.a : 0f;
        highlightImage.color = c;
    }

    /// <summary>
    /// Doi mau nen (de phan biet player/enemy hoac trang thai sap chet).
    /// </summary>
    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null) backgroundImage.color = color;
    }
}
