using Unity.Netcode;
using UnityEngine;

public class PlayerGrab : NetworkBehaviour
{
    [Header("Ayarlar")]
    public Transform holdPoint;
    public Camera playerCamera;

    public float grabRadius = 0.8f;
    public float grabDistance = 5f;
    public float throwForce = 15f;
    public LayerMask interactableLayer;

    [Header("Fizik")]
    public float holdSpring = 500f;
    public float holdDamper = 50f;
    public float heldObjectDrag = 10f;
    public float heldObjectAngularDrag = 10f;

    private SpringJoint currentJoint;
    public GrabbableObject currentGrabbedObject;
    private int originalLayer;
    private Collider myCollider;
    private float initialObjectDrag;
    private float initialObjectAngularDrag;

    public override void OnNetworkSpawn()
    {
        myCollider = GetComponent<Collider>();
        if (IsOwner && playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // E TUÞU: YERDEN AL
        if (Input.GetKeyDown(KeyCode.E) && currentGrabbedObject == null)
        {
            TryGrab();
        }

        // G TUÞU: YERE BIRAK
        if (Input.GetKeyDown(KeyCode.G) && currentGrabbedObject != null)
        {
            Drop();
        }

        // SOL TIK: FIRLAT
        if (Input.GetMouseButtonDown(0) && currentGrabbedObject != null)
        {
            Throw();
        }

        // MOUSE TEKERLEÐÝ
        if (currentGrabbedObject != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                Vector3 localPos = holdPoint.localPosition;
                localPos.z = Mathf.Clamp(localPos.z + scroll * 2f, 1.5f, 4f);
                holdPoint.localPosition = localPos;
            }
        }

        // SAÐ TIK: EÞYAYI KULLAN
        if (Input.GetMouseButtonDown(1) && currentGrabbedObject != null)
        {
            // Elimizdeki þey bir "Tüketilebilir Eþya" mý?
            ConsumableItem item = currentGrabbedObject.GetComponent<ConsumableItem>();
            if (item != null)
            {
                // Kullan!
                item.UseItem(gameObject);
            }
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner || currentJoint == null || holdPoint == null) return;
        currentJoint.anchor = transform.InverseTransformPoint(holdPoint.position);
    }

    void TryGrab()
    {
        if (playerCamera == null) return;
        Transform camTransform = playerCamera.transform;
        RaycastHit hit;

        if (Physics.SphereCast(camTransform.position, grabRadius, camTransform.forward, out hit, grabDistance, interactableLayer))
        {
            GrabbableObject grabbable = hit.transform.GetComponentInParent<GrabbableObject>();
            if (grabbable == null) grabbable = hit.transform.GetComponentInChildren<GrabbableObject>();

            if (grabbable != null)
            {
                // BURADAKÝ STRETCHER KONTROLÜNÜ KALDIRDIK
                // ÇÜNKÜ ARTIK SEDYE KENDÝ ÝÞÝNÝ KENDÝ YAPIYOR

                RequestGrabServerRpc(grabbable.NetworkObjectId);
            }
        }
    }

    // BU FONKSÝYON ÞART (Stretcher bunu çaðýracak)
    public void ForceDrop()
    {
        if (currentGrabbedObject != null)
        {
            Drop();
        }
    }

    void Drop()
    {
        if (currentJoint != null) Destroy(currentJoint);

        if (currentGrabbedObject != null)
        {
            Rigidbody rb = currentGrabbedObject.GetComponent<Rigidbody>();
            if (rb == null) rb = currentGrabbedObject.GetComponentInChildren<Rigidbody>();

            if (rb != null)
            {
                rb.drag = initialObjectDrag;
                rb.angularDrag = initialObjectAngularDrag;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.velocity = Vector3.ClampMagnitude(rb.velocity, 20f);
            }

            SetLayerRecursively(currentGrabbedObject.gameObject, originalLayer);
            ToggleCollision(currentGrabbedObject.gameObject, true);
            RequestDropServerRpc(currentGrabbedObject.NetworkObjectId);
            currentGrabbedObject = null;
        }
    }

    void Throw()
    {
        if (currentGrabbedObject != null)
        {
            GrabbableObject objToThrow = currentGrabbedObject;
            Drop();

            if (playerCamera != null)
            {
                Vector3 throwDir = playerCamera.transform.forward;
                RequestThrowServerRpc(objToThrow.NetworkObjectId, throwDir * throwForce);
            }
        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }

    void ToggleCollision(GameObject targetObj, bool enableCollision)
    {
        if (myCollider == null) return;
        Collider[] targetColliders = targetObj.GetComponentsInChildren<Collider>();
        foreach (Collider col in targetColliders)
        {
            if (col == myCollider) continue;
            Physics.IgnoreCollision(myCollider, col, !enableCollision);
        }
    }

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
            foreach (Rigidbody rb in rbs) rb.AddForce(force, ForceMode.Impulse);
        }
    }

    [ClientRpc]
    void GrabClientRpc(ulong targetObjectId)
    {
        if (!IsOwner) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject networkObject))
        {
            currentGrabbedObject = networkObject.GetComponent<GrabbableObject>();
            if (currentGrabbedObject == null) currentGrabbedObject = networkObject.GetComponentInChildren<GrabbableObject>();
            if (currentGrabbedObject == null) return;

            Rigidbody targetRb = networkObject.GetComponent<Rigidbody>();
            if (targetRb == null) targetRb = networkObject.GetComponentInChildren<Rigidbody>();

            ToggleCollision(networkObject.gameObject, false);

            initialObjectDrag = targetRb.drag;
            initialObjectAngularDrag = targetRb.angularDrag;

            targetRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            targetRb.interpolation = RigidbodyInterpolation.None;

            originalLayer = networkObject.gameObject.layer;
            SetLayerRecursively(networkObject.gameObject, 2);

            targetRb.drag = heldObjectDrag;
            targetRb.angularDrag = heldObjectAngularDrag;

            currentJoint = gameObject.AddComponent<SpringJoint>();
            currentJoint.connectedBody = targetRb;
            currentJoint.autoConfigureConnectedAnchor = false;
            currentJoint.anchor = Vector3.zero;
            currentJoint.connectedAnchor = Vector3.zero;
            currentJoint.spring = holdSpring;
            currentJoint.damper = holdDamper;
            currentJoint.minDistance = 0;
            currentJoint.maxDistance = 0;
            currentJoint.breakForce = Mathf.Infinity;
        }
    }
}