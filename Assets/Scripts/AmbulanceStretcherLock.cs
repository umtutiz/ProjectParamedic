using Unity.Netcode;
using UnityEngine;

public class AmbulanceStretcherLock : NetworkBehaviour
{
    [Header("Ayarlar")]
    // Inspector'da oluþturduðun o boþ 'PatientPoint' objesini buraya sürüklemeyi UNUTMA!
    [SerializeField] private Transform patientPoint;

    // Sedye dolu mu boþ mu kontrolü
    // Inspector'da Is Full tikinin KALKIK (Boþ) olduðundan emin ol.
    public NetworkVariable<bool> isFull = new NetworkVariable<bool>(false);

    // Baþlangýçta çalýþacak kod
    public override void OnNetworkSpawn()
    {
        // Eðer gerekirse görsel güncelleme kodlarý buraya
        // Þimdilik sadece logic çalýþýyor
    }

    // Dýþarýdan gelen gerçek hastayý sedyeye monte eder
    // PlayerGrab scripti burayý çaðýrýr
    public void PlacePatientReal(NetworkObject patientNetObj)
    {
        if (isFull.Value) return; // Zaten doluysa alma

        // 1. Durumu dolu yap
        isFull.Value = true;

        // 2. Hastayý Netcode uyumlu þekilde sedyenin çocuðu yap (Parenting)
        patientNetObj.TrySetParent(patientPoint);

        // 3. Pozisyonu ve açýyý sýfýrla (Tam noktaya otursun)
        patientNetObj.transform.localPosition = Vector3.zero;
        patientNetObj.transform.localRotation = Quaternion.identity;

        // 4. Hastanýn fiziðini kapat (Kýpýrdamasýn, donuk kalsýn)
        Rigidbody rb = patientNetObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Fiziði kapat
            rb.detectCollisions = false; // Çarpýþmayý kapat
        }

        // Puan ver
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(500);
        }
    }
}