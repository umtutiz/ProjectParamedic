using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 10f; // Dönüþ hýzý eklendi

    private Rigidbody rb;
    private Vector3 moveInput;

    public override void OnNetworkSpawn()
    {
        // Karakter doðduðunda Rigidbody'yi alalým
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!IsOwner) return;

        // Girdileri her karede al (Input lag olmasýn)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(moveX, 0, moveZ).normalized; // normalized: Çapraz giderken hýzlanmayý engeller
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        // Fiziði burada uyguluyoruz
        if (moveInput.magnitude > 0.1f)
        {
            // 1. Hareket: Pozisyonu fizik motoruyla ittir
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);

            // 2. Dönüþ: Gittiði yöne baksýn
            Quaternion toRotation = Quaternion.LookRotation(moveInput, Vector3.up);
            rb.rotation = Quaternion.Lerp(rb.rotation, toRotation, rotateSpeed * Time.fixedDeltaTime);
        }
    }
}