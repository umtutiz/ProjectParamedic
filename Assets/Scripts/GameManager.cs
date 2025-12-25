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

    [Header("UI BAÐLANTILARI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI moneyText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    // NETCODE DEÐÝÞKENLERÝ
    private NetworkVariable<float> netTimeLeft = new NetworkVariable<float>(180f);

    // Bu sadece O ANKÝ maçýn parasý
    private NetworkVariable<int> currentMatchMoney = new NetworkVariable<int>(0);

    // TOPLAM ANA PARA (Bunu kayýttan çekeceðiz)
    private int localTotalBank = 0;

    private bool isGameActive = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        // 1. OYUN BAÞLARKEN ESKÝ PARAYI YÜKLE (LOAD)
        LoadMoney();

        if (IsServer)
        {
            netTimeLeft.Value = gameDuration;
            currentMatchMoney.Value = 0;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Ekrana baþlangýçta toplam paramýzý yazalým
        UpdateMoneyUI();
    }

    private void Update()
    {
        // UI GÜNCELLEME
        UpdateTimerUI();

        // Para yazýsýnda: "Maç Parasý (Toplam Banka)" þeklinde gösterebiliriz
        // Veya sadece maç parasýný gösteririz, tercih senin.
        if (moneyText != null)
        {
            // Örnek: 150 $ (Maçtaki kazanç)
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

    // --- OYUN BÝTÝÞÝ ---
    void EndGame()
    {
        isGameActive = false;
        // Server herkese "Oyun bitti, þu kadar kazandýnýz" der
        EndGameClientRpc(currentMatchMoney.Value);
    }

    [ClientRpc]
    void EndGameClientRpc(int matchEarnings)
    {
        // 1. MAÇ PARASINI KUMBARAYA EKLE VE KAYDET (AUTO SAVE)
        localTotalBank += matchEarnings;
        SaveMoney(); // <--- ÝÞTE KAYIT BURADA YAPILIYOR

        // 2. Paneli Aç
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Skor Tablosu: "Kazanç: 500 $ | Toplam Servet: 15000 $"
            if (finalScoreText != null)
            {
                finalScoreText.text = $"KAZANÇ: {matchEarnings} $\nTOPLAM SERVET: {localTotalBank} $";
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer != null)
        {
            localPlayer.GetComponent<FirstPersonController>().enabled = false;
        }
    }

    // --- KAYIT SÝSTEMÝ (PLAYER PREFS) ---

    // Parayý Kaydet
    void SaveMoney()
    {
        PlayerPrefs.SetInt("MyTotalMoney", localTotalBank);
        PlayerPrefs.Save();
        Debug.Log("Oyun Kaydedildi! Yeni Bakiye: " + localTotalBank);
    }

    // Parayý Yükle
    void LoadMoney()
    {
        // Eðer daha önce kayýt varsa yükle, yoksa 0 yap
        if (PlayerPrefs.HasKey("MyTotalMoney"))
        {
            localTotalBank = PlayerPrefs.GetInt("MyTotalMoney");
            Debug.Log("Kayýt Yüklendi. Bakiye: " + localTotalBank);
        }
        else
        {
            localTotalBank = 0;
        }
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null) moneyText.text = "0 $"; // Baþlangýçta 0 görünür
    }
}