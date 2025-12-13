using Unity.Netcode;
using UnityEngine;

public class PlayerGrab : NetworkBehaviour
{
    [Header("Ayarlar")]
    public Transform holdPoint; // KAMERANIN ALTINDAKÝ NOKTA (Bunu atamayý unutma!)
    public float grabRadius = 0.8f;
    public float grabDistance = 5f;
    public float throwForce = 20f;
    public LayerMask interactableLayer;

    [Header("Fizik Ayarlarý")]
    public float holdSpring = 200f;  // Yayýn gücü (Daha sýký tutuþ)
    public float holdDamper = 10f;   // Titremeyi önleme
    public float heldObjectDrag = 10f; // Tutarken eþya aðýrlaþsýn (Sallanmasýn)
    public float heldObjectAngularDrag = 10f; // Dönmesi yavaþlasýn

    private SpringJoint currentJoint;
    private GrabbableObject currentGrabbedObject;
    private Collider myCollider;
    private float initialObjectDrag;
    private float initialObjectAngularDrag;

    public override void OnNetworkSpawn()
    {
        myCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (!IsOwner) return;

        // DEBUG: HoldPoint'i gör
       // if (holdPoint != null)
            Debug.DrawLine(Camera.main.transform.position, holdPoint.position, Color.cyan);

        // E TUÞU: Tut
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentGrabbedObject == null) TryGrab();
        }

        // G TUÞU: Býrak
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (currentGrabbedObject != null) Drop();
        }

        // SOL TIK: Fýrlat
        if (Input.GetMouseButtonDown(0) && currentGrabbedObject != null)
        {
            Throw();
        }

        // MOUSE TEKERLEÐÝ: Eþyayý Ýleri/Geri Al
        if (currentGrabbedObject != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                // HoldPoint'in Z pozisyonunu deðiþtir (Min 1.5m, Max 4m)
                Vector3 localPos = holdPoint.localPosition;
                localPos.z += scroll * 2f;
                localPos.z = Mathf.Clamp(localPos.z, 1.5f, 4f);
                holdPoint.localPosition = localPos;
            }
        }
    }

    // FÝZÝK GÜNCELLEMESÝ (HAVAYA KALDIRMA BURADA OLUYOR)
    void FixedUpdate()
    {
        if (!IsOwner || currentJoint == null || holdPoint == null) return;

        // Yayýn oyuncudaki ucunu (Anchor) HoldPoint'in olduðu yere taþý
        // InverseTransformPoint: Dünya pozisyonunu, oyuncunun local pozisyonuna çevirir
        currentJoint.anchor = transform.InverseTransformPoint(holdPoint.position);
    }

    void TryGrab()
    {
        if (Camera.main == null) return;

        Transform camTransform = Camera.main.transform;
        RaycastHit hit;

        // SphereCast ile ara
        if (Physics.SphereCast(camTransform.position, grabRadius, camTransform.forward, out hit, grabDistance, interactableLayer))
        {
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
            // Eski fizik deðerlerini geri yükle (Kayganlýðý geri gelsin)
            Rigidbody rb = currentGrabbedObject.GetComponent<Rigidbody>();
            if (rb == null) rb = currentGrabbedObject.GetComponentInChildren<Rigidbody>();

            if (rb != null)
            {
                rb.drag = initialObjectDrag;
                rb.angularDrag = initialObjectAngularDrag;
            }

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

            // Kameranýn baktýðý yöne fýrlat
            Vector3 throwDir = Camera.main.transform.forward;
            RequestThrowServerRpc(objToThrow.NetworkObjectId, throwDir * throwForce);
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
            currentGrabbedObject = networkObject.GetComponent<GrabbableObject>();
            if (currentGrabbedObject == null) currentGrabbedObject = networkObject.GetComponentInChildren<GrabbableObject>();
            if (currentGrabbedObject == null) return;

            Rigidbody targetRb = networkObject.GetComponent<Rigidbody>();
            if (targetRb == null) targetRb = networkObject.GetComponentInChildren<Rigidbody>();

            // Çarpýþmayý kapat
            ToggleCollision(networkObject.gameObject, false);

            // Eski fizik deðerlerini kaydet
            initialObjectDrag = targetRb.drag;
            initialObjectAngularDrag = targetRb.angularDrag;

            // Tutarken objeyi aðýrlaþtýr (Tok dursun, sallanmasýn)
            targetRb.drag = heldObjectDrag;
            targetRb.angularDrag = heldObjectAngularDrag;

            // --- GELÝÞMÝÞ SPRING JOINT ---
            currentJoint = gameObject.AddComponent<SpringJoint>();
            currentJoint.connectedBody = targetRb;

            currentJoint.autoConfigureConnectedAnchor = false;
            // Anchor'ý scriptte sürekli güncelleyeceðiz, baþlangýçta sýfýr olsun
            currentJoint.anchor = Vector3.zero;
            currentJoint.connectedAnchor = Vector3.zero;

            currentJoint.spring = holdSpring;
            currentJoint.damper = holdDamper;
            currentJoint.minDistance = 0;
            currentJoint.maxDistance = 0; // Sýfýr olsun ki HoldPoint'e tam yapýþmaya çalýþsýn

            currentJoint.breakForce = Mathf.Infinity;
        }
    }
}