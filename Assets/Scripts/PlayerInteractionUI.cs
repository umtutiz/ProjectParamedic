using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerInteractionUI : NetworkBehaviour
{
    [Header("AYARLAR")]
    public float reachDistance = 5.0f;
    public LayerMask interactableLayers;

    private Camera playerCam;
    private PlayerGrab playerGrab;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        playerCam = GetComponentInChildren<Camera>();
        playerGrab = GetComponent<PlayerGrab>();
    }

    void Update()
    {
        if (!IsOwner) return;
        if (GameUIManager.Instance == null) return;

        CheckLookingObject();
    }

    void CheckLookingObject()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        TextMeshProUGUI txt = GameUIManager.Instance.interactionText;

        // 1. ELÝMÝZ DOLU MU KONTROLÜ
        bool isHandFull = (playerGrab != null && playerGrab.currentGrabbedObject != null);

        // 2. ELÝMÝZDEKÝ ÞEY "HASTA" MI KONTROLÜ
        bool holdingPatient = false;

        if (isHandFull)
        {
            // Debug: Elimizde ne olduðunu görelim
            // (Konsolda kýrmýzý hata çýkarsa PlayerGrab'de currentGrabbedObject PUBLIC yapýlmamýþ demektir)
            // Debug.Log("Elimdeki: " + playerGrab.currentGrabbedObject.name + " | Tag: " + playerGrab.currentGrabbedObject.tag);

            // Hem objenin kendisine, hem de en tepedeki babasýna (Root) bakýyoruz.
            // Bazen script içerideki kemikte olur, ama Tag en dýþtaki kutudadýr.
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
                    txt.color = Color.green; // Yazý yeþil olsun belli olsun
                }
                else if (!isHandFull)
                {
                    txt.text = "[E] Drag Stretcher"; // El boþ -> Sürükle
                    txt.color = Color.white;
                }
                else
                {
                    // Elim dolu ama HASTA DEÐÝL (Kutu vs.), o zaman yazý çýkmasýn
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