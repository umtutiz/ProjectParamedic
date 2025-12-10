using Unity.Netcode;
using UnityEngine;

public class PlayerInteractor : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private LayerMask interactLayer;

    // ÞU AN ELÝMDE NE VAR?
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
        if (currentHeldItem != null)
        {
            Debug.Log("Zaten elinde bir þey var.");
            return;
        }

        Ray ray = new Ray(cameraRoot.position, cameraRoot.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            // --- KRÝTÝK DEÐÝÞÝKLÝK BURADA ---
            // Sadece çarptýðým objeye deðil, onun BABASINA (Parent) da bak!
            // Çünkü scriptimiz kol/bacakta deðil, ana kutuda duruyor.
            if (hit.collider.GetComponentInParent<NetworkObject>() != null)
            {
                // ID'yi babadan al
                ulong objectId = hit.collider.GetComponentInParent<NetworkObject>().NetworkObjectId;
                InteractServerRpc(objectId);
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
        if (currentHeldItem != null) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            var interactable = netObj.GetComponent<IInteractable>();
            var pickable = netObj.GetComponent<NetworkPickable>();

            if (interactable != null)
            {
                interactable.Interact(OwnerClientId);

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
        if (currentHeldItem != null)
        {
            currentHeldItem.DropItem();
            currentHeldItem = null;
        }
    }
}