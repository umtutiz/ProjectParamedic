using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections;

public class AmbulanceStretcherLock : NetworkBehaviour
{
    [Header("GEREKLÝ ATAMALAR")]
    [SerializeField] private Transform patientPoint; // Hastanýn duracaðý nokta
    public NetworkVariable<bool> isFull = new NetworkVariable<bool>(false);

    [Header("AYARLAR")]
    public float searchRadius = 5.0f; // Ambulansý arama mesafesi
    public string anchorName = "AmbulanceAnchor"; // Ambulans içindeki noktanýn adý

    private bool isLocked = false;

    void Update()
    {
        // Sadece sahibi olan oyuncu T'ye basabilir
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.T) && !isLocked)
        {
            FindAndLockToAmbulance();
        }
    }

    void FindAndLockToAmbulance()
    {
        // 1. Etraftaki herhangi bir fiziksel objeyi (Ambulans Kasasý) tara
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius);

        foreach (var hit in hits)
        {
            // 2. Çarptýðýmýz objenin kökünden (Root) baþlayýp, içindeki "AmbulanceAnchor"ý ara
            Transform targetAnchor = FindDeepChild(hit.transform.root, anchorName);

            if (targetAnchor != null)
            {
                // Hedefte NetworkObject var mý kontrol et
                if (targetAnchor.TryGetComponent(out NetworkObject anchorNetObj))
                {
                    // Sunucuya kilitlenme isteði gönder
                    RequestLockServerRpc(anchorNetObj.NetworkObjectId);
                    return; // Ýlk bulduðuna kilitlen ve çýk
                }
            }
        }
    }

    // Objelerin içini tarayan yardýmcý fonksiyon
    Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // --- SUNUCU TARAFI (SERVER) ---
    [ServerRpc]
    void RequestLockServerRpc(ulong anchorId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(anchorId, out NetworkObject anchorNetObj))
        {
            // 1. SAHÝPLÝÐÝ SÝL (Client artýk pozisyonu yönetemez)
            NetworkObject.RemoveOwnership();

            // 2. FÝZÝKLERÝ VE AÐI KAPAT (Server tarafýnda)
            DisablePhysicsAndNetwork();

            // 3. AMBULANSA BAÐLA (Parent yap)
            NetworkObject.TrySetParent(anchorNetObj.transform);

            // 4. ÝÞLEMÝ TAMAMLA
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            // 5. TÜM OYUNCULARA HABER VER
            LockClientRpc();
        }
    }

    // --- OYUNCU TARAFI (CLIENT) ---
    [ClientRpc]
    void LockClientRpc()
    {
        // Clientlarda da fizikleri kapat
        DisablePhysicsAndNetwork();

        // Titremeyi önlemek için pozisyonu sýfýrla
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    // --- EN ÖNEMLÝ KISIM: HER ÞEYÝ KAPATAN FONKSÝYON ---
    void DisablePhysicsAndNetwork()
    {
        isLocked = true;

        // 1. NETWORK TRANSFORM'U SUSTUR
        // Bunu kapatmazsak sedye eski konumuna gitmeye çalýþýr ve ambulansý geri çeker (Iþýnlanma sebebi).
        var netPos = GetComponent<NetworkTransform>();
        if (netPos != null) netPos.enabled = false;

        // 2. RIGIDBODY'YÝ ÖLDÜR
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Fizikten etkilenmesin
            rb.detectCollisions = false; // Çarpýþmalarý yoksay
            rb.velocity = Vector3.zero;
        }

        // 3. BÜTÜN COLLIDER'LARI KAPAT
        // "InChildren" komutu sayesinde sedyenin altýndaki HASTANIN colliderlarýný da kapatýr.
        // Böylece sedye ve hasta, ambulansýn içinden hayalet gibi geçer.
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var col in cols)
        {
            col.enabled = false;
        }
    }

    // --- HASTA ALMA KISMI (Buraya dokunmana gerek yok) ---
    public void PlacePatientReal(NetworkObject patientNetObj)
    {
        if (isFull.Value) return;
        isFull.Value = true;

        patientNetObj.TrySetParent(patientPoint);
        patientNetObj.transform.localPosition = Vector3.zero;
        patientNetObj.transform.localRotation = Quaternion.identity;

        Rigidbody rb = patientNetObj.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }

        SetLayerRecursively(patientNetObj.gameObject, 2); // Ignore Raycast

        if (GameManager.Instance != null) GameManager.Instance.AddScore(500);
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }
}