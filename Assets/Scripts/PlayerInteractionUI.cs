using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerInteractionUI : NetworkBehaviour
{
    [Header("AYARLAR")]
    public float reachDistance = 5.0f;
    public LayerMask interactableLayers;

    [Header("KAMERA AYARI")]
    // BURASI ÖNEMLÝ: Inspector'dan karakterin içindeki kamerayý buraya sürükle!
    public Camera playerCamera;

    private PlayerGrab playerGrab;

    public override void OnNetworkSpawn()
    {
        // Karakter bize aitse çalýþtýr, deðilse kapat
        if (IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true); // Kamerayý aç
                // Sesleri duymak için AudioListener ekle (yoksa ekle)
                if (playerCamera.GetComponent<AudioListener>() == null)
                    playerCamera.gameObject.AddComponent<AudioListener>();
            }
            playerGrab = GetComponent<PlayerGrab>();
        }
        else
        {
            // Baþkasýnýn karakteriyse kamerasýný kapat ki ekran karýþmasýn
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            enabled = false; // Update fonksiyonunu durdur
        }
    }

    void Update()
    {
        // Karakter benim deðilse veya UI Manager hazýr deðilse çalýþma
        if (!IsOwner) return;
        if (GameUIManager.Instance == null) return;

        // Kamera atanmamýþsa hata vermesin diye durdur
        if (playerCamera == null) return;

        CheckLookingObject();
    }

    void CheckLookingObject()
    {

        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * reachDistance, Color.red);
        // Senin yazdýðýn mantýðýn aynýsý, sadece playerCamera deðiþkenini kullanýyor
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        TextMeshProUGUI txt = GameUIManager.Instance.interactionText;

        // 1. ELÝMÝZ DOLU MU KONTROLÜ
        bool isHandFull = (playerGrab != null && playerGrab.currentGrabbedObject != null);

        // 2. ELÝMÝZDEKÝ ÞEY "HASTA" MI KONTROLÜ
        bool holdingPatient = false;

        if (isHandFull)
        {
            // Hem objenin kendisine, hem de en tepedeki babasýna (Root) bakýyoruz.
            if (playerGrab.currentGrabbedObject.CompareTag("Patient") ||
                playerGrab.currentGrabbedObject.transform.root.CompareTag("Patient"))
            {
                holdingPatient = true;
            }
        }

        if (Physics.Raycast(ray, out hit, reachDistance, interactableLayers))
        {
            Transform targetRoot = hit.transform.root;
            Transform hitObj = hit.transform;

            // --- SEDYEYE BAKIYORSAK ---
            if (targetRoot.CompareTag("Stretcher") || hitObj.CompareTag("Stretcher") || targetRoot.GetComponent<Stretcher>() != null)
            {
                if (holdingPatient)
                {
                    txt.text = "[R] Place Patient"; // Hasta var -> Koy
                    txt.color = Color.green;
                }
                else if (!isHandFull)
                {
                    txt.text = "[E] Drag Stretcher"; // El boþ -> Sürükle
                    txt.color = Color.white;
                }
                else
                {
                    // Elim dolu ama HASTA DEÐÝL, o zaman yazý çýkmasýn
                    txt.gameObject.SetActive(false);
                    return;
                }
                txt.gameObject.SetActive(true);
            }
            // --- HASTAYA BAKIYORSAK ---
            else if (targetRoot.CompareTag("Patient") || hitObj.CompareTag("Patient"))
            {
                if (!isHandFull)
                {
                    txt.text = "Press [E] to Carry Patient";
                    txt.color = Color.white;
                    txt.gameObject.SetActive(true);
                }
                else
                {
                    txt.gameObject.SetActive(false);
                }
            }
            // --- AMBULANSA BAKIYORSAK ---
            else if (targetRoot.CompareTag("Ambulance") || hitObj.CompareTag("Ambulance"))
            {
                txt.text = "Ambulans";
                txt.gameObject.SetActive(true);
            }
            else
            {
                if (txt.gameObject.activeSelf) txt.gameObject.SetActive(false);
            }
        }
        else
        {
            if (txt.gameObject.activeSelf) txt.gameObject.SetActive(false);
        }
    }
}