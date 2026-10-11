using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MushroomController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f; // How fast the mushroom turns

    [Header("Jump Settings")]
    public float jumpForce = 5f;
    public float fallMultiplier = 2.5f;
    
    [Header("Ground Detection")]
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    
    private Vector2 currentMoveInput;
    private bool jumpRequested = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Setup New Input System Actions via code
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");

        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth"); // A button on Xbox, Cross on PS
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    void Update()
    {
        currentMoveInput = moveAction.ReadValue<Vector2>();

        if (jumpAction.WasPressedThisFrame() && IsGrounded())
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
        HandleRotation();
        ApplySnappyGravity();

        if (jumpRequested)
        {
            ExecuteJump();
        }
    }

    void MovePlayer()
    {
        Vector3 targetVelocity = new Vector3(currentMoveInput.x * moveSpeed, rb.linearVelocity.y, currentMoveInput.y * moveSpeed);
        rb.linearVelocity = targetVelocity;
    }

    void HandleRotation()
    {
        // Only rotate if we have input (prevents snapping back to zero when we stop)
        Vector3 moveDirection = new Vector3(currentMoveInput.x, 0, currentMoveInput.y);
        
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            // Slerp smoothly rotates the character instead of snapping instantly
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void ExecuteJump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        jumpRequested = false;
    }

    void ApplySnappyGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }
}