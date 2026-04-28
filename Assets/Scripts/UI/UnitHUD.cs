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
}
