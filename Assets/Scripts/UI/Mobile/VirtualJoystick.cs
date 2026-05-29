using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Joystick ảo cho mobile. Gắn vào Background Image của joystick.
/// Setup: Background (outer circle) → Handle (inner circle, child của Background).
/// Kéo Handle RectTransform vào field "handle".
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] [Range(0f, 0.3f)] private float deadZone = 0.1f;

    public Vector2 InputVector { get; private set; }
    public bool IsActive { get; private set; }

    public event System.Action<Vector2> OnInputChanged;

    private RectTransform bg;
    private float radius;

    void Awake()
    {
        bg = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData data)
    {
        IsActive = true;
        radius = bg.rect.width * 0.5f;
        ProcessDrag(data);
    }

    public void OnDrag(PointerEventData data)
    {
        ProcessDrag(data);
    }

    public void OnPointerUp(PointerEventData data)
    {
        IsActive = false;
        handle.anchoredPosition = Vector2.zero;
        InputVector = Vector2.zero;
        OnInputChanged?.Invoke(Vector2.zero);
    }

    void ProcessDrag(PointerEventData data)
    {
        if (radius <= 0f) return; // canvas chưa layout xong → tránh chia cho 0

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                bg, data.position, data.pressEventCamera, out Vector2 local))
            return;

        Vector2 clamped = Vector2.ClampMagnitude(local, radius);
        handle.anchoredPosition = clamped;

        Vector2 raw = clamped / radius;
        InputVector = raw.magnitude < deadZone ? Vector2.zero : raw;
        OnInputChanged?.Invoke(InputVector);
    }
}
