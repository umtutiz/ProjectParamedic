using System.Collections;
using TMPro; // TextMeshPro için þart
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI BAÐLANTILARI")]
    public TextMeshProUGUI scoreText; // Sað üstteki skor
    public TextMeshProUGUI popUpText; // Ortada çýkacak +1000 yazýsý

    [Header("AYARLAR")]
    public float countSpeed = 1.0f; // Sayý sayma hýzý

    private int currentScore = 0; // Þu anki paramýz
    private int displayedScore = 0; // Ekranda görünen sayý (Animasyon için)

    private void Awake()
    {
        // Singleton (Her yerden eriþebilmek için)
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- SUNUCU KISMI: Parayý Verir ve Emreder ---
    public void AddMoney(int amount)
    {
        if (!IsServer) return;

        // Parayý ekle
        currentScore += amount;

        // Tüm oyunculara (Clientlara) haber ver: "Animasyonu baþlatýn!"
        UpdateScoreClientRpc(currentScore, amount);
    }

    // --- OYUNCU KISMI: Animasyonu Oynatýr ---
    [ClientRpc]
    void UpdateScoreClientRpc(int newTotalScore, int addedAmount)
    {
        // 1. Hedef skoru güncelle
        currentScore = newTotalScore;

        // 2. Sayarak Artma Animasyonunu Baþlat
        StopAllCoroutines(); // Eski animasyon varsa durdur karýþmasýn
        StartCoroutine(AnimateScore());

        // 3. Ortada Çýkan "+1000" Animasyonunu Baþlat
        StartCoroutine(ShowPopUp(addedAmount));
    }

    // --- ANIMASYON 1: SKOR SAYACI (0 -> 1000) ---
    IEnumerator AnimateScore()
    {
        float start = displayedScore;
        float end = currentScore;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * countSpeed;
            // Lerp: Ýki sayý arasýný yumuþakça doldurur
            displayedScore = (int)Mathf.Lerp(start, end, t);

            scoreText.text = displayedScore.ToString() + "$";
            yield return null;
        }

        // Garanti olsun diye döngü bitince net sayýyý yaz
        displayedScore = currentScore;
        scoreText.text = currentScore.ToString() + "$";
    }

    // --- ANIMASYON 2: POP-UP (+1000 yazýp uçma) ---
    IEnumerator ShowPopUp(int amount)
    {
        popUpText.gameObject.SetActive(true);
        popUpText.text = "+" + amount.ToString() + "$";

        // Baþlangýç pozisyonunu ve rengini sýfýrla
        popUpText.alpha = 1;
        Vector3 startPos = popUpText.transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 100; // 100 birim yukarý uçsun

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 2; // Hýzlýca uçsun

            // Yukarý kaydýr
            popUpText.transform.localPosition = Vector3.Lerp(startPos, endPos, t);

            // Yavaþça þeffaflaþ (Fade Out)
            popUpText.alpha = Mathf.Lerp(1, 0, t);

            yield return null;
        }

        // Ýþ bitince kapat ve yerine koy
        popUpText.gameObject.SetActive(false);
        popUpText.transform.localPosition = startPos;
    }
}