using Unity.Netcode;
using UnityEngine;

public class HospitalDropZone : NetworkBehaviour
{
    [Header("AYARLAR")]
    public int rewardAmount = 1000; // Canlý hasta parasý
    public string patientTag = "Patient"; // Hastanýn Tag'i

    private void OnTriggerEnter(Collider other)
    {
        // Sadece Server karar verir
        if (!IsServer) return;

        Transform rootObj = other.transform.root;

        // 1. DURUM: HASTA (Kucakta veya Yerde atýldýysa)
        if (rootObj.CompareTag(patientTag))
        {
            ProcessPatient(rootObj.gameObject);
        }
        // 2. DURUM: SEDYE (Üstünde hasta varsa)
        else if (rootObj.GetComponent<Stretcher>() != null)
        {
            // Sedyenin içindeki çocuk objeleri tara
            foreach (Transform child in rootObj)
            {
                if (child.CompareTag(patientTag))
                {
                    // Hastayý bulduk, iþlemi yap
                    ProcessPatient(child.gameObject);

                    // Sedyenin "Dolu" bilgisini sýfýrla ki tekrar kullanýlsýn
                    var stretcher = rootObj.GetComponent<Stretcher>();
                    if (stretcher != null) stretcher.isFull.Value = false;

                    break; // Bir hasta yetti, döngüden çýk
                }
            }
        }
    }

    // Hastayý inceleyip parayý verdiðimiz veya vermediðimiz yer
    void ProcessPatient(GameObject patientObj)
    {
        bool isAlive = true;

        // Hastanýn üzerindeki Can Scriptine ulaþ
        var healthScript = patientObj.GetComponent<PatientHealth>();

        if (healthScript != null)
        {
            // Eðer script varsa ve 'isDead' true ise -> Hasta ölmüþtür
            if (healthScript.isDead.Value)
            {
                isAlive = false;
            }
        }

        // --- KARAR ANI ---
        if (isAlive)
        {
            // Yaþýyorsa parayý ver
            AddReward();
            Debug.Log($"<color=green>CANLI HASTA TESLÝM EDÝLDÝ! +{rewardAmount} $</color>");
        }
        else
        {
            // Ölüyse para yok (Hatta istersen eksi puan yazabilirsin)
            Debug.Log("<color=red>HASTA EX OLMUÞ! PARA YOK.</color>");

            // Eðer ceza kesmek istersen þu satýrý aç:
            // if (GameManager.Instance != null) GameManager.Instance.AddMoney(-200);
        }

        // Sonuç ne olursa olsun hastayý oyundan sil (Despawn)
        if (patientObj.TryGetComponent(out NetworkObject netObj))
        {
            netObj.Despawn();
        }
    }

    void AddReward()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(rewardAmount);
        }
        else
        {
            Debug.LogError("Hata: Sahnede GameManager bulunamadý!");
        }
    }
}