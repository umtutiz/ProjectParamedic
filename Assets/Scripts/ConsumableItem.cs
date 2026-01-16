using UnityEngine;
using Unity.Netcode;

public class ConsumableItem : NetworkBehaviour
{
    public enum ItemType { Adrenaline, Bribe, Battery }
    public ItemType itemType;

    // Oyuncu SAĞ TIK yaptığında PlayerGrab bu fonksiyonu çağıracak
    public void UseItem(GameObject player)
    {
        UseItemServerRpc(player.GetComponent<NetworkObject>().OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    void UseItemServerRpc(ulong playerId)
    {
        // 1. ADRENALİN: Yakındaki hastayı iyileştir
        if (itemType == ItemType.Adrenaline)
        {
            // Etrafta hasta var mı?
            Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
            foreach (var hit in hits)
            {
                PatientHealth patient = hit.GetComponent<PatientHealth>(); // Senin script ismin
                if (patient == null) patient = hit.GetComponentInParent<PatientHealth>();

                if (patient != null)
                {
                    patient.Heal(20f); // +20 Saniye ekle
                    DestroyItem();
                    return;
                }
            }
        }

        // 2. RÜŞVET ZARFI: Azrail eventini iptal et (Koruma kalkanı)
        else if (itemType == ItemType.Bribe)
        {
            // AzraelManager'a "Bir sonraki savaşı iptal et" de
            if (AzraelArenaManager.Instance != null)
            {
                AzraelArenaManager.Instance.EnableBribeMode();
                DestroyItem();
            }
        }

        // 3. PİL: Şok cihazını kolaylaştır
        else if (itemType == ItemType.Battery)
        {
            if (AzraelArenaManager.Instance != null)
            {
                AzraelArenaManager.Instance.UpgradeBattery();
                DestroyItem();
            }
        }
    }

    void DestroyItem()
    {
        // Eşyayı yok et
        GetComponent<NetworkObject>().Despawn();
    }
}