using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;

    void Update()
    {
        // KURAL: Eðer bu karakter benim deðilse (Semih'in karakteriyse) klavyemi dinleme!
        if (!IsOwner) return;

        // Klasik WASD hareketi
        float moveX = Input.GetAxis("Horizontal"); // A-D veya Sol-Sað ok
        float moveZ = Input.GetAxis("Vertical");   // W-S veya Yukarý-Aþaðý ok

        Vector3 moveDir = new Vector3(moveX, 0, moveZ);
        transform.position += moveDir * speed * Time.deltaTime;
    }
}