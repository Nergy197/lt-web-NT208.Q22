using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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
        if (InputController.Instance == null)
            return;

        if (InputController.Instance.Mode != InputMode.Map)
        {
            move = Vector2.zero;
            return;
        }

        move = InputController.Instance.Input.Map.Moves.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = move * speed;
    }
}