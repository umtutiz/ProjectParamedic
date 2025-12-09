using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPickable : NetworkBehaviour, IInteractable
{
    private Rigidbody rb;
    private Collider col;
    private NetworkTransform netTransform;

    private Transform targetHandPoint; // Hedef nokta

    // YENÝ AYAR: Eline alýnca boyutu ne olsun? (0.3 idealdýr)
    [SerializeField] private Vector3 inHandScale = new Vector3(0.3f, 0.3f, 0.3f);
    private Vector3 originalScale; // Yere atýnca eski boyutuna dönsün

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        netTransform = GetComponent<NetworkTransform>();
        originalScale = transform.localScale; // Baþlangýç boyutunu hafýzaya al
    }

    public string GetInteractText()
    {
        return "Al";
    }

    public void Interact(ulong playerID)
    {
        PickUpServerRpc(playerID);
    }

    private void Update()
    {
        // Eldeyse sürekli pozisyonu, açýyý VE BOYUTU zorla ayarla
        if (targetHandPoint != null)
        {
            transform.position = targetHandPoint.position;
            transform.rotation = targetHandPoint.rotation;
            transform.localScale = inHandScale; // BÜYÜMEYÝ ENGELLEYEN SATIR
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PickUpServerRpc(ulong playerID)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerID, out NetworkClient client))
        {
            Transform foundHand = FindDeepChild(client.PlayerObject.transform, "HandHoldPoint");

            if (foundHand != null)
            {
                rb.isKinematic = true;
                col.enabled = false;
                if (netTransform) netTransform.enabled = false;

                // Player'a baðla
                NetworkObject.TrySetParent(client.PlayerObject.transform, false);

                // Clientlara hedefi göster
                SetTargetClientRpc(playerID);
            }
        }
    }

    public void DropItem()
    {
        targetHandPoint = null;
        ClearTargetClientRpc();

        NetworkObject.TrySetParent((GameObject)null);

        rb.isKinematic = false;
        col.enabled = true;
        if (netTransform) netTransform.enabled = true;

        // --- DEÐÝÞEN KISIM: KOMÝK FÝZÝK ---

        // 1. ESKÝ BOYUTUNA DÖN
        transform.localScale = originalScale;

        // 2. DELÝ GÝBÝ DÖNDÜR (Random Tork)
        // Her seferinde rastgele bir yöne fýrýldak gibi döner
        Vector3 randomSpin = new Vector3(
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f)
        );
        rb.AddTorque(randomSpin, ForceMode.Impulse);

        // 3. YERE ÇAK (Smaç Bas)
        // transform.forward * 2f -> Hafif ileri gitsin (ayaðýna düþmesin)
        // Vector3.down * 8f     -> Yere füze gibi insin (HIZ BURADA)
        rb.AddForce(transform.forward * 2f + Vector3.down * 8f, ForceMode.Impulse);
    }

    [ClientRpc]
    private void SetTargetClientRpc(ulong playerID)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerID, out NetworkClient client))
        {
            targetHandPoint = FindDeepChild(client.PlayerObject.transform, "HandHoldPoint");
            if (rb) rb.isKinematic = true;
            if (col) col.enabled = false;
            if (netTransform) netTransform.enabled = false;
        }
    }

    [ClientRpc]
    private void ClearTargetClientRpc()
    {
        targetHandPoint = null;
        if (rb) rb.isKinematic = false;
        if (col) col.enabled = true;
        transform.localScale = originalScale; // Client tarafýnda da boyutu düzelt
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}