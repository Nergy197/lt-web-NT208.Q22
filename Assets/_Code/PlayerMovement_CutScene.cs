using UnityEngine;

public class PlayerMovement_Cutscene : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public float moveSpeed = 12f;
    public bool canMove = true; // Công tắc khóa chân

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator anim;

    void Start()
    {
        // Lấy các thành phần cần thiết
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // BƯỚC 1: Kiểm tra xem có được phép di chuyển không
        if (!canMove)
        {
            movement = Vector2.zero; // Triệt tiêu lực đi
            if (anim != null) anim.speed = 0f; // Đứng hình tư thế Idle
            return; // Dừng luôn hàm Update tại đây, không đọc phím bấm nữa
        }

        // BƯỚC 2: Nếu được đi, thì đọc phím bấm từ người chơi
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // BƯỚC 3: Xử lý Animation dựa trên hướng đi
        if (movement != Vector2.zero)
        {
            anim.SetFloat("MoveX", movement.x);
            anim.SetFloat("MoveY", movement.y);
            anim.speed = 1f; // Chạy hoạt ảnh khi đang di chuyển
        }
        else
        {
            anim.speed = 0f; // Dừng hoạt ảnh khi đứng im
        }
    }

    void FixedUpdate()
    {
        // Di chuyển bằng vật lý (sẽ bị cản bởi Collider)
        // Nếu canMove = false thì movement đã là zero, nên nhân vật sẽ đứng yên
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}