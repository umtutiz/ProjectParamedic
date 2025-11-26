using Unity.Netcode;
using UnityEngine;

public class FirstPersonController : NetworkBehaviour
{
    [Header("Ayarlar")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    [Header("Referanslar")]
    public Transform cameraRoot; // Kafa objesi buraya gelecek

    private float xRotation = 0f; // Yukarý aþaðý bakma açýsý

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Eðer bu karakter benimse, sahnedeki Ana Kamerayý bul ve Kafamýn içine sok
            Transform cameraTransform = Camera.main.transform;
            cameraTransform.parent = cameraRoot; // Kamerayý kafanýn çocuðu yap
            cameraTransform.localPosition = Vector3.zero; // Tam kafanýn ortasýna oturt
            cameraTransform.localRotation = Quaternion.identity; // Açýsýný sýfýrla

            // Fareyi ekrana kilitle ve gizle (FPS modu)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return; // Baþkasýnýn karakterini kontrol etme

        HandleMovement();
        HandleMouseLook();
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal"); // A-D
        float z = Input.GetAxis("Vertical");   // W-S

        // Baktýðým yöne doðru hareket et
        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * moveSpeed * Time.deltaTime;
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Yukarý-Aþaðý bakma (Kafayý döndürür)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Boynu kýrmamak için sýnýrla

        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Saða-Sola bakma (Tüm vücudu döndürür)
        transform.Rotate(Vector3.up * mouseX);
    }
}