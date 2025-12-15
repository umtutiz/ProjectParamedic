using Unity.Netcode;
using UnityEngine;
using TMPro; // TextMeshPro için gerekli

public class GameManager : NetworkBehaviour
{
    // Singleton yapýsý: Her yerden GameManager.Instance diye ulaþabilmek için
    public static GameManager Instance;

    [Header("UI Settings")]
    [SerializeField] private TextMeshProUGUI scoreText; // Inspector'dan ScoreText'i buraya sürükle

    // Skoru að üzerinde senkronize tutan deðiþken
    // Sadece sunucu yazabilir (NetworkVariableWritePermission.Server)
    private NetworkVariable<int> totalScore = new NetworkVariable<int>(0);

    private void Awake()
    {
        // Singleton atamasý
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        // Skor deðiþtiðinde (herhangi bir oyuncu sayý yaparsa) UI'ý güncelle
        totalScore.OnValueChanged += (oldVal, newVal) =>
        {
            UpdateScoreUI(newVal);
        };

        // Oyun baþladýðýnda mevcut skoru yazdýr
        UpdateScoreUI(totalScore.Value);
    }

    // Sedyeden çaðýracaðýmýz fonksiyon bu
    public void AddScore(int amount)
    {
        // Sadece sunucu skoru deðiþtirebilir
        if (!IsServer) return;

        totalScore.Value += amount;
    }

    private void UpdateScoreUI(int currentScore)
    {
        // Bu kod her oyuncunun kendi ekranýnda çalýþýr
        scoreText.text = "Para: " + currentScore.ToString() + "$";
    }
}