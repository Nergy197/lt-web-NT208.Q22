using UnityEngine;

public class ObjectFader : MonoBehaviour
{
    [Header("Fading Settings")]
    [Range(0, 1)] public float fadeAlpha = 0.5f; // Độ mờ khi nấp sau
    public float fadeSpeed = 5f;

    private SpriteRenderer sr;
    private float targetAlpha = 1f;

    void Start() {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update() {
        if (sr == null) return;
        // Chỉnh Alpha mượt mà
        Color curColor = sr.color;
        float newAlpha = Mathf.MoveTowards(curColor.a, targetAlpha, fadeSpeed * Time.deltaTime);
        sr.color = new Color(curColor.r, curColor.g, curColor.b, newAlpha);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        // QUAN TRỌNG: Player của ông phải có Tag là "Player"
        if (collision.CompareTag("Player")) {
            targetAlpha = fadeAlpha;
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            targetAlpha = 1f;
        }
    }
}