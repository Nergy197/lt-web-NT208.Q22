using UnityEngine;

public class PlayerMovement_Cutscene : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public float moveSpeed = 12f;
    public bool canMove = true; // Công tắc khóa chân

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator anim;

    // Lưu hướng đi cuối cùng để khi đứng im vẫn hiện sprite đúng hướng
    private Vector2 lastDirection = Vector2.down;

    void Start()
    {
        // Lấy các thành phần cần thiết
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Khởi tạo hướng mặc định để Animator không bị trống frame
        if (anim != null)
        {
            anim.SetFloat("MoveX", lastDirection.x);
            anim.SetFloat("MoveY", lastDirection.y);
        }
    }

    void Update()
    {
        // BƯỚC 1: Kiểm tra xem có được phép di chuyển không
        if (!canMove)
        {
            movement = Vector2.zero;
            if (anim != null)
            {
                // Giữ hướng nhìn cuối cùng + đông cứng tại frame hiện tại
                anim.SetFloat("MoveX", lastDirection.x);
                anim.SetFloat("MoveY", lastDirection.y);
                anim.speed = 0f;
            }
            return;
        }

        // Đảm bảo Animator chạy bình thường khi được phép di chuyển
        if (anim != null && anim.speed < 1f) anim.speed = 1f;

        // BƯỚC 2: Nếu được đi, thì đọc phím bấm từ người chơi
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // BƯỚC 3: Xử lý Animation dựa trên hướng đi
        if (movement != Vector2.zero)
        {
            lastDirection = movement; // Nhớ hướng cuối cùng
            anim.SetFloat("MoveX", movement.x);
            anim.SetFloat("MoveY", movement.y);
            anim.speed = 1f;
        }
        else
        {
            // Đứng im: giữ hướng nhìn cuối, đông cứng tại frame walk hiện tại
            anim.SetFloat("MoveX", lastDirection.x);
            anim.SetFloat("MoveY", lastDirection.y);
            anim.speed = 0f;
        }
    }

    void FixedUpdate()
    {
        // Di chuyển bằng vật lý (sẽ bị cản bởi Collider)
        // Nếu canMove = false thì movement đã là zero, nên nhân vật sẽ đứng yên
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}