using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI; // Butonlara eriþmek için þart

public class NetworkMenu : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private GameObject menuPanel; // Butonlarý içeren panel (veya canvasýn kendisi)

    private void Start()
    {
        // Host butonuna basýlýnca ne olsun?
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            HideMenu(); // Menüyü gizle
        });

        // Client butonuna basýlýnca ne olsun?
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            HideMenu(); // Menüyü gizle
        });
    }

    private void HideMenu()
    {
        // Oyun baþlayýnca butonlarý yok et ki ekranda kalmasýnlar
        menuPanel.SetActive(false);
    }
}