using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MushroomController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f; // How fast the mushroom turns
    public float maxTiltAngle = 15f; // Mushroom kitna aage jhukega (tilt hoga)


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
        Vector3 moveDirection = new Vector3(currentMoveInput.x, 0, currentMoveInput.y);
        
        if (moveDirection.magnitude > 0.1f)
        {
            // 1. Jis direction mein ja rahe hain, wahan face karein
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            
            // 2. Aage ki taraf tilt add karein (jitna tez input, utna zyada tilt)
            float currentTilt = moveDirection.magnitude * maxTiltAngle;
            Quaternion tiltRotation = Quaternion.Euler(currentTilt, 0, 0); 
            
            // 3. Dono rotations ko combine karein
            Quaternion finalRotation = lookRotation * tiltRotation;
            
            // 4. Smoothly rotate karein
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // Jab player ruk jaye, toh wapas seedha khada ho jaye (0 tilt)
            Quaternion standStraight = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, standStraight, rotationSpeed * Time.fixedDeltaTime);
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