using Unity.Netcode;
using UnityEngine;

public class HospitalDropZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Sadece sunucu karar verir

        // Ýçeri giren þey bir Sedye mi?
        // (Sedyenin içindeki colliderlar da girebilir, o yüzden InParent bakýyoruz)
        Stretcher stretcher = other.GetComponentInParent<Stretcher>();
        if (stretcher == null) stretcher = other.GetComponent<Stretcher>();

        if (stretcher != null)
        {
            CheckAndRescuePatient(stretcher);
        }
    }

    void CheckAndRescuePatient(Stretcher stretcher)
    {
        // Sedyenin "lockedPatient" deðiþkenine ulaþmamýz lazým.
        // Ama private olduðu için Stretcher scriptine minik bir ekleme yapacaðýz.
        // Þimdilik çocuklarýna bakarak bulalým (Daha garanti).

        // Sedyenin altýndaki GrabbableObject'i bul (Bu hastadýr)
        GrabbableObject patient = stretcher.GetComponentInChildren<GrabbableObject>();

        if (patient != null)
        {
            // HASTA VAR! KURTARMA BAÞLASIN.

            // 1. Puan Ver
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(100);
            }

            // 2. Hastayý Yok Et (Despawn)
            NetworkObject patientNetObj = patient.GetComponent<NetworkObject>();
            if (patientNetObj != null)
            {
                patientNetObj.Despawn(true); // True = Destroy
            }

            Debug.Log("HASTA KURTARILDI! +100 PUAN");
        }
    }
}