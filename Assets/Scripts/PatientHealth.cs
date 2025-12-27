using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PatientHealth : NetworkBehaviour
{
    [Header("YAÞAM AYARLARI")]
    public float maxLifeTime = 60f; // Hastanýn ölmesi için kaç saniye var?

    // Herkesin göreceði can deðiþkeni
    public NetworkVariable<float> currentLifeTime = new NetworkVariable<float>(60f);
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);

    [Header("GÖRSEL")]
    public Image healthBarFill; // Yeþil barý buraya sürükle
    public GameObject healthCanvas; // Tüm barý (Canvas'ý) buraya sürükle
    public Renderer patientRenderer; // Hastanýn rengini deðiþtirmek için (MeshRenderer)

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
        // 1. CAN BARI GÜNCELLEME (Herkes görür)
        UpdateUI();

        // 2. ÖLÜM KONTROLÜ (Sadece Server)
        if (IsServer)
        {
            if (isDead.Value) return; // Zaten öldüyse iþlem yapma

            currentLifeTime.Value -= Time.deltaTime;

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
            // Yüzde hesapla (0 ile 1 arasý)
            float fill = currentLifeTime.Value / maxLifeTime;
            healthBarFill.fillAmount = fill;

            // Can azaldýkça renk deðiþsin (Yeþil -> Kýrmýzý)
            healthBarFill.color = Color.Lerp(Color.red, Color.green, fill);
        }
    }

    void Die()
    {
        isDead.Value = true;

        // Öldüðünü belli et (Herkes görsün diye ClientRpc lazým)
        DieClientRpc();
    }

    [ClientRpc]
    void DieClientRpc()
    {
        // 1. Rengi Karart (Ölü gibi olsun)
        if (patientRenderer != null)
        {
            patientRenderer.material.color = Color.gray; // Veya Siyah
        }

        // 2. Barý gizle (Ölünün caný olmaz)
        if (healthCanvas != null)
        {
            healthCanvas.SetActive(false);
        }

        Debug.Log("HASTA EX OLDU! BAÞIMIZ SAÐ OLSUN.");
    }
}