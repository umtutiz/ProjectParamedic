using UnityEngine;
using TMPro; // TextMeshPro kullanýyorsan bunu aç, normal Text ise 'using UnityEngine.UI;' kullan
// Eðer hata verirse yukarýdaki TMPro satýrýný sil, 'using UnityEngine.UI;' ekle.

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Ayarlarý")]
    public TextMeshProUGUI moneyText; // Normal Text kullanýyorsan burayý 'Text' yap
    public int currentMoney = 0;

    private void Awake()
    {
        // Singleton (Sahneler arasý geçiþte yok olmasýn istiyorsan DontDestroyOnLoad ekle, ama þimdilik gerek yok)
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        currentMoney += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (moneyText != null)
        {
            // Ýþte burasý. Ýstediðin gibi MONEY yazýyor.
            moneyText.text = "MONEY: $" + currentMoney.ToString();
        }
    }
}