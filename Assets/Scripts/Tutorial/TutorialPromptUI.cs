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
        activeFlashCoroutine = StartCoroutine(FlashParryRoutine());
    }

    private void StopActiveCoroutine()
    {
        if (activeFlashCoroutine != null)
        {
            StopCoroutine(activeFlashCoroutine);
            activeFlashCoroutine = null;
        }
    }

    private IEnumerator FlashParryRoutine()
    {
        if (promptText == null) yield break;

        promptText.text = "BAM SPACE DE DO DON (PARRY)!";
        float duration = 1.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float speed = 15f;
            float factor = (Mathf.Sin(elapsed * speed) + 1f) / 2f;
            
            promptText.color = Color.Lerp(Color.yellow, Color.red, factor);
            promptText.fontSize = Mathf.Lerp(24f, 30f, factor);

            elapsed += Time.deltaTime;
            yield return null;
        }

        promptText.color = Color.white;
        promptText.fontSize = 24f;
    }
}
