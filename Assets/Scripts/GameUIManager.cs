using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    // Her yerden eriþilsin diye "static" yapýyoruz
    public static GameUIManager Instance;

    [Header("UI ELEMENTLERÝ")]
    public TextMeshProUGUI interactionText; // O lanet yazýyý buraya koyacaðýz

    private void Awake()
    {
        // Oyun açýlýnca "Ben buradayým" diyor
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Baþlangýçta o yazýyý kapatalým ki ekranda durmasýn
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }
}