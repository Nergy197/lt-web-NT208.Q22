using UnityEngine;

public class PlayerMovement_Cutscene : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public float moveSpeed = 8f; // Giảm xuống chút cho mượt anh nhé
    public bool canMove = true;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 mobileMovement;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (!canMove)
        {
            movement = Vector2.zero;
            if (anim != null) anim.speed = 0f;
            return;
        }

        // Đọc phím bấm hoặc joystick mobile (Cho phép đi đủ 4 hướng)
        if (mobileMovement.sqrMagnitude > 0.0001f)
        {
            movement = mobileMovement;
        }
        else if (InputController.Instance != null)
        {
            movement = InputController.Instance.Input.Map.Moves.ReadValue<Vector2>();
        }
        else
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }

        if (movement != Vector2.zero)
        {
            anim.SetFloat("MoveX", movement.x);
            anim.SetFloat("MoveY", movement.y);
            anim.speed = 1f;
        }
        else
        {
            anim.speed = 0f;
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    public void SetMobileInput(Vector2 input)
    {
        mobileMovement = input;
    }
}