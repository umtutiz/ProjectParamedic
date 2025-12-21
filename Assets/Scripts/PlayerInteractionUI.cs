using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerInteractionUI : NetworkBehaviour
{
    [Header("AYARLAR")]
    public float reachDistance = 5.0f;
    public LayerMask interactableLayers;

    private Camera playerCam;
    private PlayerGrab playerGrab; // <-- Senin taþýma scriptin

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        playerCam = GetComponentInChildren<Camera>();

        // PlayerGrab scriptini otomatik buluyoruz
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

        // ELÝMÝZ DOLU MU?
        // (Adým 1'de public yaptýðýn deðiþkeni okuyoruz)
        bool isHandFull = (playerGrab != null && playerGrab.currentGrabbedObject != null);

        // ELÝMÝZDEKÝ ÞEY "HASTA" MI?
        bool holdingPatient = false;
        if (isHandFull)
        {
            // Elimizdeki objenin tag'i Patient mi diye bakýyoruz
            if (playerGrab.currentGrabbedObject.CompareTag("Patient"))
            {
                holdingPatient = true;
            }
        }

        if (Physics.Raycast(ray, out hit, reachDistance, interactableLayers))
        {
            Transform targetRoot = hit.transform.root;
            Transform hitObj = hit.transform;

            // 1. SEDYE (STRETCHER)
            if (targetRoot.CompareTag("Stretcher") || hitObj.CompareTag("Stretcher") || targetRoot.GetComponent<Stretcher>() != null)
            {
                if (holdingPatient)
                {
                    // Elimde hasta var + Sedyeye bakýyorum -> YERLEÞTÝR
                    txt.text = "[R] Place Patient";
                }
                else if (!isHandFull)
                {
                    // Elim boþ + Sedyeye bakýyorum -> SÜRÜKLE
                    txt.text = "[E] Drag Stretcher";
                }
                else
                {
                    // Elimde baþka bir þey var (Kutu vs), yazý çýkmasýn
                    txt.gameObject.SetActive(false);
                    return;
                }
                txt.gameObject.SetActive(true);
            }
            // 2. HASTA (PATIENT)
            else if (targetRoot.CompareTag("Patient") || hitObj.CompareTag("Patient"))
            {
                if (!isHandFull)
                {
                    // Elim boþ + Hastaya bakýyorum -> TAÞI
                    txt.text = "Press [E] to Carry Patient";
                    txt.gameObject.SetActive(true);
                }
                else
                {
                    // Elim zaten dolu, yazý çýkmasýn
                    txt.gameObject.SetActive(false);
                }
            }
            // 3. AMBULANS
            else if (targetRoot.CompareTag("Ambulance") || hitObj.CompareTag("Ambulance"))
            {
                txt.text = "Ambulans";
                txt.gameObject.SetActive(true);
            }
            // Hiçbiri deðilse
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