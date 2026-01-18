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
        // 1. R TUÞU KONTROLÜ
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (lockedPatient != null)
            {
                RequestDetachPatientServerRpc();
            }
            else
            {
                TryAttachPatient();
            }
        }

        // 2. HARD LOCK (Japon Yapýþtýrýcýsý)
        if (lockedPatient != null)
        {
            lockedPatient.transform.position = patientHoldPoint.position;
            lockedPatient.transform.rotation = patientHoldPoint.rotation;
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
                ForcePlayerToDrop();
                RequestAttachPatientServerRpc(grabbable.NetworkObjectId);
                return;
            }
        }
    }

    void ForcePlayerToDrop()
    {
        if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var playerGrab = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerGrab>();
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
        if (isFull.Value) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(patientId, out NetworkObject patientNetObj))
        {
            patientNetObj.RemoveOwnership();
            isFull.Value = true;
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

    // --- CLIENT TARAFI ---

    [ClientRpc]
    void AttachClientRpc(ulong patientId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(patientId, out NetworkObject patientNetObj))
        {
            lockedPatient = patientNetObj.GetComponent<GrabbableObject>();
            if (lockedPatient == null) lockedPatient = patientNetObj.GetComponentInChildren<GrabbableObject>();

            if (lockedPatient != null)
            {
                // <--- KAMERA SARSINTISI (YERLEÞME HÝSSÝ) --->
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f, 0.3f);

                Rigidbody rb = lockedPatient.GetComponent<Rigidbody>();
                if (rb == null) rb = lockedPatient.GetComponentInChildren<Rigidbody>();

                if (rb != null) rb.isKinematic = true;

                lockedPatient.transform.position = patientHoldPoint.position;
                lockedPatient.transform.rotation = patientHoldPoint.rotation;
            }
        }
    }

    [ClientRpc]
    void DetachClientRpc()
    {
        // <--- KAMERA SARSINTISI (AYRILMA HÝSSÝ) --->
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.2f);

        if (lockedPatient != null)
        {
            Rigidbody rb = lockedPatient.GetComponent<Rigidbody>();
            if (rb == null) rb = lockedPatient.GetComponentInChildren<Rigidbody>();

            if (rb != null) rb.isKinematic = false;

            lockedPatient = null;
        }
    }
}