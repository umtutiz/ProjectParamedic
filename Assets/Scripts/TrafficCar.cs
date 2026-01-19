using UnityEngine;
using Unity.Netcode;

public class TrafficCar : NetworkBehaviour
{
    public float speed = 15f;
    public float hitForce = 2000f; // Ne kadar sert vursun?
    public float lifeTime = 10f;   // Kaç saniye sonra yok olsun?

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 10 saniye sonra otomatik yok et (Performans için)
            Invoke(nameof(KillSelf), lifeTime);
        }
    }

    void Update()
    {
        if (!IsServer) return;

        // Dümdüz ileri git (Basit AI)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // Çarptığımız şeyin Rigidbody'si var mı? (Oyuncu veya Sedye)
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) rb = other.GetComponentInParent<Rigidbody>();

        if (rb != null)
        {
            // 1. FİZİKSEL PATLAMA (Uçurma)
            Vector3 dir = (other.transform.position - transform.position).normalized;
            dir.y = 0.5f; // Biraz havaya doğru vursun
            rb.AddForce(dir * hitForce, ForceMode.Impulse);

            // 2. HASAR VERME
            // Eğer sedyeyse veya hastaysa
            PatientHealth patient = other.GetComponent<PatientHealth>();
            if (patient == null) patient = other.GetComponentInChildren<PatientHealth>();

            if (patient != null)
            {
                patient.TakeDamage(50f); // Araba çarptı, yarısı gitsin canın
                Debug.Log("ARABA HASTAYA ÇARPTI!");
            }

            // Çarptıktan sonra araba yok olsun mu? (Bence olsun, yoksa içinden geçer)
            KillSelf();
        }
    }

    void KillSelf()
    {
        if (IsSpawned) GetComponent<NetworkObject>().Despawn();
    }
}