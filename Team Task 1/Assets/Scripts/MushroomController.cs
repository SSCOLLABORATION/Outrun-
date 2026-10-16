using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class MushroomController : MonoBehaviour
{
    [Header("Pit Death Settings")]
    public float deathHeight = -10f; // Agar player ki height isse neeche gayi toh respawn hoga
    [Header("UI Settings")]
    public GameObject winPanel;
    public GameObject startPanel;
    public GameObject pausePanel; // Naya Pause UI
    
    private bool isPaused = false;
    private InputAction pauseAction; // Pause input ke liye
[Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip jumpSound;
    public AudioClip winSound;
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


    public void StartGame()
    {
        if (startPanel != null)
        {
            startPanel.SetActive(false); 
        }
        Time.timeScale = 1f; 

        // Mouse ko hide aur lock karein
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

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

        pauseAction = new InputAction("Pause", binding: "<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
    }
   void Start()
    {
        // Game start hote hi Start Menu dikhna chahiye
        if (startPanel != null)
        {
            startPanel.SetActive(true); 
        }
        
        Time.timeScale = 0f; // Game ko pause rakhein

        // Mouse ko dikhayein taaki player click kar sake
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None; 
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        pauseAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        pauseAction.Disable();
    }

    void Update()
    {
        currentMoveInput = moveAction.ReadValue<Vector2>();

        if (jumpAction.WasPressedThisFrame() && IsGrounded())
        {
            jumpRequested = true;
        }
        // Pit (Khai) mein girne par death logic
        if (transform.position.y < deathHeight)
        {
            Debug.Log("Fell in the pit! Respawning...");
            transform.position = startPosition;
            rb.linearVelocity = Vector3.zero; // Girne ki speed zero kar do
        }
        if (pauseAction.WasPressedThisFrame())
        {
            TogglePause();
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
        // Sound play karne ka code
        if (jumpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
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
    // Yeh function triggers (jo physical deewar nahi hote) ke liye use hota hai
   

    private void OnTriggerEnter(Collider other)
 {
     if (other.CompareTag("Goal"))
     {
         Debug.Log("YOU WIN! Level Completed!");
         moveSpeed = 0;
         jumpForce = 0;
         rb.linearVelocity = Vector3.zero; 

         // UI Panel ko on karna
         if (winPanel != null)
         {
             winPanel.SetActive(true);
             Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
         }
         // Jeetne par sound play karna
            if (winSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(winSound);
            }
     }
 }

 // Yeh function Restart button click hone par chalega
 public void RestartGame()
 {
     SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
 }
 // Yeh function Start button click hone par chalega
   

    // Yeh function Quit button click hone par chalega
    public void QuitGame()
    {
        Debug.Log("Game Band Ho Gaya!");
        Application.Quit();
    }
    public void TogglePause()
    {
        // Agar win ya start screen open hai, toh pause kaam na kare
        if ((winPanel != null && winPanel.activeSelf) || (startPanel != null && startPanel.activeSelf))
            return;

        isPaused = !isPaused; // Pause ka status ulta kar do (true ko false, false ko true)

        if (isPaused)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f; // Game freeze karein

            // Pause menu aane par mouse wapas dikhayein
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f; // Game wapas chalu karein

            // Resume hone par mouse wapas hide kar dein
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // Yeh function Resume button ke liye hai
    public void ResumeGame()
    {
        if (isPaused)
        {
            TogglePause();
        }
    }
}