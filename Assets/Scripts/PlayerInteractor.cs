using Unity.Netcode;
using UnityEngine;

public class PlayerInteractor : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private LayerMask interactLayer;

    // ÞU AN ELÝMDE NE VAR? (Server tarafýnda tutulur)
    private NetworkPickable currentHeldItem;

    private void Update()
    {
        if (!IsOwner) return;

        // E TUÞU: YERDEN AL
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }

        // G TUÞU: ELÝNDEKÝNÝ AT
        if (Input.GetKeyDown(KeyCode.G))
        {
            TryDrop();
        }
    }

    private void TryInteract()
    {
        // Eðer zaten elim doluysa yeni bir þey alma!
        // (Server'a sormadan önce client tarafýnda basit kontrol)
        if (currentHeldItem != null)
        {
            Debug.Log("Zaten elinde bir þey var, önce onu býrak.");
            return;
        }

        Ray ray = new Ray(cameraRoot.position, cameraRoot.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent(out NetworkObject netObj))
            {
                InteractServerRpc(netObj.NetworkObjectId);
            }
        }
    }

    private void TryDrop()
    {
        DropServerRpc();
    }

    [ServerRpc]
    private void InteractServerRpc(ulong objectId)
    {
        if (currentHeldItem != null) return; // Zaten doluysak alma

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            var interactable = netObj.GetComponent<IInteractable>();
            var pickable = netObj.GetComponent<NetworkPickable>(); // BU SATIR ÖNEMLÝ

            if (interactable != null)
            {
                interactable.Interact(OwnerClientId);

                // EÞYAYI HAFIZAYA ALIYORUZ KÝ G TUÞU NEYÝ ATACAÐINI BÝLSÝN
                if (pickable != null)
                {
                    currentHeldItem = pickable;
                }
            }
        }
    }

    [ServerRpc]
    private void DropServerRpc()
    {
        // Hafýzada tuttuðumuz eþya var mý?
        if (currentHeldItem != null)
        {
            // Varsa býrakma fonksiyonunu çaðýr
            currentHeldItem.DropItem();

            // Hafýzayý temizle (Elimiz artýk boþ)
            currentHeldItem = null;
        }
    }
}