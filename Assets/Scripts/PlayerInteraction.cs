using Unity.Netcode;
using UnityEngine;
using TMPro; // UI için gerekli

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Fizik Ayarları")]
    public float grabDistance = 3f;
    public Transform handPoint;       // El noktası (Bunun dolu olması şart)
    public LayerMask grabLayer;       // Hangi layer tutulabilir?

    [Header("UI Ayarları")]
    public TextMeshProUGUI interactionText; // "Press E" yazısı
    public GameObject interactionObject;    // Yazı objesi (Aç/Kapat için)

    private NetworkObject currentHeldObject; // Elimde ne var?

    void Update()
    {
        // UI KONTROLÜ (Her karede çalışır)
        CheckInteractionUI();

        if (!IsOwner) return;

        // E TUŞU: AL veya BİN
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

    void CheckInteractionUI()
    {
        if (!IsOwner) return;

        // Kamerayı bulabiliyor muyuz?
        if (Camera.main == null)
        {
            Debug.LogError("KAMERA BULUNAMADI! Tag'i 'MainCamera' olan bir kamera yok!");
            return;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Sahne ekranında kırmızı çizgi çiz (Gözle kontrol için)
        Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red);

        if (Physics.Raycast(ray, out hit, 10f)) // Mesafe 10 metre
        {
            // HER ŞEYİ KONSOLA YAZDIR
            Debug.Log("BAKTIĞIM ŞEY: " + hit.transform.name + " | TAG: " + hit.transform.tag);

            if (hit.transform.CompareTag("Car"))
            {
                // UI Bağlı mı kontrol et
                if (interactionObject != null)
                {
                    interactionObject.SetActive(true);
                    if (interactionText != null) interactionText.text = "Surmek icin [E]";
                }
                else
                {
                    Debug.LogError("HATA: 'interactionObject' (Canvas/Yazı) koda bağlanmamış!");
                }
            }
            else if (hit.transform.CompareTag("Grabbable"))
            {
                if (currentHeldObject == null && interactionObject != null)
                {
                    interactionObject.SetActive(true);
                    if (interactionText != null) interactionText.text = "Tutmak icin [E]";
                }
            }
            else
            {
                if (interactionObject != null) interactionObject.SetActive(false);
            }
        }
        else
        {
            // Havaya bakıyorsan bunu yazar
            // Debug.Log("BOŞLUĞA BAKIYORUM...");
            if (interactionObject != null) interactionObject.SetActive(false);
        }
    }

    void TryGrab()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabDistance, grabLayer))
        {
            Debug.Log("E tuşuna basıldı, vurulan obje: " + hit.transform.name); // KONTROL 1

            // JERRY KONTROLÜ
            if (hit.transform.CompareTag("Grabbable"))
            {
                NetworkObject targetNetObj = hit.transform.GetComponentInParent<NetworkObject>();
                if (targetNetObj != null) RequestGrabServerRpc(targetNetObj.NetworkObjectId);
            }
            // ARABA KONTROLÜ
            else if (hit.transform.CompareTag("Car"))
            {
                Debug.Log("Araba etiketi bulundu!"); // KONTROL 2

                AmbulanceController ambulance = hit.transform.GetComponentInParent<AmbulanceController>();

                if (ambulance != null)
                {
                    Debug.Log("Ambulans scripti bulundu, sunucuya istek atılıyor..."); // KONTROL 3
                    ambulance.RequestEnterCarServerRpc(NetworkManager.Singleton.LocalClientId);
                }
                else
                {
                    Debug.LogError("HATA: Objede 'Car' tagi var ama 'AmbulanceController' scripti yok!"); // HATA BULUCU
                }
            }
        }
    }

    void DropObject()
    {
        if (currentHeldObject != null) RequestDropServerRpc();
    }

    // --- SERVER TARAFI ---

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

    // --- CLIENT TARAFI (FİZİK BURADA) ---

    [ClientRpc]
    void GrabClientRpc(ulong objectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject networkObject))
        {
            currentHeldObject = networkObject;

            // Objeyi bul
            Rigidbody targetRb = currentHeldObject.GetComponentInChildren<Rigidbody>();
            if (targetRb != null)
            {
                // Elime ışınla
                targetRb.transform.position = handPoint.position;

                // Fiziği aç
                targetRb.isKinematic = false;

                // Collider'ları aç (Duvarın içinden geçmesin)
                var cols = currentHeldObject.GetComponentsInChildren<Collider>();
                foreach (var col in cols) col.enabled = true;

                // EKLEM OLUŞTUR (Joint)
                FixedJoint joint = targetRb.gameObject.AddComponent<FixedJoint>();
                joint.breakForce = 20000;
                // El noktasındaki Rigidbody'ye bağla
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
                // Eklemi kopar
                FixedJoint joint = targetRb.GetComponent<FixedJoint>();
                if (joint != null) Destroy(joint);

                // Fırlat
                targetRb.velocity = Vector3.zero;
                targetRb.AddForce(Camera.main.transform.forward * 5f, ForceMode.Impulse);
            }
            currentHeldObject = null;
        }
    }

    // Multiplayer'da başkasının UI'ını görmemek için
    public override void OnNetworkSpawn()
    {
        if (!IsOwner && interactionObject != null)
        {
            // Canvas'ı bulup kapat
            var canvas = interactionObject.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.enabled = false;
        }
    }
}