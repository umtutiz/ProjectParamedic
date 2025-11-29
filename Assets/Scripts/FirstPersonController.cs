using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : NetworkBehaviour
{
    [Header("Hareket Ayarlarý")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 2.0f;
    public float gravity = -30f; // Yer çekimi

    [Header("Kamera Ayarlarý")]
    public float mouseSensitivity = 2f;
    public Transform cameraRoot;

    private CharacterController controller;
    private float xRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;

    // --- BURASI DEÐÝÞTÝ ---
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // 1. Lobi Kamerasýný Bul ve Kapat
            GameObject lobbyCam = GameObject.Find("LobbyCamera");
            if (lobbyCam != null)
            {
                lobbyCam.SetActive(false);
            }

            // 2. Karakter Hazýrlýðý
            controller = GetComponent<CharacterController>();

            Transform cameraTransform = Camera.main.transform;
            cameraTransform.parent = cameraRoot;
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleMouseLook();
        HandleMovement();
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}