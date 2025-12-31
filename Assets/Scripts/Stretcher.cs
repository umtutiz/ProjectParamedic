using Unity.Netcode;
using UnityEngine;

public class Stretcher : NetworkBehaviour
{
    [Header("AYARLAR")]
    public Transform patientHoldPoint; // Hastanýn yatacaðý nokta
    public float lockRadius = 3.0f;    // Ne kadar yakýndakini kapsýn?

    // Herkesin görebilmesi için NetworkVariable
    public NetworkVariable<bool> isFull = new NetworkVariable<bool>(false);

    private GrabbableObject lockedPatient; // O an kilitli olan hasta

    void LateUpdate()
    {
        // 1. R TUÞU KONTROLÜ (Senin eski kodun gibi buraya koydum)
        // Sadece yakýndaysak çalýþsýn istersen mesafe kontrolü de ekleriz ama þimdilik senin kodun
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (lockedPatient != null)
            {
                // Zaten hasta varsa, R'ye basýnca býrak
                RequestDetachPatientServerRpc();
            }
            else
            {
                // Hasta yoksa, etrafta hasta ara ve kilitle
                TryAttachPatient();
            }
        }

        // 2. HARD LOCK (Japon Yapýþtýrýcýsý)
        // Hasta varsa, her karede ZORLA pozisyonu eþitle. Asla kayamaz.
        if (lockedPatient != null)
        {
            lockedPatient.transform.position = patientHoldPoint.position;
            lockedPatient.transform.rotation = patientHoldPoint.rotation;
        }
    }

    void TryAttachPatient()
    {
        // Etraftaki objeleri tara
        Collider[] hits = Physics.OverlapSphere(patientHoldPoint.position, lockRadius);
        foreach (var hit in hits)
        {
            GrabbableObject grabbable = hit.GetComponentInParent<GrabbableObject>();
            if (grabbable == null) grabbable = hit.GetComponent<GrabbableObject>();

            // Kendisi deðilse ve bir grabbable obje bulduysak
            if (grabbable != null && grabbable.gameObject != gameObject)
            {
                // ÖNCE OYUNCUNUN ELÝNDEN DÜÞÜRT (Burasý Çok Önemli)
                ForcePlayerToDrop();

                // Sonra Server'a "Bunu kilitle" de
                RequestAttachPatientServerRpc(grabbable.NetworkObjectId);
                return; // Ýlk bulduðunu al ve çýk
            }
        }
    }

    // Oyuncunun elindekini zorla býraktýran fonksiyon
    void ForcePlayerToDrop()
    {
        // Local oyuncuyu bul
        if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var playerGrab = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerGrab>();
            // Oyuncunun elinde bir þey varsa býraktýr
            if (playerGrab != null)
            {
                playerGrab.ForceDrop();
            }
        }
    }

    // --- SERVER TARAFI ---

    [ServerRpc(RequireOwnership = false)]
    void RequestAttachPatientServerRpc(ulong patientId)
    {
        // Eðer zaten doluysak iþlem yapma
        if (isFull.Value) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(patientId, out NetworkObject patientNetObj))
        {
            // Sahipliði kaldýr
            patientNetObj.RemoveOwnership();

            // Dolu olduðunu iþaretle
            isFull.Value = true;

            // Tüm clientlara bildir
            AttachClientRpc(patientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestDetachPatientServerRpc()
    {
        if (!isFull.Value) return;

        isFull.Value = false;
        DetachClientRpc();
    }

    // --- CLIENT TARAFI (Herkesin ekranýnda çalýþýr) ---

    [ClientRpc]
    void AttachClientRpc(ulong patientId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(patientId, out NetworkObject patientNetObj))
        {
            // Hastayý bul
            lockedPatient = patientNetObj.GetComponent<GrabbableObject>();
            if (lockedPatient == null) lockedPatient = patientNetObj.GetComponentInChildren<GrabbableObject>();

            if (lockedPatient != null)
            {
                // Fiziðini tamamen kapat (Titremeyi önler)
                Rigidbody rb = lockedPatient.GetComponent<Rigidbody>();
                if (rb == null) rb = lockedPatient.GetComponentInChildren<Rigidbody>();

                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                // Anýnda pozisyonu eþitle
                lockedPatient.transform.position = patientHoldPoint.position;
                lockedPatient.transform.rotation = patientHoldPoint.rotation;
            }
        }
    }

    [ClientRpc]
    void DetachClientRpc()
    {
        if (lockedPatient != null)
        {
            // Fiziðini geri aç
            Rigidbody rb = lockedPatient.GetComponent<Rigidbody>();
            if (rb == null) rb = lockedPatient.GetComponentInChildren<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = false;
            }

            // Deðiþkeni boþalt (Artýk LateUpdate çalýþmayacak)
            lockedPatient = null;
        }
    }
}