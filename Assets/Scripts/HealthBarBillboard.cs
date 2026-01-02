using UnityEngine;

public class HealthBarBillboard : MonoBehaviour
{
    [Header("TAKÝP AYARLARI")]
    // Hastanýn kafasýný buraya sürükleyeceksin
    public Transform targetBone;

    // Kafanýn ne kadar üstünde dursun? (Örn: Y = 0.5 veya 1)
    public Vector3 offset = new Vector3(0, 0.5f, 0);

    private Camera mainCam;

    void Start()
    {
        // Kamerayý bul
        mainCam = Camera.main;
        if (mainCam == null) mainCam = FindObjectOfType<Camera>();

        // EÐER TARGET BOÞSA OTOMATÝK BULMAYA ÇALIÞ (Garanti olsun)
        if (targetBone == null)
        {
            // Eðer scripti Canvas'a attýysan ve Canvas hastanýn içindeyse,
            // Hastanýn "Head" isimli kemiðini bulmaya çalýþýrýz.
            Transform root = transform.root; // En tepeye (Patient) çýk
            // Tüm çocuklarý tara ve "Head" geçen bir kemik bul
            foreach (Transform t in root.GetComponentsInChildren<Transform>())
            {
                if (t.name.Contains("Head") || t.name.Contains("head"))
                {
                    targetBone = t;
                    break;
                }
            }
            // Bulamazsa hastanýn kendisini hedef al
            if (targetBone == null) targetBone = root;
        }

        // ÖNEMLÝ: Canvas'ý hastanýn içinden koparýyoruz!
        // Böylece hasta takla atsa bile Canvas baðýmsýz hareket eder.
        transform.SetParent(null);
    }

    void LateUpdate()
    {
        // Hedef yok olduysa (Hasta öldüyse/silindiyse) barý da yok et
        if (targetBone == null)
        {
            Destroy(gameObject);
            return;
        }

        if (mainCam == null) return;

        // 1. POZÝSYON TAKÝBÝ:
        // Hedef kemiðin pozisyonu + Offset
        transform.position = targetBone.position + offset;

        // 2. ROTASYON KÝLÝDÝ:
        // Hep kameraya bak
        transform.rotation = mainCam.transform.rotation;
    }
}