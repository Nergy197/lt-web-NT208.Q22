using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPromptUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject panelObject;
    [SerializeField] private Image panelBackground;

    private Coroutine activeFlashCoroutine;

    void Awake()
    {
        if (panelObject == null) panelObject = gameObject;
    }

    public void Show(string text)
    {
        StopActiveCoroutine();
        if (panelObject != null) panelObject.SetActive(true);
        if (promptText != null)
        {
            promptText.text = text;
            promptText.color = Color.white;
            promptText.fontSize = 24f;
        }
    }

    public void Hide()
    {
        StopActiveCoroutine();
        if (panelObject != null) panelObject.SetActive(false);
    }

    public void FlashParry()
    {
        StopActiveCoroutine();
        if (panelObject != null) panelObject.SetActive(true);
        activeFlashCoroutine = StartCoroutine(FlashRoutine(
            ">>> NHẤN [SPACE] ĐỂ ĐỠ ĐÒN! <<<",
            Color.yellow, new Color(1f, 0.4f, 0f), duration: 1.6f));
    }

    private void StopActiveCoroutine()
    {
        if (activeFlashCoroutine != null)
        {
            StopCoroutine(activeFlashCoroutine);
            activeFlashCoroutine = null;
        }
    }

    private IEnumerator FlashRoutine(string text, Color colorA, Color colorB, float duration = 1.6f)
    {
        if (promptText == null) yield break;

        promptText.text = text;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float factor = (Mathf.Sin(elapsed * 12f) + 1f) / 2f;
            promptText.color    = Color.Lerp(colorA, colorB, factor);
            promptText.fontSize = Mathf.Lerp(22f, 28f, factor);
            elapsed += Time.deltaTime;
            yield return null;
        }

        promptText.color    = Color.white;
        promptText.fontSize = 22f;
    }
}
