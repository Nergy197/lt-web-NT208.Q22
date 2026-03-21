using UnityEngine;

/// <summary>
/// Di chuyển nhân vật trên map (cutscene scene).
/// Dùng InputController.Input.Map.Moves khi có InputController,
/// fallback về Input.GetAxisRaw khi test trực tiếp scene.
/// canMove = false để khoá chân trong khi cutscene chạy.
/// </summary>
public class PlayerMovement_Cutscene : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public float moveSpeed = 12f;
    public bool canMove = true;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 lastDirection = Vector2.down;

    void Start()
    {
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (anim != null)
        {
            anim.SetFloat("MoveX", lastDirection.x);
            anim.SetFloat("MoveY", lastDirection.y);
        }
    }

    void Update()
    {
        if (!canMove)
        {
            return;
        }

        if (anim != null && anim.speed < 1f) anim.speed = 1f;

        var movement = DocInput();

        if (movement != Vector2.zero)
        {
            lastDirection = movement;
            if (anim != null)
            {
                anim.SetFloat("MoveX", movement.x);
                anim.SetFloat("MoveY", movement.y);
                anim.speed = 1f;
            }
        }
        else
        {
            if (anim != null)
            {
                anim.SetFloat("MoveX", lastDirection.x);
                anim.SetFloat("MoveY", lastDirection.y);
                anim.speed = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        if (!canMove) return;
        var movement = DocInput();
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    Vector2 DocInput()
    {
        var ic = InputController.Instance;
        if (ic != null)
            return ic.Input.Map.Moves.ReadValue<Vector2>();

        // Fallback khi test trực tiếp scene không qua StartScene
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
}
