using Unity.Netcode;
using UnityEngine;

public class CarInteraction : NetworkBehaviour
{
    public ArcadeCarController carController;
    public Transform driverViewPoint;
    public Transform exitPoint;
    public float interactionDist = 6f;

    [Header("Araba Ýçi Kamera")]
    public float mouseSensitivity = 2f;
    private float camXRotation = 0f;
    private float camYRotation = 0f;

    private GameObject localPlayer;

    void Update()
    {
        if (localPlayer == null)
        {
            if (NetworkManager.Singleton.LocalClient != null &&
                NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
            }
            return;
        }

        // --- 1. ARABADAYSAK VE BÝZ SÜRÜYORSAK: ETRAFA BAK ---
        if (carController.isDriven.Value && IsOwner && !localPlayer.activeSelf)
        {
            HandleCarCameraLook();
        }

        // --- 2. BÝNME / ÝNME ÝÞLEMLERÝ ---
        if (Input.GetKeyDown(KeyCode.E))
        {
            float dist = Vector3.Distance(transform.position, localPlayer.transform.position);

            // BÝNME
            if (dist < interactionDist && !carController.isDriven.Value)
            {
                RequestEnterCarServerRpc(NetworkManager.Singleton.LocalClientId);
            }
            // ÝNME
            else if (carController.isDriven.Value && IsOwner)
            {
                RequestExitCarServerRpc();
            }
        }
    }

    void HandleCarCameraLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Yukarý/Aþaðý bakýþ (Sýnýrlý)
        camXRotation -= mouseY;
        camXRotation = Mathf.Clamp(camXRotation, -30f, 30f); // Çok tepeye bakamasýn

        // Saða/Sola bakýþ (Sýnýrlý - Boyun kýrmasýn)
        camYRotation += mouseX;
        camYRotation = Mathf.Clamp(camYRotation, -80f, 80f); // Aynalara bakabilsin

        // Kamerayý döndür (Local rotation çünkü arabanýn içindeyiz)
        Camera.main.transform.localRotation = Quaternion.Euler(camXRotation, camYRotation, 0f);
    }

    // --- SERVER ---
    [ServerRpc(RequireOwnership = false)]
    void RequestEnterCarServerRpc(ulong clientId)
    {
        GetComponent<NetworkObject>().ChangeOwnership(clientId);
        carController.isDriven.Value = true;
        EnterCarClientRpc(clientId);
    }

    [ServerRpc]
    void RequestExitCarServerRpc()
    {
        carController.isDriven.Value = false;
        GetComponent<NetworkObject>().RemoveOwnership();
        ExitCarClientRpc(OwnerClientId);
    }

    // --- CLIENT ---
    [ClientRpc]
    void EnterCarClientRpc(ulong driverId)
    {
        if (NetworkManager.Singleton.LocalClientId == driverId)
        {
            Camera.main.transform.SetParent(null);
            Camera.main.transform.position = driverViewPoint.position;
            Camera.main.transform.rotation = driverViewPoint.rotation;
            Camera.main.transform.SetParent(driverViewPoint);

            // Kafayý sýfýrla (Tam karþýya bakarak baþla)
            camXRotation = 0;
            camYRotation = 0;

            localPlayer.SetActive(false);
        }
    }

    [ClientRpc]
    void ExitCarClientRpc(ulong driverId)
    {
        if (NetworkManager.Singleton.LocalClientId == driverId)
        {
            Camera.main.transform.SetParent(null);
            localPlayer.transform.position = exitPoint.position;
            localPlayer.SetActive(true);

            Transform cameraRoot = localPlayer.transform.Find("CameraRoot");
            if (cameraRoot != null)
            {
                Camera.main.transform.position = cameraRoot.position;
                Camera.main.transform.rotation = cameraRoot.rotation;
                Camera.main.transform.SetParent(cameraRoot);
            }
        }
    }
}