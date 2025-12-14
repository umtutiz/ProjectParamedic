using Unity.Netcode;
using UnityEngine;

public class AmbulanceStretcherSystem : NetworkBehaviour
{
    [Header("Ayarlar")]
    public Transform stretcherLockPos; // Ambulansýn içindeki "SedyeYeri"
    public float interactionRadius = 3.5f; // Arkadan ne kadar uzaktan alabilsin?

    private Stretcher currentStretcher; // Ýçerdeki sedye

    void Update()
    {
        // T TUÞU: Sedyeyi Ýçeri Al / Dýþarý At
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (currentStretcher != null)
            {
                RequestEjectStretcherServerRpc();
            }
            else
            {
                TryLoadStretcher();
            }
        }

        // HARD LOCK: Sedyeyi ambulansa çivile (Ambulans giderken titremesin)
        if (currentStretcher != null)
        {
            currentStretcher.transform.position = stretcherLockPos.position;
            currentStretcher.transform.rotation = stretcherLockPos.rotation;
        }
    }

    void TryLoadStretcher()
    {
        // Arkadaki alaný tara
        Collider[] hits = Physics.OverlapSphere(stretcherLockPos.position, interactionRadius);
        foreach (var hit in hits)
        {
            // Stretcher scripti olan bir obje var mý?
            Stretcher stretcher = hit.GetComponentInParent<Stretcher>();
            if (stretcher == null) stretcher = hit.GetComponent<Stretcher>();

            if (stretcher != null)
            {
                // 1. Oyuncunun elindeyse býraktýr
                GrabbableObject grabbable = stretcher.GetComponent<GrabbableObject>();
                if (grabbable != null) ForcePlayerToDrop(grabbable);

                // 2. Ýçeri al
                RequestLoadStretcherServerRpc(stretcher.NetworkObjectId);
                return;
            }
        }
    }

    void ForcePlayerToDrop(GrabbableObject targetItem)
    {
        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var playerGrab = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerGrab>();
            if (playerGrab != null) playerGrab.ForceDrop();
        }
    }

    // --- SERVER ---

    [ServerRpc(RequireOwnership = false)]
    void RequestLoadStretcherServerRpc(ulong stretcherId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(stretcherId, out NetworkObject netObj))
        {
            netObj.RemoveOwnership(); // Ambulansýn malý oldu artýk
            netObj.TrySetParent(stretcherLockPos); // Ambulansýn çocuðu yap

            LoadStretcherClientRpc(stretcherId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestEjectStretcherServerRpc()
    {
        if (currentStretcher == null) return;

        NetworkObject netObj = currentStretcher.GetComponent<NetworkObject>();
        netObj.TryRemoveParent(); // Özgür býrak

        EjectStretcherClientRpc(netObj.NetworkObjectId);
    }

    // --- CLIENT ---

    [ClientRpc]
    void LoadStretcherClientRpc(ulong stretcherId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(stretcherId, out NetworkObject netObj))
        {
            currentStretcher = netObj.GetComponent<Stretcher>();

            // Fiziði Kapat (Ambulansýn içinde çarpýþmasýn)
            Rigidbody rb = currentStretcher.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
            }

            // Collider'larý kapatmaya gerek yok, zeminle çarpýþmasý bazen iyidir ama 
            // sorun çýkarýrsa buraya collider kapatma kodu da ekleriz.

            // Konumla
            currentStretcher.transform.position = stretcherLockPos.position;
            currentStretcher.transform.rotation = stretcherLockPos.rotation;
        }
    }

    [ClientRpc]
    void EjectStretcherClientRpc(ulong stretcherId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(stretcherId, out NetworkObject netObj))
        {
            // Fiziði Aç
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = false;

            // Arkaya doðru hafif fýrlat (Dýþarý çýksýn)
            if (rb) rb.AddForce(-transform.forward * 2f, ForceMode.Impulse);

            currentStretcher = null;
        }
    }

    // Editörde alaný görelim
    void OnDrawGizmos()
    {
        if (stretcherLockPos != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(stretcherLockPos.position, interactionRadius);
        }
    }
}