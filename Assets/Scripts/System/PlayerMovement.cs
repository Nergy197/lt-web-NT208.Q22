using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    private InputController input;
    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 move;

    public float speed = 5f;
    [Header("Map Bounds")]
    public bool constrainToMapBounds = true;
    [Range(0f, 1f)] public float boundsPadding = 0.05f;

    // Lưu reference delegate để unsubscribe khi Destroy (tránh input leak)
    private System.Action<InputAction.CallbackContext> onMovePerformed;
    private System.Action<InputAction.CallbackContext> onMoveCanceled;
    private Bounds worldBounds;
    private bool hasWorldBounds = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Debug.Log($"[PlayerMovement] Awake — {gameObject.name}");
    }

    void Start()
    {
        input = InputController.Instance;

        if (input == null)
        {
            Debug.LogError("[PlayerMovement] InputController.Instance == NULL — chưa có Bootstrap?");
            return;
        }

        onMovePerformed = ctx =>
        {
            move = ctx.ReadValue<Vector2>();

            // UPDATE ANIMATION
            UpdateAnimation();

            GameLog.Log("MOVE INPUT: " + move);
        };

        onMoveCanceled = ctx =>
        {
            move = Vector2.zero;

            // UPDATE ANIMATION
            UpdateAnimation();
        };

        input.Input.Map.Moves.performed += onMovePerformed;
        input.Input.Map.Moves.canceled += onMoveCanceled;

        // Khôi phục vị trí sau Battle
        if (GameManager.Instance != null &&
            GameManager.Instance.TryGetLastMapPosition(out Vector2 savedPos))
        {
            transform.position = savedPos;
            GameManager.Instance.ClearMapPosition();
            Debug.Log($"[PlayerMovement] Restored position: {savedPos}");
        }
        // Khôi phục vị trí Save Point khi mới load game hoặc respawn sau khi chết
        else if (GameManager.Instance != null &&
                 !string.IsNullOrEmpty(GameManager.Instance.pendingSavePointId))
        {
            string targetId = GameManager.Instance.pendingSavePointId;
            SavePoint[] allPoints = Object.FindObjectsByType<SavePoint>(FindObjectsSortMode.None);

            foreach (var sp in allPoints)
            {
                if (sp.pointId == targetId)
                {
                    transform.position = sp.transform.position;
                    Debug.Log($"[PlayerMovement] Teleported to save point: {targetId}");
                    break;
                }
            }

            // Không gọi ConsumeSavePoint() — giữ lại để nếu chết tiếp vẫn respawn đúng chỗ
        }

        Debug.Log("PLAYER READY");
        CollectWorldBounds();
    }

    void OnDestroy()
    {
        // Unsubscribe để tránh input leak khi scene reload
        if (input != null && input.Input != null)
        {
            if (onMovePerformed != null) input.Input.Map.Moves.performed -= onMovePerformed;
            if (onMoveCanceled != null) input.Input.Map.Moves.canceled -= onMoveCanceled;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = move * speed;
        ClampInsideMapBounds();

        // đảm bảo update liên tục
        UpdateAnimation();

        // Random encounter: tối đa 1 roll / FixedUpdate khi đang di chuyển (tránh spam theo từng input performed).
        if (move.sqrMagnitude > 0.0001f)
            TryEncounter();
    }

    /// <summary>Gọi từ MobileInputUI để feed joystick vào hệ thống di chuyển.</summary>
    public void SetMobileInput(Vector2 v)
    {
        move = v;
        UpdateAnimation();
    }

    // ================= ANIMATION =================

    void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = move.sqrMagnitude > 0.0001f;
        animator.SetFloat("MoveX", move.x);
        animator.SetFloat("MoveY", move.y);
        animator.speed = isMoving ? 1f : 0f;
    }


    // ================= ENCOUNTER =================

    void TryEncounter()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogWarning("MapManager NULL");
            return;
        }

        MapManager.Instance.CheckForEncounter();
    }

    void ClampInsideMapBounds()
    {
        if (!constrainToMapBounds || !hasWorldBounds) return;

        Vector2 pos = rb.position;
        float minX = worldBounds.min.x + boundsPadding;
        float maxX = worldBounds.max.x - boundsPadding;
        float minY = worldBounds.min.y + boundsPadding;
        float maxY = worldBounds.max.y - boundsPadding;

        float clampedX = Mathf.Clamp(pos.x, minX, maxX);
        float clampedY = Mathf.Clamp(pos.y, minY, maxY);

        if (!Mathf.Approximately(pos.x, clampedX) || !Mathf.Approximately(pos.y, clampedY))
        {
            rb.position = new Vector2(clampedX, clampedY);

            // Nếu đang đẩy ra ngoài thì triệt velocity theo trục vi phạm để tránh rung.
            Vector2 v = rb.linearVelocity;
            if ((pos.x < minX && v.x < 0f) || (pos.x > maxX && v.x > 0f)) v.x = 0f;
            if ((pos.y < minY && v.y < 0f) || (pos.y > maxY && v.y > 0f)) v.y = 0f;
            rb.linearVelocity = v;
        }
    }

    void CollectWorldBounds()
    {
        hasWorldBounds = false;

        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tm = tilemaps[i];
            if (tm == null || !tm.gameObject.activeInHierarchy) continue;
            if (tm.cellBounds.size == Vector3Int.zero) continue;

            tm.CompressBounds();
            BoundsInt cb = tm.cellBounds;
            Vector3 worldMin = tm.CellToWorld(cb.min);
            Vector3 worldMax = tm.CellToWorld(cb.max);
            Bounds b = new Bounds();
            b.SetMinMax(Vector3.Min(worldMin, worldMax), Vector3.Max(worldMin, worldMax));

            if (!hasWorldBounds)
            {
                worldBounds = b;
                hasWorldBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(b);
            }
        }

        if (!hasWorldBounds)
        {
            SpriteRenderer[] srs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            for (int i = 0; i < srs.Length; i++)
            {
                if (!srs[i].enabled || !srs[i].gameObject.activeInHierarchy) continue;
                if (!hasWorldBounds)
                {
                    worldBounds = srs[i].bounds;
                    hasWorldBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(srs[i].bounds);
                }
            }
        }
    }
}