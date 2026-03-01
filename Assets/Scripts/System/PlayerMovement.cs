using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private InputController input;

    private Rigidbody2D rb;

    private Vector2 move;

    public float speed = 5f;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

            Debug.Log("MOVE INPUT: " + move);
            TryEncounter();
        };


        input.Input.Map.Moves.canceled += ctx =>
        {
            move = Vector2.zero;
        };


        Debug.Log("PLAYER READY");
    }



    void FixedUpdate()
    {
        rb.linearVelocity = move * speed;
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