using Unity.Netcode;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Ayarlar")]
    public float grabDistance = 3f;
    public Transform handPoint;
    public LayerMask grabLayer;

    private NetworkObject currentHeldObject;

    void Update()
    {
        // 1. HER KAREDE POZÝSYON GÜNCELLEME (KÝLÝT NOKTA BURASI)
        // Eðer elimde bir þey varsa, onu zorla el noktasýna ýþýnla
        if (currentHeldObject != null)
        {
            currentHeldObject.transform.position = handPoint.position;
            currentHeldObject.transform.rotation = handPoint.rotation;
        }

        if (!IsOwner) return;

        // E tuþuna basma kontrolü
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentHeldObject == null) TryGrab();
            else DropObject();
        }
    }

    void TryGrab()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabDistance, grabLayer))
        {
            if (hit.transform.CompareTag("Grabbable"))
            {
                NetworkObject targetNetObj = hit.transform.GetComponent<NetworkObject>();
                if (targetNetObj != null)
                {
                    RequestGrabServerRpc(targetNetObj.NetworkObjectId);
                }
            }
        }
    }

    void DropObject()
    {
        if (currentHeldObject != null)
        {
            RequestDropServerRpc();
        }
    }

    [ServerRpc]
    void RequestGrabServerRpc(ulong objectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject networkObject))
        {
            // DÜZELTME: Sunucu sadece "Player"a baðlasýn (Çünkü HandPoint'i tanýmaz)
            networkObject.TrySetParent(transform);
            GrabClientRpc(objectId);
        }
    }

    [ServerRpc]
    void RequestDropServerRpc()
    {
        if (currentHeldObject != null)
        {
            currentHeldObject.TryRemoveParent();
            DropClientRpc();
        }
    }

    [ClientRpc]
    void GrabClientRpc(ulong objectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject networkObject))
        {
            currentHeldObject = networkObject;

            // FÝZÝKLERÝ KAPAT
            var rb = currentHeldObject.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            var col = currentHeldObject.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    [ClientRpc]
    void DropClientRpc()
    {
        if (currentHeldObject != null)
        {
            // FÝZÝKLERÝ AÇ
            var rb = currentHeldObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(Camera.main.transform.forward * 3f, ForceMode.Impulse); // Baktýðýn yere fýrlat
            }

            var col = currentHeldObject.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            currentHeldObject = null;
        }
    }
}