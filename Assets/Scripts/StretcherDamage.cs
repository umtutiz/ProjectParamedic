using Unity.Netcode;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

public class StretcherDamage : NetworkBehaviour
{
    [Header("AYARLAR")]
    public float damageThreshold = 5f; // Ne kadar sert çarparsa hasar alsın? (5 iyidir)
    public float damageMultiplier = 2f; // Hasar çarpanı (Hız x 2 = Hasar)
    public float cooldown = 0.5f; // Arka arkaya hasar almasın diye bekleme süresi

    private float lastDamageTime;

    // Sedyenin üzerindeki hastayı bulmak için
    private PatientHealth currentPatient;

    // Çarpışma olduğunda Unity otomatik çağırır
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return; // Hasarı sadece server hesaplar

        // Çarpışma şiddetini al (Hız farkı)
        float impactForce = collision.relativeVelocity.magnitude;

        // Eğer şiddet eşiği geçtiyse VE bekleme süresi dolduysa
        if (impactForce > damageThreshold && Time.time > lastDamageTime + cooldown)
        {
            // Hastayı bulmaya çalış (Sedyenin içinde child olabilir)
            if (currentPatient == null) currentPatient = GetComponentInChildren<PatientHealth>();

            if (currentPatient != null)
            {
                // Hasarı hesapla: (Şiddet - Eşik) * Çarpan
                float damage = (impactForce - damageThreshold) * damageMultiplier;

                // Hastaya vur
                currentPatient.TakeDamage(damage);

                lastDamageTime = Time.time;
                Debug.Log($"KAZA YAPTIK! Şiddet: {impactForce}, Hasar: {damage}");

                // Varsa Kamera Sarsıntısı çağır (Herkes hissetsin)
                ShakeClientRpc();
            }
        }
    }

    // Hasta sedyeye konunca scripti güncellemek için (Opsiyonel ama garanti)
    public void RefreshPatient()
    {
        currentPatient = GetComponentInChildren<PatientHealth>();
    }

    [ClientRpc]
    void ShakeClientRpc()
    {
        // Eğer CameraShake scriptin varsa çalıştır
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.2f, 0.5f);
        }
    }
}