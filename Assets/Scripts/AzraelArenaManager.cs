using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AzraelArenaManager : NetworkBehaviour
{
    public static AzraelArenaManager Instance;

    [Header("ARENA IÞINLANMA NOKTALARI")]
    public Transform boxingSpawnPoint;  // Mod 1: Boks
    public Transform gunSpawnPoint;     // Mod 2: Silah
    public Transform shockSpawnPoint;   // Mod 3: Þok (Defibrilatör)
    public Transform runnerSpawnPoint;  // Mod 4: Tünel Koþusu
    public Transform morgueSpawnPoint;  // Mod 5: Morg
    public Transform bloodSpawnPoint;   // Mod 6: Kan Nakli

    [Header("GÖRSEL HEDEFLER & OBJELER")]
    public GameObject boxingAzrael;
    public GameObject gunAzrael;
    public GameObject shockMachine;     // Þok cihazý modeli
    public GameObject runnerAzrael;     // Tüneldeki Azrail
    public GameObject morgueAzrael;     // Morgdaki Azrail
    public GameObject bloodBag;         // Kan torbasý modeli

    [Header("GENEL UI")]
    public GameObject fightPanel;
    public TextMeshProUGUI modeText;    // "ÞOKU BAS!", "KAÇ!" vs.
    public TextMeshProUGUI infoText;    // "SPACE'E BAS", "W'YE BAS" vs.

    [Header("ÖZEL UI ELEMANLARI")]
    public Slider commonSlider;         // Þok ve Can barý için ortak slider
    public GameObject fakeGunModel;     // Silah Modu için

    // --- NETWORK DEÐÝÞKENLERÝ ---
    private NetworkVariable<int> fightMode = new NetworkVariable<int>(0);
    private NetworkVariable<ulong> fightingPlayerId = new NetworkVariable<ulong>(99999);

    // Oyun Ýçi Deðiþkenler
    private PatientHealth currentPatient;
    private Vector3 playerOriginalPos;

    // MOD ÖZEL DEÐÝÞKENLERÝ
    private float shockValue = 0f;      // Mod 3 için ibre deðeri
    private int shockSuccessCount = 0;  // Mod 3 kaç kere baþardýk?

    private KeyCode currentQTEKey;      // Mod 6 için basýlmasý gereken tuþ
    private float qteTimer = 0f;        // Mod 6 zaman sayacý
    private int bloodSuccessCount = 0;

    private NetworkVariable<bool> isAzraelLooking = new NetworkVariable<bool>(false); // Mod 5 (Morg) için

    private void Awake() { if (Instance == null) Instance = this; }

    public override void OnNetworkSpawn()
    {
        fightPanel.SetActive(false);
        CloseAllProps();
    }

    void Update()
    {
        // Sadece seçilen oyuncuysan ve bir mod aktifse çalýþ
        if (fightingPlayerId.Value != NetworkManager.Singleton.LocalClientId || fightMode.Value == 0) return;

        fightPanel.SetActive(true);

        switch (fightMode.Value)
        {
            case 1: UpdateBoxingMode(); break;
            case 2: UpdateGunMode(); break;
            case 3: UpdateShockMode(); break;
            case 4: UpdateRunnerMode(); break;
            case 5: UpdateMorgueMode(); break;
            case 6: UpdateBloodMode(); break;
        }
    }

    // --- TETÝKLEME (PATIENT SCRIPTINDEN GELÝR) ---
    public void StartAzraelEvent(PatientHealth patient)
    {
        if (!IsServer) return;

        currentPatient = patient;

        // Rastgele Oyuncu Seç
        var clientIds = NetworkManager.Singleton.ConnectedClientsIds;
        ulong chosenId = clientIds[Random.Range(0, clientIds.Count)];
        fightingPlayerId.Value = chosenId;

        // Rastgele Mod Seç (1 ile 6 arasý)
        int selectedMode = Random.Range(1, 7);
        fightMode.Value = selectedMode;

        // Modlara göre hazýrlýk (Server tarafý)
        if (selectedMode == 3) shockSuccessCount = 0;
        if (selectedMode == 6) { bloodSuccessCount = 0; PickNewQTEKey(); }
        if (selectedMode == 5) StartCoroutine(MorgueAzraelRoutine()); // Azrail döngüsünü baþlat

        TeleportPlayerClientRpc(chosenId, selectedMode);
    }

    // --- MOD 1: BOKS ---
    void UpdateBoxingMode()
    {
        commonSlider.gameObject.SetActive(true); // Can barý olarak kullan
        if (Input.GetMouseButtonDown(0)) AttackServerRpc(10);
    }

    // --- MOD 2: SÝLAH ---
    void UpdateGunMode()
    {
        fakeGunModel.SetActive(true);
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.gameObject == gunAzrael) AttackServerRpc(25);
            }
        }
    }

    // --- MOD 3: ÞOK CÝHAZI (DEFIBRILATOR) ---
    void UpdateShockMode()
    {
        commonSlider.gameObject.SetActive(true);
        // Ýbre sürekli 0 ile 1 arasý gidip gelir (PingPong)
        shockValue = Mathf.PingPong(Time.time * 2.5f, 1f);
        commonSlider.value = shockValue;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Yeþil Alan: 0.4 ile 0.6 arasý
            if (shockValue > 0.4f && shockValue < 0.6f)
            {
                SubmitShockServerRpc(true);
            }
            else
            {
                SubmitShockServerRpc(false); // Yanlýþ bastýn
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitShockServerRpc(bool success)
    {
        if (success)
        {
            shockSuccessCount++;
            if (shockSuccessCount >= 4) EndFight(true); // 4 kere yapan kazanýr
        }
        else
        {
            // Yanlýþ basarsan ceza (veya direkt kayýp)
            EndFight(false);
        }
    }

    // --- MOD 4: TÜNEL KOÞUSU (RUNNER) ---
    void UpdateRunnerMode()
    {
        // Karakteri otomatik ileri koþtur
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        player.transform.Translate(Vector3.forward * 6f * Time.deltaTime);

        // A ve D ile saða sola kaçýþ (Basitçe)
        float moveX = Input.GetAxis("Horizontal") * 5f * Time.deltaTime;
        player.transform.Translate(Vector3.right * moveX);

        // Engel çarpýþmasýný Player üzerindeki Collider halleder veya buraya Raycast koyabilirsin.
        // Bitiþ çizgisine varýrsa kazanýr (Trigger ile kontrol edilir).
    }

    // --- MOD 5: MORG (RED LIGHT / GREEN LIGHT) ---
    void UpdateMorgueMode()
    {
        infoText.text = isAzraelLooking.Value ? "DON! KIPIRDAMA!" : "YÜRÜ (W)";
        infoText.color = isAzraelLooking.Value ? Color.red : Color.green;

        if (isAzraelLooking.Value)
        {
            // Eðer Azrail bakarken hareket ediyorsan (W,A,S,D basýlýysa)
            if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
            {
                FailMorgueServerRpc();
            }
        }

        // Hedefe ulaþýrsan kazanmayý Trigger ile kontrol et (MorgExit scripti yapýp EndFight çaðýrabilirsin)
    }

    [ServerRpc(RequireOwnership = false)]
    void FailMorgueServerRpc()
    {
        EndFight(false);
    }

    // Server tarafýnda Azrail'i döndürüp duran Coroutine
    IEnumerator MorgueAzraelRoutine()
    {
        while (fightMode.Value == 5)
        {
            yield return new WaitForSeconds(Random.Range(2f, 4f)); // 2-4 sn arkasý dönük
            isAzraelLooking.Value = true; // DÖNDÜ!
            yield return new WaitForSeconds(Random.Range(1f, 2f)); // 1-2 sn bakýyor
            isAzraelLooking.Value = false; // ARKASINI DÖNDÜ
        }
    }

    // --- MOD 6: KAN NAKLÝ (QTE / BUTTON MASHING) ---
    void UpdateBloodMode()
    {
        infoText.text = "BAS: " + currentQTEKey.ToString();
        qteTimer -= Time.deltaTime;

        if (qteTimer <= 0)
        {
            SubmitBloodServerRpc(false); // Süre bitti, kaybettin
            return;
        }

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(currentQTEKey))
            {
                SubmitBloodServerRpc(true); // Doðru tuþ
            }
            else
            {
                SubmitBloodServerRpc(false); // Yanlýþ tuþ
            }
        }
    }

    void PickNewQTEKey()
    {
        // Rastgele tuþ seç (W, A, S, D, Space)
        KeyCode[] keys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Space };
        KeyCode newKey = keys[Random.Range(0, keys.Length)];

        // Client'a bildir
        SyncQTEClientRpc(newKey);
    }

    [ClientRpc]
    void SyncQTEClientRpc(KeyCode key)
    {
        currentQTEKey = key;
        qteTimer = 2.0f; // Her tuþ için 2 saniyen var
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitBloodServerRpc(bool success)
    {
        if (success)
        {
            bloodSuccessCount++;
            if (bloodSuccessCount >= 6) EndFight(true); // 6 tuþ bilen kazanýr
            else PickNewQTEKey(); // Sýradaki tuþa geç
        }
        else
        {
            EndFight(false);
        }
    }

    // --- ORTAK SALDIRI VE BÝTÝÞ ---
    [ServerRpc(RequireOwnership = false)]
    void AttackServerRpc(float damage)
    {
        // Boks ve Silah modu için basit can düþme
        // (Burada Azrail caný deðiþkeni eklenebilir ama basit tuttum)
        // Þimdilik 5 vuruþta ölsün mantýðý:
        if (Random.Range(0, 100) > 80) EndFight(true); // %20 þansla kritik atýp bitirme (Örnek)
    }

    public void WinByReachGoal() // Runner ve Morg için bitiþ çizgisi çaðýrýr
    {
        if (IsServer) EndFight(true);
    }

    void EndFight(bool playerWon)
    {
        if (!IsServer) return;

        fightMode.Value = 0;
        StopAllCoroutines();
        ReturnPlayerClientRpc(fightingPlayerId.Value, playerWon);

        if (playerWon)
        {
            Debug.Log("HASTA KURTARILDI!");
            if (currentPatient != null) currentPatient.Heal(40f);
        }
        else
        {
            Debug.Log("HASTA ÖLDÜ!");
            if (currentPatient != null) currentPatient.KillPatient();
        }
    }

    [ClientRpc]
    void TeleportPlayerClientRpc(ulong targetId, int mode)
    {
        CloseAllProps(); // Önce her þeyi gizle

        if (NetworkManager.Singleton.LocalClientId != targetId) return;

        // Pozisyon Kaydet
        var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        playerOriginalPos = playerObj.transform.position;
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        // Iþýnla ve Mod Yazýsýný Ayarla
        Transform targetPos = null;
        string msg = "";

        switch (mode)
        {
            case 1: targetPos = boxingSpawnPoint; boxingAzrael.SetActive(true); msg = "BOX MATCH"; break;
            case 2: targetPos = gunSpawnPoint; gunAzrael.SetActive(true); msg = "FIGHT!"; break;
            case 3: targetPos = shockSpawnPoint; shockMachine.SetActive(true); msg = "SHOCK DUEL"; break;
            case 4: targetPos = runnerSpawnPoint; runnerAzrael.SetActive(true); msg = "DEATH TUNNEL!"; break;
            case 5: targetPos = morgueSpawnPoint; morgueAzrael.SetActive(true); msg = "WELCOME TO THE MORGUE. BE QUIET."; break;
            case 6: targetPos = bloodSpawnPoint; bloodBag.SetActive(true); msg = "BLOOD TRANSFER!"; break;
        }

        if (targetPos != null)
        {
            playerObj.transform.position = targetPos.position;
            playerObj.transform.rotation = targetPos.rotation;
        }

        modeText.text = msg;
        if (cc) cc.enabled = true;
    }

    [ClientRpc]
    void ReturnPlayerClientRpc(ulong targetId, bool won)
    {
        CloseAllProps();
        fightPanel.SetActive(false);

        if (NetworkManager.Singleton.LocalClientId != targetId) return;

        var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        playerObj.transform.position = playerOriginalPos;
        if (cc) cc.enabled = true;
    }

    void CloseAllProps()
    {
        if (boxingAzrael) boxingAzrael.SetActive(false);
        if (gunAzrael) gunAzrael.SetActive(false);
        if (shockMachine) shockMachine.SetActive(false);
        if (runnerAzrael) runnerAzrael.SetActive(false);
        if (morgueAzrael) morgueAzrael.SetActive(false);
        if (bloodBag) bloodBag.SetActive(false);
        if (fakeGunModel) fakeGunModel.SetActive(false);
        if (commonSlider) commonSlider.gameObject.SetActive(false);
    }
}