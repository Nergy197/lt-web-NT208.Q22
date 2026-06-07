using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn component này vào bất kỳ GameObject nào có Button.
/// Tự động phát SFX khi button được click.
///
/// Cách dùng:
///   1. Chọn Button trong Hierarchy
///   2. Add Component → ButtonSFX
///   3. Xong! Tự động dùng SFXManager.Instance.buttonClick
///
/// Nếu muốn dùng âm thanh riêng cho button đặc biệt, kéo clip vào [overrideClip].
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    [Tooltip("Để trống = dùng SFXManager.buttonClick mặc định.\nKéo clip vào đây nếu muốn button này dùng âm thanh riêng.")]
    public AudioClip overrideClip;

    void Awake()
    {
        var button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (SFXManager.Instance == null) return;

        if (overrideClip != null)
            SFXManager.Instance.PlayClip(overrideClip);
        else
            SFXManager.Instance.PlayButtonClick();
    }
}
