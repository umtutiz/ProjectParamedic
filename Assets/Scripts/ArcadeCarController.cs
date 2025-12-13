using Unity.Netcode;
using UnityEngine;

public class ArcadeCarController : NetworkBehaviour
{
    [Header("Motor Ayarlarý")]
    public float acceleration = 30f; // Hýzlanma
    public float steering = 50f;     // Dönüþ
    public float gravity = 20f;      // Yere yapýþma gücü

    // Araba þu an kullanýlýyor mu?
    public NetworkVariable<bool> isDriven = new NetworkVariable<bool>(false);

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Aðýrlýk merkezini yere indirdik ki virajda devrilmesin
        rb.centerOfMass = new Vector3(0, -0.9f, 0);
    }

    void Update()
    {
        // Sadece þoför koltuðundaki (Owner) input verebilir
        if (!IsOwner) return;

        moveInput = Input.GetAxis("Vertical");   // W - S
        turnInput = Input.GetAxis("Horizontal"); // A - D
    }

    void FixedUpdate()
    {
        // Sadece birisi sürüyorsa hareket etsin
        if (isDriven.Value)
        {
            MoveCar();
        }
    }

    void MoveCar()
    {
        // 1. Gaz (Arabanýn baktýðý yöne güç uygula)
        Vector3 speedForce = Vector3.forward * moveInput * acceleration * 100f;
        rb.AddRelativeForce(speedForce * Time.fixedDeltaTime);

        // 2. Direksiyon (Sadece hareket halindeyken dön)
        if (rb.velocity.magnitude > 1f)
        {
            // Geri giderken direksiyon ters dönmesin
            float direction = moveInput >= 0 ? 1 : -1;
            Vector3 turnTorque = Vector3.up * turnInput * steering * direction * 100f;
            rb.AddTorque(turnTorque * Time.fixedDeltaTime);
        }

        // 3. Yapay Yerçekimi (Uçmasýn)
        rb.AddForce(Vector3.down * gravity * 100f * Time.fixedDeltaTime);
    }
}