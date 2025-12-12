using Unity.Netcode;
using UnityEngine;

public class PlayerGrab : NetworkBehaviour
{
    public Transform handPosition;
    public float grabRadius = 1.0f;
    public float grabDistance = 4f;
    public float throwForce = 15f;
    public LayerMask interactableLayer;

    // KÝLÝTLENMEYÝ VE ÝÇ ÝÇE GÝRMEYÝ ÖNLEYEN YAY
    private SpringJoint currentJoint;
    private GrabbableObject currentGrabbedObject;
    private Collider myCollider;

    public override void OnNetworkSpawn()
    {
        // Start yerine burayý kullanmak multiplayerda daha güvenlidir
        myCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (!IsOwner) return;

        // E TUÞU: Tut
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentGrabbedObject == null) TryGrab();
        }

        // G TUÞU: Býrak
        if (Input.GetKeyDown(KeyCode.G))
        {
            // Eðer elimizde obje varsa býrak, yoksa hata mesajý verme
            if (currentGrabbedObject != null) Drop();
        }

        // SOL TIK: Fýrlat
        if (Input.GetMouseButtonDown(0) && currentGrabbedObject != null)
        {
            Throw();
        }
    }

    void TryGrab()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.4f;
        Vector3 direction = (transform.forward + Vector3.down * 0.2f).normalized;

        RaycastHit hit;
        if (Physics.SphereCast(rayOrigin, grabRadius, direction, out hit, grabDistance, interactableLayer))
        {
            // Hem objede hem parentýnda ara (Garanti olsun)
            GrabbableObject grabbable = hit.transform.GetComponentInParent<GrabbableObject>();
            if (grabbable == null) grabbable = hit.transform.GetComponentInChildren<GrabbableObject>();

            if (grabbable != null)
            {
                RequestGrabServerRpc(grabbable.NetworkObjectId);
            }
        }
    }

    void Drop()
    {
        if (currentJoint != null) Destroy(currentJoint);

        if (currentGrabbedObject != null)
        {
            ToggleCollision(currentGrabbedObject.gameObject, true);
            RequestDropServerRpc(currentGrabbedObject.NetworkObjectId);
            currentGrabbedObject = null; // Deðiþkeni boþalt
            Debug.Log("Obje Býrakýldý.");
        }
    }

    void Throw()
    {
        if (currentGrabbedObject != null)
        {
            GrabbableObject objToThrow = currentGrabbedObject;
            Drop(); // Önce baðý kopar

            Vector3 force = (transform.forward + Vector3.up * 0.2f).normalized * throwForce;
            RequestThrowServerRpc(objToThrow.NetworkObjectId, force);
        }
    }

    void ToggleCollision(GameObject targetObj, bool enableCollision)
    {
        if (myCollider == null) return;
        Transform rootObj = targetObj.transform.root;
        Collider[] targetColliders = rootObj.GetComponentsInChildren<Collider>();

        foreach (Collider col in targetColliders)
        {
            if (col == myCollider) continue;
            // ignore = !enableCollision (True ise yoksay, False ise çarpýþ)
            Physics.IgnoreCollision(myCollider, col, !enableCollision);
        }
    }

    // --- RPC ---

    [ServerRpc]
    void RequestGrabServerRpc(ulong targetObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject networkObject))
        {
            networkObject.ChangeOwnership(OwnerClientId);
            GrabClientRpc(targetObjectId);
        }
    }

    [ServerRpc]
    void RequestDropServerRpc(ulong targetObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject networkObject))
        {
            networkObject.RemoveOwnership();
        }
    }

    [ServerRpc]
    void RequestThrowServerRpc(ulong targetObjectId, Vector3 force)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject networkObject))
        {
            Rigidbody[] rbs = networkObject.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rbs)
            {
                rb.AddForce(force, ForceMode.Impulse);
            }
        }
    }

    [ClientRpc]
    void GrabClientRpc(ulong targetObjectId)
    {
        if (!IsOwner) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject networkObject))
        {
            // DÜZELTME: Sadece GetComponent deðil, çocuklarý da tara!
            currentGrabbedObject = networkObject.GetComponent<GrabbableObject>();
            if (currentGrabbedObject == null) currentGrabbedObject = networkObject.GetComponentInChildren<GrabbableObject>();

            // Eðer hala bulamadýysa hata ver ve çýk (G tuþu sorununun kökü burasýydý)
            if (currentGrabbedObject == null)
            {
                Debug.LogError("HATA: Grabbable scripti bulunamadý! Prefab yapýsýný kontrol et.");
                return;
            }

            // Rigidbody bul
            Rigidbody targetRb = networkObject.GetComponent<Rigidbody>();
            if (targetRb == null) targetRb = networkObject.GetComponentInChildren<Rigidbody>();

            // Çarpýþmayý kapat (Ýçime girmesin diye önlem 1)
            ToggleCollision(networkObject.gameObject, false);

            // --- YAY AYARLARI (ÝÇÝME GÝRMESÝN DÝYE ÖNLEM 2) ---
            currentJoint = gameObject.AddComponent<SpringJoint>();
            currentJoint.connectedBody = targetRb;

            // Bu ayarlar nesneyi uzakta tutar:
            currentJoint.autoConfigureConnectedAnchor = false;
            currentJoint.anchor = Vector3.up * 0.5f; // Omuz hizasýndan tut
            currentJoint.connectedAnchor = Vector3.zero;

            currentJoint.spring = 100f;   // Çekme gücü
            currentJoint.damper = 10f;    // Titremeyi önleme

            // KRÝTÝK AYAR: Nesne en az 1.5 metre uzakta dursun!
            currentJoint.minDistance = 1.5f;
            currentJoint.maxDistance = 2.0f;

            currentJoint.breakForce = Mathf.Infinity;
            Debug.Log("BAÐLANTI TAMAM: " + currentGrabbedObject.name);
        }
    }
}