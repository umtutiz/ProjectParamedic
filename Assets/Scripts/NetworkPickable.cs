using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPickable : NetworkBehaviour, IInteractable
{
    private Rigidbody rb;
    private Collider col; // Tek collider referansý
    private Collider[] allColliders; // Eðer birden fazla varsa (Ragdoll vb.)
    private NetworkTransform netTransform;

    private Transform targetHandPoint;

    [SerializeField] private Vector3 inHandScale = Vector3.one;
    private Vector3 originalScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        allColliders = GetComponentsInChildren<Collider>(); // Tüm colliderlarý bul
        netTransform = GetComponent<NetworkTransform>();
        originalScale = transform.localScale;
    }

    public string GetInteractText() => "Taþý";

    public void Interact(ulong playerID) => PickUpServerRpc(playerID);

    // DEÐÝÞÝKLÝK BURADA: LateUpdate kullanýyoruz!
    // Karakter hareketini tamamladýktan SONRA eþyayý ýþýnlarýz ki geride kalmasýn.
    private void LateUpdate()
    {
        if (targetHandPoint != null)
        {
            transform.position = targetHandPoint.position;
            transform.rotation = targetHandPoint.rotation;
            transform.localScale = inHandScale;
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

                // TÜM COLLIDERLARI KAPAT (Çok önemli, yoksa seni iter!)
                if (col) col.enabled = false;
                foreach (var c in allColliders) c.enabled = false;

                if (netTransform) netTransform.enabled = false;

                // Fake Parent mantýðý
                NetworkObject.TrySetParent(client.PlayerObject.transform, false);
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

        // TÜM COLLIDERLARI AÇ
        if (col) col.enabled = true;
        foreach (var c in allColliders) c.enabled = true;

        if (netTransform) netTransform.enabled = true;

        // Fýrlatýrken boyutu düzelt
        transform.localScale = originalScale;

        // SÜRTÜNMEYÝ AZALT (Hýzlý düþsün)
        rb.drag = 0.05f;

        // FIRLAT
        Vector3 throwForce = transform.forward * 12f + Vector3.up * 2f;
        rb.AddForce(throwForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
    }

    [ClientRpc]
    private void SetTargetClientRpc(ulong playerID)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerID, out NetworkClient client))
        {
            targetHandPoint = FindDeepChild(client.PlayerObject.transform, "HandHoldPoint");
            if (rb) rb.isKinematic = true;

            // Client tarafýnda da colliderlarý kapat
            if (col) col.enabled = false;
            foreach (var c in allColliders) c.enabled = false;

            if (netTransform) netTransform.enabled = false;
        }
    }

    [ClientRpc]
    private void ClearTargetClientRpc()
    {
        targetHandPoint = null;
        if (rb) rb.isKinematic = false;

        // Client tarafýnda colliderlarý aç
        if (col) col.enabled = true;
        foreach (var c in allColliders) c.enabled = true;

        transform.localScale = originalScale;
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