using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Stretcher : NetworkBehaviour
{
    [Header("AYARLAR")]
    public Transform patientHoldPoint;
    public float lockRadius = 3.0f;

    private GrabbableObject lockedPatient;
    public NetworkVariable<bool> isFull = new NetworkVariable<bool>(false);

    void LateUpdate()
    {
        // R TUÞU
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (lockedPatient != null)
                RequestDetachPatientServerRpc();
            else
                TryAttachPatient();
        }

        // HARD LOCK (Japon Yapýþtýrýcýsý)
        if (lockedPatient != null)
        {
            if (patientHoldPoint != null)
            {
                lockedPatient.transform.position = patientHoldPoint.position;
                lockedPatient.transform.rotation = patientHoldPoint.rotation;
            }
        }
    }

    void TryAttachPatient()
    {
        Collider[] hits = Physics.OverlapSphere(patientHoldPoint.position, lockRadius);
        foreach (var hit in hits)
        {
            GrabbableObject grabbable = hit.GetComponentInParent<GrabbableObject>();
            if (grabbable == null) grabbable = hit.GetComponent<GrabbableObject>();

            if (grabbable != null && grabbable.gameObject != gameObject)
            {
                Debug.Log("BULUNDU: " + grabbable.name);
                ForcePlayerToDrop(grabbable);
                RequestAttachPatientServerRpc(grabbable.NetworkObjectId);
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
    void RequestAttachPatientServerRpc(ulong patientId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(patientId, out NetworkObject patientNetObj))
        {
            // Sahipliði sil (Server yönetsin)
            patientNetObj.RemoveOwnership();
            patientNetObj.TrySetParent(patientHoldPoint);
            AttachClientRpc(patientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestDetachPatientServerRpc()
    {
        if (lockedPatient == null) return;

        // Null check ekledik
        if (lockedPatient.TryGetComponent(out NetworkObject netObj))
        {
            netObj.TryRemoveParent();
            DetachClientRpc(netObj.NetworkObjectId);
        }
    }

    // --- CLIENT (HATA BURADAYDI, DÜZELTÝLDÝ) ---

    [ClientRpc]
    void AttachClientRpc(ulong patientId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(patientId, out NetworkObject patientNetObj))
        {
            // HATA ÇÖZÜMÜ 1: Scripti sadece kendinde deðil, çocuklarýnda da ara!
            lockedPatient = patientNetObj.GetComponent<GrabbableObject>();
            if (lockedPatient == null) lockedPatient = patientNetObj.GetComponentInChildren<GrabbableObject>();

            // HATA ÇÖZÜMÜ 2: Hala bulamadýysa oyunu çökertme, iþlemi iptal et.
            if (lockedPatient == null)
            {
                Debug.LogError("HATA: GrabbableObject scripti bulunamadý! ID: " + patientId);
                return;
            }

            // 1. NetworkTransform'u KAPAT (Varsa)
            NetworkTransform netTransform = lockedPatient.GetComponent<NetworkTransform>();
            if (netTransform != null) netTransform.enabled = false;

            // 2. Fiziði KAPAT
            Rigidbody[] rbs = lockedPatient.GetComponentsInChildren<Rigidbody>();
            Collider[] cols = lockedPatient.GetComponentsInChildren<Collider>();

            foreach (var rb in rbs)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
            }
            foreach (var col in cols) col.enabled = false;

            // 3. Konumu eþitle
            lockedPatient.transform.position = patientHoldPoint.position;
            lockedPatient.transform.rotation = patientHoldPoint.rotation;

            Debug.Log("KÝLÝTLENDÝ: " + lockedPatient.name);
        }
    }

    [ClientRpc]
    void DetachClientRpc(ulong patientId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(patientId, out NetworkObject patientNetObj))
        {
            // Geri açarken de null check yapýyoruz
            NetworkTransform netTransform = patientNetObj.GetComponent<NetworkTransform>();
            if (netTransform != null) netTransform.enabled = true;

            Rigidbody[] rbs = patientNetObj.GetComponentsInChildren<Rigidbody>();
            Collider[] cols = patientNetObj.GetComponentsInChildren<Collider>();

            foreach (var rb in rbs) rb.isKinematic = false;
            foreach (var col in cols) col.enabled = true;

            lockedPatient = null;
            Debug.Log("KÝLÝT AÇILDI!");
        }
    }
}