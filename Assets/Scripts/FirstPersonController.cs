using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class FirstPersonController : NetworkBehaviour
{
    [Header("Hareket")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float jumpForce = 5f;

    [Header("Stamina (Yorulma)")]
    public float maxStamina = 100f;
    public float drainRate = 15f;
    public float regenRate = 10f;
    private float currentStamina;

    [Header("Stamina UI Ayarlarý")]
    public float fadeSpeed = 5f;

    // Deðiþkenleri private yaptýk çünkü Manager'dan çekeceðiz
    private CanvasGroup uiCanvasGroup;
    private Image staminaBarFill;

    [Header("Kamera")]
    public float mouseSensitivity = 2f;
    public Transform cameraRoot;

    [Header("Zýplama Dedektifi")]
    public LayerMask groundMask;
    public float groundCheckDist = 1.2f;

    private float xRotation = 0f;
    private Rigidbody rb;
    private Camera mainCam;
    private bool isGrounded;
    private bool isSprinting;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        currentStamina = maxStamina;

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

            // --- OTOMATÝK BAÐLAMA KISMI ---
            // UI elementlerini GameUIManager'dan alýyoruz
            if (GameUIManager.Instance != null)
            {
                uiCanvasGroup = GameUIManager.Instance.staminaCanvasGroup;
                staminaBarFill = GameUIManager.Instance.staminaBarFill;
            }
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

        // --- ZIPLAMA ---
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDist, groundMask);
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // --- STAMINA ---
        HandleStamina();
    }

    void HandleStamina()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        bool isMoving = (x != 0 || z != 0);

        if (Input.GetKey(KeyCode.LeftShift) && isMoving && currentStamina > 0)
        {
            isSprinting = true;
            currentStamina -= drainRate * Time.deltaTime;
        }
        else
        {
            isSprinting = false;
            if (currentStamina < maxStamina)
                currentStamina += regenRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        // UI GÜNCELLEME (Eðer baðlantý baþarýlýysa)
        if (staminaBarFill != null)
            staminaBarFill.fillAmount = currentStamina / maxStamina;

        if (uiCanvasGroup != null)
        {
            float targetAlpha = 0f;
            if (isSprinting || currentStamina < maxStamina - 1f)
                targetAlpha = 1f;

            uiCanvasGroup.alpha = Mathf.Lerp(uiCanvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        float currentSpeed = isSprinting ? runSpeed : walkSpeed;

        Vector3 moveDir = transform.right * x + transform.forward * z;
        moveDir.Normalize();

        Vector3 targetVelocity = moveDir * currentSpeed;
        targetVelocity.y = rb.velocity.y;

        rb.velocity = targetVelocity;
    }
}