using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD cho mỗi unit trong battle. Hỗ trợ Image.fillAmount (style Tutorial)
/// cho HP/AP thay vì Slider.
/// </summary>
public class UnitHUD : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI apText;

    [Header("HP Bar (Image fillAmount)")]
    [SerializeField] private Image hpFillImage;

    [Header("AP Dots (BP_Dot style)")]
    [Tooltip("Mảng các dot AP theo thứ tự — mỗi dot sáng lên khi currentAP >= (i+1) * apPerDot.")]
    [SerializeField] private GameObject[] apDots;
    [Tooltip("Lượng AP mỗi dot đại diện (1 dot = 1 AP, maxAP = 5).")]
    [SerializeField] private int apPerDot = 1;

    [Header("Visual")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color highlightActiveColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Animation")]
    [SerializeField] private float fillLerpSpeed = 5f;

    private float targetFill = 0f;

    void Awake() => SetEmpty();

    void Update()
    {
        if (hpFillImage == null || Mathf.Approximately(hpFillImage.fillAmount, targetFill)) return;
        hpFillImage.fillAmount = Mathf.MoveTowards(
            hpFillImage.fillAmount, targetFill, fillLerpSpeed * Time.deltaTime);
    }

    /// <summary>Wire HP fill và text từ bên ngoài (dùng khi gắn UnitHUD vào Enemy_HP_Canvas lúc runtime).</summary>
    public void SetHPFill(Image fill, TMPro.TextMeshPro worldTMP = null)
    {
        hpFillImage  = fill;
        _worldHPText = worldTMP;
        if (fill == null) Debug.LogWarning("[UnitHUD] SetHPFill: fill image is NULL.");
        // fillAmount và targetFill được set bởi UpdateHUD ngay sau đó
    }

    private TMPro.TextMeshPro _worldHPText;

    public void SetEmpty()
    {
        if (nameText != null) nameText.text = string.Empty;
        if (hpText != null)   hpText.text   = string.Empty;
        if (apText != null)   apText.text   = string.Empty;
        targetFill = 0f;
        if (hpFillImage != null) hpFillImage.fillAmount = 0f;
        SetAPDots(0, 5);
        SetHighlight(false);
    }

    public void UpdateHUD(string unitName, int currentHP, int maxHP, int currentAP = -1, int maxAP = -1, bool instant = false)
    {
        if (nameText != null) nameText.text = unitName;

        targetFill = maxHP > 0 ? (float)currentHP / maxHP : 0f;
        // Luôn set trực tiếp — đảm bảo hiển thị đúng kể cả khi Update() không chạy
        if (hpFillImage != null) hpFillImage.fillAmount = instant ? targetFill
            : Mathf.MoveTowards(hpFillImage.fillAmount, targetFill, 0.5f);

        if (hpText != null)
            hpText.text = $"{currentHP}/{maxHP}";
        if (_worldHPText != null)
            _worldHPText.text = $"{currentHP}";

        if (currentAP >= 0 && maxAP > 0)
        {
            SetAPDots(currentAP, maxAP);
            if (apText != null)
            {
                apText.gameObject.SetActive(true);
                apText.text = $"AP:{currentAP}/{maxAP}";
            }
        }
        else
        {
            SetAPDots(0, 1);
            if (apText != null) apText.gameObject.SetActive(false);
        }
    }

    public void SetHighlight(bool active)
    {
        if (highlightImage == null) return;
        var c = highlightActiveColor;
        c.a = active ? highlightActiveColor.a : 0f;
        highlightImage.color = c;
    }

    static void SetDotLit(GameObject dot, bool lit)
    {
        var img = dot.GetComponent<UnityEngine.UI.Image>();
        if (img == null) return;
        var c = img.color;
        c.a = lit ? 1f : 0.25f;
        img.color = c;
    }

    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null) backgroundImage.color = color;
    }

    void SetAPDots(int currentAP, int maxAP)
    {
        if (apDots == null || apDots.Length == 0) return;
        for (int i = 0; i < apDots.Length; i++)
        {
            if (apDots[i] == null) continue;

            // Dot background luôn hiện
            apDots[i].SetActive(true);

            // Tìm child glow (tên có thể là Yellow_Glow, Glow, hoặc AP_Glow)
            bool lit = currentAP >= (i + 1) * apPerDot;
            Transform glow = apDots[i].transform.Find("Yellow_Glow")
                          ?? apDots[i].transform.Find("Glow")
                          ?? apDots[i].transform.Find("AP_Glow");
            if (glow != null)
                glow.gameObject.SetActive(lit);
            else
                // Fallback: nếu không có child glow, bật tắt Image color của dot
                SetDotLit(apDots[i], lit);
        }
    }
}
