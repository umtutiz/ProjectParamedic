using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections;

public class AmbulanceStretcherLock : NetworkBehaviour
{
    [Header("HASTA AYARLARI")]
    [SerializeField] private Transform patientPoint;
    public NetworkVariable<bool> isFull = new NetworkVariable<bool>(false);

    [Header("AMBULANS AYARLARI")]
    public float ambulanceCheckRadius = 4.0f; // Yarýçapý biraz arttýrdým, ambulansý görsün diye
    public string ambulancePointName = "AmbulanceLockPoint";
    private bool isLockedToAmbulance = false;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            TryToggleAmbulanceLock();
        }
    }

    void TryToggleAmbulanceLock()
    {
        if (isLockedToAmbulance) return;

        // 1. Etraftaki HERHANGÝ BÝR þeye çarp (Ambulansýn kasasý, tekeri vs.)
        Collider[] hits = Physics.OverlapSphere(transform.position, ambulanceCheckRadius);

        foreach (var hit in hits)
        {
            // 2. Çarptýðýmýz þeyin (Ambulansýn) çocuklarýný tara
            // LockPoint'te Collider olmadýðý için direkt onu bulamayýz, babasýndan bulacaðýz.
            Transform foundPoint = FindChildRecursive(hit.transform.root, ambulancePointName);

            if (foundPoint != null)
            {
                // Bulduk! Üstünde NetworkObject var mý?
                NetworkObject pointNetObj = foundPoint.GetComponent<NetworkObject>();
                if (pointNetObj != null)
                {
                    RequestLockToAmbulanceServerRpc(pointNetObj.NetworkObjectId);
                    return;
                }
            }
        }
    }

    // Ýsimden obje bulan yardýmcý fonksiyon (Derin arama yapar)
    Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    [ServerRpc]
    void RequestLockToAmbulanceServerRpc(ulong pointId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pointId, out NetworkObject pointNetObj))
        {
            NetworkObject.RemoveOwnership();

            // Fiziði öldür
            KillPhysics();

            // Yapýþtýr
            NetworkObject.TrySetParent(pointNetObj.transform);

            // Pozisyonu sýfýrla
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            LockToAmbulanceClientRpc();
        }
    }

    [ClientRpc]
    void LockToAmbulanceClientRpc()
    {
        KillPhysics();
        // Emin olmak için tekrar sýfýrla
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    void KillPhysics()
    {
        isLockedToAmbulance = true;

        // NetworkTransform susmazsa titrer
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null) netTransform.enabled = false;

        // Rigidbody ölmezse düþer/çarpar
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.velocity = Vector3.zero;
        }

        // Sedyenin Colliderlarýný kapatmazsak ambulansýn zeminine çarpýp uçar
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var col in cols) col.enabled = false;
    }

    // --- HASTA KISMI (DOKUNMADIM) ---
    public void PlacePatientReal(NetworkObject patientNetObj)
    {
        if (isFull.Value) return;
        isFull.Value = true;
        patientNetObj.TrySetParent(patientPoint);
        patientNetObj.transform.localPosition = Vector3.zero;
        patientNetObj.transform.localRotation = Quaternion.identity;

        Rigidbody rb = patientNetObj.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }

        SetLayerRecursively(patientNetObj.gameObject, 2);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(500);
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }
}