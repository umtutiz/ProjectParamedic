using Unity.Netcode;
using UnityEngine;

public class PlayerGrab : NetworkBehaviour
{
    public Transform handPosition;
    public float grabRadius = 0.5f;
    public float grabDistance = 3f;
    public float throwForce = 10f; // Fýrlatma gücü
    public LayerMask grabLayer;

    private FixedJoint currentJoint;
    private GrabbableObject currentGrabbedObject;

    void Update()
    {
        if (!IsOwner) return;

        // DEBUG ÇÝZGÝSÝ
        Vector3 rayOrigin = transform.position + Vector3.up * 0.4f;
        Vector3 direction = (transform.forward + Vector3.down * 0.4f).normalized;
        Debug.DrawRay(rayOrigin, direction * grabDistance, Color.red);

        // E TUÞU: Tut / Býrak
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentGrabbedObject == null) TryGrab();
            else Drop();
        }

        // SOL TIK: Fýrlat (Eðer elinde bir þey varsa)
        if (Input.GetMouseButtonDown(0) && currentGrabbedObject != null)
        {
            Throw();
        }
    }

    void TryGrab()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.4f;
        Vector3 direction = (transform.forward + Vector3.down * 0.4f).normalized;

        RaycastHit hit;
        if (Physics.SphereCast(rayOrigin, grabRadius, direction, out hit, grabDistance, grabLayer))
        {
            if (hit.transform.TryGetComponent(out GrabbableObject grabbable))
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
            RequestDropServerRpc(currentGrabbedObject.NetworkObjectId);
            currentGrabbedObject = null;
        }
    }

    void Throw()
    {
        if (currentGrabbedObject != null)
        {
            // Önce tuttuðumuz objenin referansýný ve ID'sini alalým
            ulong objId = currentGrabbedObject.NetworkObjectId;
            Rigidbody rb = currentGrabbedObject.GetComponent<Rigidbody>();

            // Baðlantýyý kopar
            Drop();

            // Fýrlatma gücünü hesapla (Ýleri + biraz yukarý)
            Vector3 force = (transform.forward + Vector3.up * 0.2f).normalized * throwForce;

            // Fýrlatma emrini sunucuya gönder (Fizik sunucuda hesaplansýn ki herkes ayný görsün)
            RequestThrowServerRpc(objId, force);
        }
    }

    [ServerRpc]
    void RequestThrowServerRpc(ulong targetObjectId, Vector3 force)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject networkObject))
        {
            // Sunucuda nesneye güç uygula
            Rigidbody rb = networkObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(force, ForceMode.Impulse);
            }
        }
    }

    // --- GRAB / DROP RPC'leri (Öncekiyle ayný) ---
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

    [ClientRpc]
    void GrabClientRpc(ulong targetObjectId)
    {
        if (!IsOwner) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject networkObject))
        {
            currentGrabbedObject = networkObject.GetComponent<GrabbableObject>();
            currentJoint = gameObject.AddComponent<FixedJoint>();
            currentJoint.connectedBody = networkObject.GetComponent<Rigidbody>();
            currentJoint.breakForce = Mathf.Infinity;
        }
    }
}