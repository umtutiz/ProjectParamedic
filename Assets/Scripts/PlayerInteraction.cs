using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Ayarlar")]
    public float grabDistance = 3f;
    public Transform handPoint;
    public LayerMask grabLayer;

    private NetworkObject currentHeldObject;

    void Update()
    {
        if (currentHeldObject != null)
        {
            // Elimizdeyken pozisyonu zorla (Update içinde)
            // Bu, objenin titremesini azaltır
            // Fiziği GrabClientRpc'de hallediyoruz ama bu görsel destek atar
        }

        if (!IsOwner) return;

        // E TUŞU: AL
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentHeldObject == null) TryGrab();
        }

        // G TUŞU: FIRLAT
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (currentHeldObject != null) DropObject();
        }
    }

    void TryGrab()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabDistance, grabLayer))
        {
            // Sadece Grabbable etiketine bakıyoruz
            if (hit.transform.CompareTag("Grabbable"))
            {
                NetworkObject targetNetObj = hit.transform.GetComponentInParent<NetworkObject>();
                if (targetNetObj != null) RequestGrabServerRpc(targetNetObj.NetworkObjectId);
            }
        }
    }

    void DropObject()
    {
        if (currentHeldObject != null) RequestDropServerRpc();
    }

    [ServerRpc]
    void RequestGrabServerRpc(ulong objectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject networkObject))
        {
            networkObject.ChangeOwnership(OwnerClientId);
            GrabClientRpc(objectId);
        }
    }

    [ServerRpc]
    void RequestDropServerRpc()
    {
        if (currentHeldObject != null)
        {
            currentHeldObject.RemoveOwnership();
            DropClientRpc();
        }
    }

    [ClientRpc]
    void GrabClientRpc(ulong objectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject networkObject))
        {
            currentHeldObject = networkObject;

            Rigidbody targetRb = currentHeldObject.GetComponentInChildren<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.transform.position = handPoint.position;
                targetRb.isKinematic = false;

                var cols = currentHeldObject.GetComponentsInChildren<Collider>();
                foreach (var col in cols) col.enabled = true; // Yere çarpsın

                FixedJoint joint = targetRb.gameObject.AddComponent<FixedJoint>();
                joint.breakForce = 20000;
                joint.connectedBody = handPoint.GetComponent<Rigidbody>();
            }
        }
    }

    [ClientRpc]
    void DropClientRpc()
    {
        if (currentHeldObject != null)
        {
            Rigidbody targetRb = currentHeldObject.GetComponentInChildren<Rigidbody>();
            if (targetRb != null)
            {
                FixedJoint joint = targetRb.GetComponent<FixedJoint>();
                if (joint != null) Destroy(joint);

                targetRb.velocity = Vector3.zero;
                targetRb.AddForce(Camera.main.transform.forward * 5f, ForceMode.Impulse);
            }
            currentHeldObject = null;
        }
    }
}