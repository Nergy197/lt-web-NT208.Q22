using UnityEngine;

public class MapTrigger : MonoBehaviour
{
    public MapAction action;
    public bool triggerOnce = true;
    
    [Tooltip("Check nếu muốn Player tới gần phải bấm F mới kích hoạt (dùng cho rương, cửa). Tắt đi nếu muốn đạp lên tự kích hoạt (gài trap, chuyển map).")]
    public bool requireInteract = false;

    private bool triggered = false;
    private bool isPlayerInRange = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        if (requireInteract)
        {
            isPlayerInRange = true;
            Debug.Log($"[MapTrigger] Nhấn F để tương tác ({gameObject.name})");
        }
        else
        {
            ExecuteTrigger();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInRange = false;
    }

    private void Update()
    {
        if (triggered) return;

        if (requireInteract && isPlayerInRange && InputController.Instance != null)
        {
            if (InputController.Instance.Input.Map.Interact.WasPressedThisFrame())
            {
                ExecuteTrigger();
            }
        }
    }

    private void ExecuteTrigger()
    {
        action?.Execute();

        if (triggerOnce)
            triggered = true;
    }
}
