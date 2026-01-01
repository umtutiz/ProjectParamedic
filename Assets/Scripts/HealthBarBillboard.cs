using UnityEngine;

public class HealthBarBillboard : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        // Kendi kameramýzý buluyoruz
        mainCam = Camera.main;
        if (mainCam == null) mainCam = FindObjectOfType<Camera>();
    }

    // LateUpdate: Her þey (animasyon, fizik) bittikten sonra çalýþýr.
    // Böylece hasta yamulsa bile biz en son karede bunu düzeltiriz.
    void LateUpdate()
    {
        if (mainCam == null) return;

        // 1. ROTASYON KÝLÝDÝ:
        // Hastanýn nasýl durduðu umrumuzda deðil.
        // Bizim rotasyonumuz = Kameranýn rotasyonu.
        // Böylece yazý her zaman ekrana dimdik bakar.
        transform.rotation = mainCam.transform.rotation;
    }
}