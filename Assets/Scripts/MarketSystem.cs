using UnityEngine;
using Unity.Netcode;

public class MarketSystem : NetworkBehaviour
{
    [Header("AYARLAR")]
    public GameObject marketUI; // Panel
    public Transform itemSpawnPoint; // Eşyaların düşeceği nokta (Masanın önü)

    [Header("EŞYA PREFABLARI")]
    public GameObject adrenalinePrefab; // İğne Modeli
    public GameObject bribePrefab;      // Zarf Modeli
    public GameObject batteryPrefab;    // Pil Modeli

    [Header("FİYATLAR")]
    public int adrenalinePrice = 200;
    public int bribePrice = 500;
    public int batteryPrice = 150;

    private bool isMarketOpen = false;

    void Update()
    {
        // "B" tuşu ile marketi aç/kapat (Sadece Local Player için)
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleMarket();
        }
    }

    public void ToggleMarket()
    {
        isMarketOpen = !isMarketOpen;
        marketUI.SetActive(isMarketOpen);

        // Mouse imlecini aç/kapat
        Cursor.lockState = isMarketOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isMarketOpen;
    }

    // --- BUTON FONKSİYONLARI ---

    public void BuyAdrenaline() { BuyItemServerRpc(1); }
    public void BuyBribe() { BuyItemServerRpc(2); }
    public void BuyBattery() { BuyItemServerRpc(3); }

    [ServerRpc(RequireOwnership = false)]
    void BuyItemServerRpc(int itemId)
    {
        int cost = 0;
        GameObject prefabToSpawn = null;

        switch (itemId)
        {
            case 1: cost = adrenalinePrice; prefabToSpawn = adrenalinePrefab; break;
            case 2: cost = bribePrice; prefabToSpawn = bribePrefab; break;
            case 3: cost = batteryPrice; prefabToSpawn = batteryPrefab; break;
        }

        // GameManager'dan para çekmeye çalış
        if (GameManager.Instance.TrySpendMoney(cost))
        {
            // Başarılı! Eşyayı oluştur
            GameObject newItem = Instantiate(prefabToSpawn, itemSpawnPoint.position, Quaternion.identity);
            newItem.GetComponent<NetworkObject>().Spawn();

            Debug.Log("Satın alma başarılı!");
        }
        else
        {
            Debug.Log("Para Yetersiz!");
        }
    }
}