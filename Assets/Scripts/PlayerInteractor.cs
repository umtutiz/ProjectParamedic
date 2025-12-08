using Unity.Netcode;
using UnityEngine;

public class PlayerInteractor : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private LayerMask interactLayer;

    // Burasý boþ kalýrsa kod otomatik bulmaya çalýþacak
    [SerializeField] private Transform handHoldPoint;

    public override void OnNetworkSpawn()
    {
        // DÜZELTME: "IsOwner" þartýný kaldýrdýk. 
        // Artýk Server da dahil herkes elin nerede olduðunu bilecek.
        if (handHoldPoint == null)
        {
            // Eðer inspector'dan atamazsan isminden bulmaya çalýþýr
            // NOT: Hiyerarþide Player -> Main Camera -> HandHoldPoint sýrasýnda olmalý
            handHoldPoint = transform.Find("Main Camera/HandHoldPoint");

            // Eðer hala bulamadýysa (isim farklýysa vs.) hata vermesin diye uyarý atalým
            if (handHoldPoint == null)
            {
                Debug.LogError("HATA: 'HandHoldPoint' bulunamadý! Lütfen Player Prefab'ýnda PlayerInteractor scriptine elle sürükle.");
            }
        }
    }

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
        Ray ray = new Ray(cameraRoot.position, cameraRoot.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                // Sunucuya 'Ben buna týkladým' diyoruz
                InteractServerRpc(hit.collider.GetComponent<NetworkObject>().NetworkObjectId);
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
        // HATA BURADAYDI: handHoldPoint null olduðu için patlýyordu.
        if (handHoldPoint == null) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            // Eðer elimiz boþsa al
            if (handHoldPoint.childCount == 0)
            {
                var interactable = netObj.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(OwnerClientId);
                }
            }
        }
    }

    [ServerRpc]
    private void DropServerRpc()
    {
        if (handHoldPoint == null) return;

        // Elimizde bir þey var mý?
        if (handHoldPoint.childCount > 0)
        {
            Transform heldObject = handHoldPoint.GetChild(0);
            NetworkPickable pickable = heldObject.GetComponent<NetworkPickable>();

            if (pickable != null)
            {
                pickable.DropItem();
            }
        }
    }
}