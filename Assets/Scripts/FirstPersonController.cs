using Unity.Netcode;
using UnityEngine;

public class FirstPersonController : NetworkBehaviour
{
    [Header("Hareket")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float jumpForce = 5f;

    [Header("Kamera")]
    public float mouseSensitivity = 2f;
    public Transform cameraRoot;

    [Header("Zýplama Dedektifi")]
    public LayerMask groundMask;
    public float groundCheckDist = 1.2f; // Mesafeyi biraz artýrdým garanti olsun

    private float xRotation = 0f;
    private Rigidbody rb;
    private Camera mainCam;
    private bool isGrounded;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();

        if (IsOwner)
        {
            mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.SetParent(cameraRoot);
                mainCam.transform.localPosition = Vector3.zero;
                mainCam.transform.localRotation = Quaternion.identity;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // --- MOUSE LOOK ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cameraRoot != null) cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // --- ZIPLAMA KONTROLÜ (GÖRSEL DEBUG) ---

        // Karakterin tam ortasýndan aþaðý ýþýn atýyoruz
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDist, groundMask);

        // DEBUG ÇÝZGÝSÝ: Scene ekranýnda karakterin altýna bak
        // YEÞÝL ise yerdesin (Zýplayabilirsin), KIRMIZI ise havadasýn (Zýplayamazsýn)
        Color debugColor = isGrounded ? Color.green : Color.red;
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDist, debugColor);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        // --- HAREKET ---
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 moveDir = transform.right * x + transform.forward * z;
        moveDir.Normalize();

        Vector3 targetVelocity = moveDir * currentSpeed;
        targetVelocity.y = rb.velocity.y;

        rb.velocity = targetVelocity;
    }
}