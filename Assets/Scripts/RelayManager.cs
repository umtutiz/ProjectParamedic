using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RelayManager : MonoBehaviour
{
    [Header("UI BAÐLANTILARI")]
    public TMP_InputField joinCodeInput; // Oyuncunun kod gireceði kutu
    public TextMeshProUGUI statusText;   // Durum yazýsý
    public GameObject buttonsPanel;      // Butonlarý gizlemek için

    private async void Start()
    {
        // BAÞLANGIÇ TEMÝZLÝÐÝ: Sahne açýlýnca o "New Text" yazýsýný yok ediyoruz.
        if (statusText != null) statusText.text = "";

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Giriþ Yapýldý ID: " + AuthenticationService.Instance.PlayerId);
        UpdateStatus("Hazýr. Oda Kur veya Katýl.");
    }

    // --- HOST (KURUCU) ---
    public async void CreateRelay()
    {
        try
        {
            UpdateStatus("Oda Oluþturuluyor...");
            buttonsPanel.SetActive(false);

            // 3 Kiþilik yer ayýr (Host dahil 4)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log("Oda Kodu: " + joinCode);
            UpdateStatus("ODA KODU: " + joinCode); // Ekrana kodu yaz

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();

            // Host sahneyi yükler
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            UpdateStatus("Hata: " + e.Message);
            buttonsPanel.SetActive(true); // Hata olursa butonlarý geri aç
        }
    }

    // --- CLIENT (KATILIMCI) ---
    public async void JoinRelay()
    {
        // BURASI ÖNEMLÝ: Oyuncu boþluk býrakýrsa sil (.Trim) ve küçük harf yazarsa büyüt (.ToUpper)
        string code = joinCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            UpdateStatus("Lütfen bir kod gir!");
            return;
        }

        try
        {
            UpdateStatus("Odaya Baðlanýlýyor...");
            buttonsPanel.SetActive(false);

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            UpdateStatus("Baðlandý! Host bekleniyor...");
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            UpdateStatus("Hata: Kod Yanlýþ veya Oda Dolu.");
            buttonsPanel.SetActive(true); // Hata olursa butonlarý geri aç
        }
    }

    void UpdateStatus(string msg)
    {
        // Eski yazýyý silip yenisini yazar (= operatörü sayesinde).
        if (statusText != null) statusText.text = msg;
    }
}