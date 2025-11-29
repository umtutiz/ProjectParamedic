using UnityEngine;
using Unity.Netcode;

public class AmbulanceController : NetworkBehaviour
{
    [Header("Referanslar")]
    public Transform driverSeat;
    public Transform exitPoint;

    [Header("Tekerlek Colliderlarý")]
    public WheelCollider onSolCol, onSagCol, arkaSolCol, arkaSagCol;

    [Header("Görsel Tekerlekler")]
    public Transform onSolMesh, onSagMesh, arkaSolMesh, arkaSagMesh;

    [Header("Ayarlar")]
    public float motorGucu = 1500f;
    public float donusGucu = 30f;
    public float frenGucu = 10000f; // Güçlü el freni

    private NetworkVariable<ulong> currentDriverId = new NetworkVariable<ulong>(ulong.MaxValue);
    private float gazInput, direksiyonInput, frenInput;
    private Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        if (IsServer) currentDriverId.Value = ulong.MaxValue;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Aðýrlýk merkezini aþaðý çek (Devrilmesin)
        rb.centerOfMass = new Vector3(0, -0.9f, 0);
    }

    void Update()
    {
        // Sadece kendi karakterim için çalýþ
        if (currentDriverId.Value != NetworkManager.Singleton.LocalClientId) return;

        AlGirdileri();

        if (Input.GetKeyDown(KeyCode.E))
        {
            RequestExitCarServerRpc();
        }
    }

    void FixedUpdate()
    {
        // --- BURASI DEÐÝÞTÝ: KÝLÝT SÝSTEMÝ ---

        // EÐER SÜRÜCÜ VARSA (Sürüþ Modu)
        if (currentDriverId.Value != ulong.MaxValue)
        {
            // Kilidi aç: Her yere gidebilir
            rb.constraints = RigidbodyConstraints.None;

            HareketEt();
            TekerlekleriDondur();
        }
        // EÐER SÜRÜCÜ YOKSA (Park Modu)
        else
        {
            // Sadece YER ÇEKÝMÝ (Y) çalýþsýn. Saða sola (X, Z) ve dönmeye (Rot) kilit vur.
            // Böylece sen itince kýpýrdamaz ama havada kalýrsa yere düþer.
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            // El frenini kökle
            onSolCol.brakeTorque = frenGucu;
            onSagCol.brakeTorque = frenGucu;
            arkaSolCol.brakeTorque = frenGucu;
            arkaSagCol.brakeTorque = frenGucu;
        }
    }

    // --- SERVER GÝRÝÞ/ÇIKIÞ ---

    [ServerRpc(RequireOwnership = false)]
    public void RequestEnterCarServerRpc(ulong playerId)
    {
        if (currentDriverId.Value != ulong.MaxValue) return;

        GetComponent<NetworkObject>().ChangeOwnership(playerId);
        currentDriverId.Value = playerId;
        EnterCarClientRpc(playerId);
    }

    [ServerRpc]
    public void RequestExitCarServerRpc()
    {
        ulong playerId = currentDriverId.Value;
        GetComponent<NetworkObject>().RemoveOwnership();
        currentDriverId.Value = ulong.MaxValue;
        ExitCarClientRpc(playerId);
    }

    // --- CLIENT GÖRSEL ---

    [ClientRpc]
    void EnterCarClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            // FPS Kontrolcüsünü kapat
            var fpsController = playerObj.GetComponent<FirstPersonController>();
            if (fpsController != null) fpsController.enabled = false;

            // Collider'ý kapat (Arabanýn içinde titremesin)
            var col = playerObj.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Rigidbody'yi kapat (Fizik çakýþmasý olmasýn)
            var rbPlayer = playerObj.GetComponent<Rigidbody>();
            if (rbPlayer != null) rbPlayer.isKinematic = true;

            playerObj.transform.position = driverSeat.position;
            playerObj.transform.rotation = driverSeat.rotation;
            playerObj.transform.parent = driverSeat;
        }
    }

    [ClientRpc]
    void ExitCarClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            playerObj.transform.parent = null;
            playerObj.transform.position = exitPoint.position;
            playerObj.transform.rotation = Quaternion.identity;

            // Her þeyi geri aç
            var fpsController = playerObj.GetComponent<FirstPersonController>();
            if (fpsController != null) fpsController.enabled = true;

            var col = playerObj.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            var rbPlayer = playerObj.GetComponent<Rigidbody>();
            if (rbPlayer != null) rbPlayer.isKinematic = false;
        }
    }

    // --- FÝZÝK ---
    void AlGirdileri()
    {
        gazInput = Input.GetAxis("Vertical");
        direksiyonInput = Input.GetAxis("Horizontal");
        frenInput = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    void HareketEt()
    {
        arkaSolCol.motorTorque = gazInput * motorGucu;
        arkaSagCol.motorTorque = gazInput * motorGucu;
        onSolCol.steerAngle = direksiyonInput * donusGucu;

        float anlikFren = frenInput * frenGucu;
        onSolCol.brakeTorque = anlikFren;
        onSagCol.brakeTorque = anlikFren;
        arkaSolCol.brakeTorque = anlikFren;
        arkaSagCol.brakeTorque = anlikFren;
    }

    void TekerlekleriDondur()
    {
        TekerlekPozisyonuGuncelle(onSolCol, onSolMesh);
        TekerlekPozisyonuGuncelle(onSagCol, onSagMesh);
        TekerlekPozisyonuGuncelle(arkaSolCol, arkaSolMesh);
        TekerlekPozisyonuGuncelle(arkaSagCol, arkaSagMesh);
    }

    void TekerlekPozisyonuGuncelle(WheelCollider col, Transform mesh)
    {
        Vector3 pos; Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        mesh.position = pos; mesh.rotation = rot;
    }
}