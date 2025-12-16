using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Butonlara atayacaðýmýz fonksiyonlar

    public void StartHostGame()
    {
        // 1. Host'u baþlat
        NetworkManager.Singleton.StartHost();

        // 2. Oyun sahnesini yükle (Netcode yöntemiyle)
        // NOT: "GameScene" yerine senin oyun sahnenin adý neyse TAM OLARAK onu yaz!
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    public void StartClientGame()
    {
        // Client sadece baðlanýr, sahneyi Host otomatik yükletir
        NetworkManager.Singleton.StartClient();
    }
}