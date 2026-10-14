using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MushroomController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f; 
    public float maxTiltAngle = 15f; 

    [Header("Jump Settings")]
    public float jumpForce = 5f;
    public float fallMultiplier = 2.5f;
    
    [Header("Ground Detection")]
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;

    [Header("Game Loop")]
    private Vector3 startPosition;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    
    private Vector2 currentMoveInput;
    private bool jumpRequested = false;

    private Transform mainCam; // Camera ka reference

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCam = Camera.main.transform; // Main camera ko dhundna
        startPosition = transform.position;
        
        // Input Setup
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
        jumpAction.AddBinding("<Gamepad>/buttonSouth"); 
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
        ApplySnappyGravity();

        if (jumpRequested)
        {
            ExecuteJump();
        }
    }
    
    // Yeh function calculate karta hai ki camera kahan dekh raha hai
    private Vector3 GetCameraRelativeMovement()
    {
        Vector3 camForward = mainCam.forward;
        Vector3 camRight = mainCam.right;

        // Y axis ko 0 karte hain taaki player hawa mein ya zameen ke andar na point kare
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        return (camForward * currentMoveInput.y + camRight * currentMoveInput.x).normalized;
    }

    void MovePlayer()
    {
        Vector3 moveDirection = GetCameraRelativeMovement();
        
        // Updated to linearVelocity
        Vector3 targetVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
        rb.linearVelocity = targetVelocity;

        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            float currentTilt = currentMoveInput.magnitude * maxTiltAngle;
            Quaternion tiltRotation = Quaternion.Euler(currentTilt, 0, 0); 
            Quaternion finalRotation = lookRotation * tiltRotation;
            
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            Quaternion standStraight = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, standStraight, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void ExecuteJump()
    {
        // Updated to linearVelocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        jumpRequested = false;
    }
   void ApplySnappyGravity()
    {
        // Updated to linearVelocity
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }
    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            Debug.Log("Mushroom died! Respawning...");
            transform.position = startPosition;
            rb.linearVelocity = Vector3.zero; // Updated to linearVelocity
        }
    }
}