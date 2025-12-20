using Unity.Netcode;
using UnityEngine;

public class HospitalDropZone : NetworkBehaviour
{
    [Header("AYARLAR")]
    public int rewardAmount = 1000; // Hasta baþý kaç para?
    public string patientTag = "Patient"; // Hastanýn Tag'i ne?

    private void OnTriggerEnter(Collider other)
    {
        // Sadece Server karar verir (Hile olmasýn diye)
        if (!IsServer) return;

        // Giren þeyin köküne bak (Çünkü collider kolda bacakta olabilir)
        Transform rootObj = other.transform.root;

        // 1. Giren þey bir "Hasta" mý? (Tag kontrolü)
        if (rootObj.CompareTag(patientTag))
        {
            // Network objesini al
            if (rootObj.TryGetComponent(out NetworkObject patientNetObj))
            {
                // Hastayý oyundan sil (Despawn)
                patientNetObj.Despawn();

                // Parayý ver
                AddReward();
            }
        }
        // 2. Veya giren þey "Sedye" mi? (Sedye içindeki hastayý bulalým)
        else if (rootObj.GetComponent<Stretcher>() != null) // Sedyede bu script var diye referans aldým
        {
            // Sedyenin çocuklarýný tara, hasta var mý?
            foreach (Transform child in rootObj)
            {
                if (child.CompareTag(patientTag))
                {
                    if (child.TryGetComponent(out NetworkObject childNetObj))
                    {
                        childNetObj.Despawn(); // Sadece hastayý sil, sedye kalsýn
                        AddReward();

                        // Sedyenin "Dolu" deðiþkenini boþalt
                        var stretcher = rootObj.GetComponent<Stretcher>();
                        if (stretcher != null) stretcher.isFull.Value = false;

                        break; // Bir hasta yetti
                    }
                }
            }
        }
    }

    void AddReward()
    {
        // GameManager varsa parayý ekle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(rewardAmount);
        }
        else
        {
            Debug.LogError("Kanka sahneye GameManager koymayý unuttun!");
        }
    }
}