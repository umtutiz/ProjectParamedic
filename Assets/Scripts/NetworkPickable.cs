using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPickable : NetworkBehaviour, IInteractable
{
    private Rigidbody rb;
    private Collider col; // Collider'ý açýp kapatmak için referans

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public string GetInteractText()
    {
        return "Al";
    }

    // Sadece YERDEYKEN çalýþýr (E Tuþu)
    public void Interact(ulong playerID)
    {
        PickUpServerRpc(playerID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PickUpServerRpc(ulong playerID)
    {
        // Zaten birinin elindeyse alma
        if (transform.parent != null) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerID, out NetworkClient client))
        {
            Transform handPoint = FindHandPointRecursive(client.PlayerObject.transform, "HandHoldPoint");

            if (handPoint != null)
            {
                // ELDEKÝ AYARLAR:
                rb.isKinematic = true; // Fiziði kapat
                col.enabled = false;   // Çarpýþmayý kapat (Raycast artýk bunu göremez, ama G tuþu görecek)

                NetworkObject.TrySetParent(handPoint);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;

                // Client tarafýnda da fiziði kapatmak için
                SetPhysicsClientRpc(false);
            }
        }
    }

    // Bu fonksiyonu PlayerInteractor (G Tuþu) çaðýracak
    public void DropItem()
    {
        // YERE DÜÞME AYARLARI:
        NetworkObject.TrySetParent((GameObject)null); // Ebeveynliði sil

        rb.isKinematic = false; // Fiziði aç
        rb.useGravity = true;   // Yerçekimi aç
        col.enabled = true;     // Çarpýþmayý aç (Tekrar alýnabilsin)

        // Hafif ileri fýrlat (ayaðýmýza düþmesin)
        rb.AddForce(transform.forward * 2f + Vector3.up * 1f, ForceMode.Impulse);

        SetPhysicsClientRpc(true);
    }

    [ClientRpc]
    private void SetPhysicsClientRpc(bool enabled)
    {
        if (rb)
        {
            rb.isKinematic = !enabled;
            rb.useGravity = enabled;
        }
        if (col) col.enabled = enabled;
    }

    private Transform FindHandPointRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindHandPointRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}