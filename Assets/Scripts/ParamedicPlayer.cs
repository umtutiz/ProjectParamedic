using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ParamedicPlayer : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Referanslar")]
    [SerializeField] private Transform cameraRoot; // Kameranýn olduðu obje (Kafa)

    private CharacterController _cc;
    private float xRotation = 0f; // Yukarý aþaðý bakma sýnýrý için

    public override void OnNetworkSpawn()
    {
        // Eðer bu karakter bana ait deðilse (baþka oyuncuysa)
        if (!IsOwner)
        {
            enabled = false; // Scripti kapat

            // Diðer oyuncunun kamerasýný kapat ki kendi ekranýmda onun gözünden görmeyeyim
            if (cameraRoot != null)
                cameraRoot.gameObject.SetActive(false);

            return;
        }

        // Kendi karakterimse kontrolleri al
        _cc = GetComponent<CharacterController>();

        // Mouse'u ekrana kilitle ve gizle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Sadece sahibi kontrol edebilir
        if (!IsOwner) return;

        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        // Mouse girdilerini al
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 1. Karakteri saða/sola döndür (Gövde döner)
        transform.Rotate(Vector3.up * mouseX);

        // 2. Kamerayý yukarý/aþaðý döndür (Sadece kafa döner)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 90 derece sýnýr koy (boyun kýrýlmasýn)

        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        // WASD girdilerini al
        float x = Input.GetAxis("Horizontal"); // A-D (Yanlara yürüme)
        float z = Input.GetAxis("Vertical");   // W-S (Ýleri geri)

        // Hareketi karakterin baktýðý yöne göre hesapla
        // transform.right = sað taraf, transform.forward = ön taraf
        Vector3 move = transform.right * x + transform.forward * z;

        // Yerçekimi ekle
        move.y = -9.81f;

        // Karakteri yürüt
        _cc.Move(move * moveSpeed * Time.deltaTime);
    }
}