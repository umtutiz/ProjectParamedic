using UnityEngine;
using Unity.Netcode;

public class QuestMarker : MonoBehaviour
{
    [Header("AYARLAR")]
    public Transform player; // Karakterimiz (Kod otomatik bulacak)
    public float hideDistance = 3.0f; // Hastaya ne kadar yaklaþýnca ok kaybolsun?

    private Transform target; // Hedef (Hasta)
    private RectTransform arrowRect;

    void Start()
    {
        arrowRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        // 1. OYUNCUYU BUL (Eðer yoksa)
        if (player == null)
        {
            // Tag ile player'ý bulmaya çalýþ
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
            }
            else
            {
                // Player henüz doðmadýysa oku gizle
                ToggleArrow(false);
                return;
            }
        }

        // 2. HEDEFÝ BUL (Hasta var mý?)
        if (target == null)
        {
            FindClosestPatient();
            if (target == null)
            {
                // Sahnede hasta yoksa oku gizle
                ToggleArrow(false);
                return;
            }
        }

        // 3. OKU ÇEVÝR
        ToggleArrow(true);
        RotateArrowToTarget();

        // 4. HASTA DÝBÝMÝZDEYSE GÝZLE
        float dist = Vector3.Distance(player.position, target.position);
        if (dist < hideDistance)
        {
            ToggleArrow(false);
        }
    }

    void FindClosestPatient()
    {
        // Sahnede "Patient" tag'li objeyi bul
        GameObject patient = GameObject.FindGameObjectWithTag("Patient");
        if (patient != null)
        {
            target = patient.transform;
        }
    }

    void RotateArrowToTarget()
    {
        // Oyuncudan Hedefe giden yönü hesapla
        Vector3 direction = target.position - player.position;

        // Bu yönü Player'ýn baktýðý yöne göre yerel hale getir
        // (Karakter döndükçe ok da ona göre dönmeli)
        Vector3 localDir = player.InverseTransformDirection(direction);

        // 2D açýyý hesapla (Atan2 fonksiyonu)
        float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        // Oku çevir (Z ekseninde ters çeviriyoruz ki UI düzgün dursun)
        arrowRect.localRotation = Quaternion.Euler(0, 0, -angle);
    }

    void ToggleArrow(bool state)
    {
        // Image componentini aç/kapa
        var img = GetComponent<UnityEngine.UI.Image>();
        if (img != null && img.enabled != state)
        {
            img.enabled = state;
        }

        // Ýçindeki yazýlarý vs de kapatmak istersen:
        // transform.GetChild(0).gameObject.SetActive(state);
    }
}