using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PatientHealth : NetworkBehaviour
{
    [Header("YAÞAM AYARLARI")]
    public float maxLifeTime = 60f;

    public NetworkVariable<float> currentLifeTime = new NetworkVariable<float>(60f);
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);

    [Header("GÖRSEL")]
    public Image healthBarFill;
    public GameObject healthCanvas;
    public Renderer patientRenderer;

    // Azrail bir kere gelsin diye kilit
    private bool azraelTriggered = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentLifeTime.Value = maxLifeTime;
            isDead.Value = false;
        }
    }

    void Update()
    {
        UpdateUI();

        if (IsServer)
        {
            if (isDead.Value) return;

            currentLifeTime.Value -= Time.deltaTime;

            // --- YENÝ EKLENEN KISIM: AZRAÝL KONTROLÜ ---
            // Eðer süre 15 saniyenin altýna düþtüyse ve Azrail daha önce gelmediyse
            if (currentLifeTime.Value <= 15f && !azraelTriggered)
            {
                if (AzraelArenaManager.Instance != null)
                {
                    azraelTriggered = true; // Kilit vur
                    Debug.Log("HASTA GÝDÝCÝ! AZRAÝL GELÝYOR...");

                    // Azrail Manager'a "Benim için savaþ baþlat" diyoruz
                    AzraelArenaManager.Instance.StartAzraelEvent(this);
                }
            }
            // -------------------------------------------

            if (currentLifeTime.Value <= 0)
            {
                currentLifeTime.Value = 0;
                Die();
            }
        }
    }

    void UpdateUI()
    {
        if (healthBarFill != null)
        {
            float fill = currentLifeTime.Value / maxLifeTime;
            healthBarFill.fillAmount = fill;
            healthBarFill.color = Color.Lerp(Color.red, Color.green, fill);
        }
    }

    // --- YENÝ EKLENEN: AZRAÝL SAVAÞI KAZANILIRSA ÇAÐRILACAK ---
    public void Heal(float amount)
    {
        if (!IsServer) return;

        currentLifeTime.Value += amount;
        if (currentLifeTime.Value > maxLifeTime) currentLifeTime.Value = maxLifeTime;

        // Kritik seviyenin (15 sn) üstüne çýkarsa kilidi aç, tekrar Azrail gelebilsin
        if (currentLifeTime.Value > 15f) azraelTriggered = false;
    }

    // --- YENÝ EKLENEN: AZRAÝL SAVAÞI KAYBEDÝLÝRSE ÇAÐRILACAK ---
    public void KillPatient()
    {
        if (!IsServer) return;
        currentLifeTime.Value = 0;
        Die();
    }

    void Die()
    {
        isDead.Value = true;

        // Eðer Azrail sahnesindeysek oradan da temizleyelim (Opsiyonel)

        DieClientRpc();

        // Ýstersen hastayý komple yok et:
        // GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    void DieClientRpc()
    {
        if (patientRenderer != null) patientRenderer.material.color = Color.gray;
        if (healthCanvas != null) healthCanvas.SetActive(false);
        Debug.Log("HASTA EX OLDU! BAÞIMIZ SAÐ OLSUN.");
    }
}