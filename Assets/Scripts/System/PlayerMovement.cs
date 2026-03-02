using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    private InputController input;
    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 move;

    public float speed = 5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // THÊM
    }

    void Start()
    {
        input = InputController.Instance;

        if (input == null)
        {
            Debug.LogError("InputController NULL");
            return;
        }

        input.Input.Map.Moves.performed += ctx =>
        {
            move = ctx.ReadValue<Vector2>();

            // UPDATE ANIMATION
            UpdateAnimation();

            Debug.Log("MOVE INPUT: " + move);
            TryEncounter();
        };

        input.Input.Map.Moves.canceled += ctx =>
        {
            move = Vector2.zero;

            // UPDATE ANIMATION
            UpdateAnimation();
        };

        Debug.Log("PLAYER READY");
    }

    void FixedUpdate()
    {
        rb.linearVelocity = move * speed;

        // đảm bảo update liên tục
        UpdateAnimation();
    }

    // ================= ANIMATION =================

    void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetFloat("MoveX", move.x);
        animator.SetFloat("MoveY", move.y);
        animator.SetFloat("Speed", move.sqrMagnitude);
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
}