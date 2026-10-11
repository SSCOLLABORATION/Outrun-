using UnityEngine;
using UnityEngine.InputSystem;

// This ensures that the script will automatically add a Rigidbody if one isn't present.
[RequireComponent(typeof(Rigidbody))]
public class MushroomController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the mushroom moves in all directions.")]
    public float moveSpeed = 8f;

    [Header("Jump Settings")]
    [Tooltip("The initial upward force applied when jumping.")]
    public float jumpForce = 8f;
    
    [Tooltip("Multiplier applied to gravity when the mushroom is falling. Makes the jump feel 'snappy'.")]
    public float fallMultiplier = 2.5f;
    
    [Tooltip("Multiplier applied to gravity when tapping jump lightly vs holding it (optional, but feels good).")]
    public float lowJumpMultiplier = 2f;

    [Header("Ground Detection")]
    [Tooltip("The layer containing the ground/terrain.")]
    public LayerMask groundLayer;
    
    [Tooltip("How far down to cast the ray to check for ground. Should be slightly larger than half your player's height.")]
    public float groundCheckDistance = 1.1f;

    [Header("Input Setup (New Input System)")]
    [Tooltip("Movement input action. Defaults to WASD/Arrows/Gamepad Left Stick if left empty.")]
    public InputAction moveAction;
    
    [Tooltip("Jump input action. Defaults to Spacebar/Gamepad South Button (A/Cross) if left empty.")]
    public InputAction jumpAction;

    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private bool isGrounded;
    private bool jumpTriggered; // Add this new variable

    void Awake()
    {
        // Setup default bindings so it works out-of-the-box without requiring an Input Action Asset
        if (moveAction.bindings.Count == 0)
        {
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
        }

        if (jumpAction.bindings.Count == 0)
        {
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");
        }
    }

    void OnEnable()
    {
        // The New Input System requires actions to be explicitly enabled
        moveAction.Enable();
        jumpAction.Enable();
    }

    void OnDisable()
    {
        // And disabled when the object is turned off to prevent memory leaks/ghost inputs
        moveAction.Disable();
        jumpAction.Disable();
    }

    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();
        
        // Ensure the Rigidbody doesn't tip over. We only want it moving, not rolling like a ball.
        rb.freezeRotation = true;
    }

    void Update()
    {
        // Read movement input from the New Input System (returns a Vector2)
        Vector2 inputVector = moveAction.ReadValue<Vector2>();
        horizontalInput = inputVector.x;
        verticalInput = inputVector.y;

        // Perform the ground check every frame
        CheckGrounded();

        // Check for jump input using WasPressedThisFrame()
        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            // Cache the input so FixedUpdate can read it safely
            jumpTriggered = true;
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
        
        // Execute the jump in FixedUpdate alongside the other physics
        if (jumpTriggered)
        {
            Jump();
            jumpTriggered = false;
        }

        ApplySnappyGravity();
    }

    private void MovePlayer()
    {
        // Calculate the movement direction based on inputs
        Vector3 moveDirection = (transform.forward * verticalInput) + (transform.right * horizontalInput);
        
        // Normalize the vector so diagonal movement isn't faster than straight movement
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }
        
        // Apply speed to the movement direction
        Vector3 targetVelocity = moveDirection * moveSpeed;
        
        // Keep the current Y velocity so jumping and gravity still work normally
        targetVelocity.y = rb.linearVelocity.y;
        
        // Apply the velocity to the Rigidbody
        rb.linearVelocity = targetVelocity;
    }

    private void Jump()
    {
        // Directly override the Y velocity instead of using AddForce.
        // This stops MovePlayer() and Jump() from fighting over the Rigidbody.
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    private void ApplySnappyGravity()
    {
        // If we are falling (Y velocity is less than 0)
        if (rb.linearVelocity.y < 0)
        {
            // Apply extra downward force
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        // If we are moving up, AND the player lets go of the jump button early (variable jump height)
        else if (rb.linearVelocity.y > 0 && !jumpAction.IsPressed())
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void CheckGrounded()
    {
        // Draw a Raycast straight down from the center of the player
        // We use the groundLayer mask to ensure we only jump off actual terrain/obstacles
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
        
        // --- DEBUG GIZMO ---
        // This helps you see the raycast in the Unity Editor Scene view
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }
}