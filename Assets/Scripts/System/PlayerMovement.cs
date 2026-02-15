using UnityEngine;

public class PlayerMovementTest : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Vector2 move;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        move.x = Input.GetAxisRaw("Horizontal");
        move.y = Input.GetAxisRaw("Vertical");
    }

    void FixedUpdate()
    {
        rb.linearVelocity = move.normalized * speed;
    }
}
