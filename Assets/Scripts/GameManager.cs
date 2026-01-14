using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("AYARLAR")]
    public float gameDuration = 180f; // 3 Dakika
    public int startingMoney = 500;   // YENÝ: Baþlangýç parasý (Market testi için)

    [Header("UI BAÐLANTILARI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI moneyText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    // NETCODE DEÐÝÞKENLERÝ
    private NetworkVariable<float> netTimeLeft = new NetworkVariable<float>(180f);

    // Bu O ANKÝ maçýn harcanabilir parasý (Hem cüzdan hem skor)
    public NetworkVariable<int> currentMatchMoney = new NetworkVariable<int>(0);

    // TOPLAM ANA PARA (Kayýttan çekilen)
    private int localTotalBank = 0;

    private bool isGameActive = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        // 1. OYUN BAÞLARKEN ESKÝ PARAYI YÜKLE
        LoadMoney();

        if (IsServer)
        {
            netTimeLeft.Value = gameDuration;

            // YENÝ: Baþlangýçta 0 deðil, belirlenen parayla baþla (Marketten eþya alabilmek için)
            currentMatchMoney.Value = startingMoney;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateMoneyUI();
    }

    private void Update()
    {
        UpdateTimerUI();

        // Para yazýsýný güncelle
        if (moneyText != null)
        {
            moneyText.text = currentMatchMoney.Value.ToString() + " $";
        }

        // SÜRE SAYIMI (Sadece Server)
        if (IsServer && isGameActive)
        {
            netTimeLeft.Value -= Time.deltaTime;

            if (netTimeLeft.Value <= 0)
            {
                netTimeLeft.Value = 0;
                EndGame();
            }
        }
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        float t = netTimeLeft.Value;
        string minutes = Mathf.Floor(t / 60).ToString("00");
        string seconds = (t % 60).ToString("00");
        timerText.text = $"{minutes}:{seconds}";

        if (t <= 10) timerText.color = Color.red;
        else timerText.color = Color.white;
    }

    // --- PARA EKLEME ---
    public void AddMoney(int amount)
    {
        if (!IsServer) return;
        currentMatchMoney.Value += amount;
    }

    // --- YENÝ EKLENEN: PARA HARCAMA (MARKET ÝÇÝN) ---
    // Bu fonksiyonu MarketSystem çaðýracak
    public bool TrySpendMoney(int amount)
    {
        if (!IsServer) return false;

        // Paramýz yetiyor mu?
        if (currentMatchMoney.Value >= amount)
        {
            currentMatchMoney.Value -= amount; // Parayý düþ
            return true; // Ýþlem baþarýlý
        }
        else
        {
            return false; // Yetersiz bakiye
        }
    }
    // ------------------------------------------------

    // --- OYUN BÝTÝÞÝ ---
    void EndGame()
    {
        isGameActive = false;
        // Server herkese "Oyun bitti, þu kadar kazandýnýz (kalan para)" der
        EndGameClientRpc(currentMatchMoney.Value);
    }

    [ClientRpc]
    void EndGameClientRpc(int remainingMoney)
    {
        // Kalan parayý bankaya ekle
        localTotalBank += remainingMoney;
        SaveMoney();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (finalScoreText != null)
            {
                finalScoreText.text = $"KALAN PARA: {remainingMoney} $\nTOPLAM SERVET: {localTotalBank} $";
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Karakter kontrolcüsünü bul ve kapat (Hareket edemesin)
        // Not: Burada kendi Player karakterini bulma yöntemin neyse onu kullan
        // Örnek: NetworkManager.Singleton.LocalClient.PlayerObject...
    }

    // --- KAYIT SÝSTEMÝ ---
    void SaveMoney()
    {
        PlayerPrefs.SetInt("MyTotalMoney", localTotalBank);
        PlayerPrefs.Save();
        Debug.Log("Kayýt Edildi. Yeni Toplam: " + localTotalBank);
    }

    void LoadMoney()
    {
        if (PlayerPrefs.HasKey("MyTotalMoney"))
        {
            localTotalBank = PlayerPrefs.GetInt("MyTotalMoney");
        }
        else
        {
            localTotalBank = 0;
        }
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null) moneyText.text = "0 $";
    }
}