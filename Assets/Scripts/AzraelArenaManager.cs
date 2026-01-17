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
    public Transform boxingSpawnPoint;
    public Transform gunSpawnPoint;
    public Transform shockSpawnPoint;
    public Transform runnerSpawnPoint;
    public Transform morgueSpawnPoint;
    public Transform bloodSpawnPoint;

    [Header("GÖRSEL HEDEFLER & OBJELER")]
    public GameObject boxingAzrael;
    public GameObject gunAzrael;
    public GameObject shockMachine;
    public GameObject runnerAzrael;
    public GameObject morgueAzrael;
    public GameObject bloodBag;

    [Header("GENEL UI")]
    public GameObject fightPanel;
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI infoText;

    [Header("ÖZEL UI ELEMANLARI")]
    public Slider commonSlider;
    public GameObject fakeGunModel;

    // --- NETWORK DEÐÝÞKENLERÝ ---
    private NetworkVariable<int> fightMode = new NetworkVariable<int>(0);
    private NetworkVariable<ulong> fightingPlayerId = new NetworkVariable<ulong>(99999);

    // OYUN ÝÇÝ DEÐÝÞKENLER
    private PatientHealth currentPatient;
    private Vector3 playerOriginalPos;

    // MOD ÖZEL DEÐÝÞKENLERÝ
    private float shockValue = 0f;
    private int shockSuccessCount = 0;

    private KeyCode currentQTEKey;
    private float qteTimer = 0f;
    private int bloodSuccessCount = 0;

    private NetworkVariable<bool> isAzraelLooking = new NetworkVariable<bool>(false);

    // --- YENÝ EKLENEN MARKET ÖZELLÝKLERÝ ---
    private bool bribeActive = false;      // Rüþvet verildi mi?
    private bool batteryUpgraded = false;  // Pil takýldý mý?
    // ---------------------------------------

    private void Awake() { if (Instance == null) Instance = this; }

    // ... Awake fonksiyonundan hemen sonra ...

    void Start()
    {
        // 1. UI PANELÝNÝ ZORLA KAPAT
        if (fightPanel != null) fightPanel.SetActive(false);

        // 2. SÝLAHI ZORLA KAPAT
        if (fakeGunModel != null) fakeGunModel.SetActive(false);

        // 3. SAHNEDEKÝ TÜM AZRAÝLLERÝ KAPAT
        CloseAllProps();
    }

    // ... OnNetworkSpawn diye devam ediyor ...

    public override void OnNetworkSpawn()
    {
        fightPanel.SetActive(false);
        CloseAllProps();
    }

    void Update()
    {
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

    // --- MARKET SÝSTEMÝNDEN ÇAÐRILANLAR (YENÝ) ---

    public void EnableBribeMode()
    {
        bribeActive = true;
        Debug.Log("RÜÞVET VERÝLDÝ! Azrail bir sonraki sefer gelmeyecek.");
    }

    public void UpgradeBattery()
    {
        batteryUpgraded = true;
        Debug.Log("PÝL TAKILDI! Þok cihazý artýk daha kolay.");
    }
    // ----------------------------------------------

    // --- TETÝKLEME ---
    public void StartAzraelEvent(PatientHealth patient)
    {
        if (!IsServer) return;

        // 1. RÜÞVET KONTROLÜ (YENÝ)
        if (bribeActive)
        {
            bribeActive = false; // Hakký kullandýk, sýfýrla
            Debug.Log("AZRAÝL RÜÞVETÝ ALDI VE GÝTTÝ. HASTA KURTULDU.");
            // Hastayý biraz iyileþtir ki hemen tekrar ölmesin
            patient.Heal(20f);
            return; // Savaþ baþlatmadan çýk
        }

        currentPatient = patient;

        // Rastgele Oyuncu Seç
        var clientIds = NetworkManager.Singleton.ConnectedClientsIds;
        ulong chosenId = clientIds[Random.Range(0, clientIds.Count)];
        fightingPlayerId.Value = chosenId;

        // Rastgele Mod Seç (1-6)
        int selectedMode = Random.Range(1, 7);
        fightMode.Value = selectedMode;

        // Hazýrlýklar
        if (selectedMode == 3) shockSuccessCount = 0;
        if (selectedMode == 6) { bloodSuccessCount = 0; PickNewQTEKey(); }
        if (selectedMode == 5) StartCoroutine(MorgueAzraelRoutine());

        TeleportPlayerClientRpc(chosenId, selectedMode);
    }

    // --- MOD 1: BOKS ---
    void UpdateBoxingMode()
    {
        commonSlider.gameObject.SetActive(true);
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

    // --- MOD 3: ÞOK CÝHAZI (PÝL EKLENDÝ) ---
    void UpdateShockMode()
    {
        commonSlider.gameObject.SetActive(true);
        shockValue = Mathf.PingPong(Time.time * 2.5f, 1f);
        commonSlider.value = shockValue;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // PÝL KONTROLÜ (YENÝ)
            // Normalde hata payý 0.1 (0.4 - 0.6 arasý)
            // Pil varsa hata payý 0.2 (0.3 - 0.7 arasý) -> Çok daha kolay olur
            float margin = batteryUpgraded ? 0.2f : 0.1f;

            if (shockValue > (0.5f - margin) && shockValue < (0.5f + margin))
            {
                SubmitShockServerRpc(true);
            }
            else
            {
                SubmitShockServerRpc(false);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitShockServerRpc(bool success)
    {
        if (success)
        {
            shockSuccessCount++;
            if (shockSuccessCount >= 4) EndFight(true);
        }
        else
        {
            EndFight(false);
        }
    }

    // --- MOD 4: RUNNER ---
    void UpdateRunnerMode()
    {
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        player.transform.Translate(Vector3.forward * 6f * Time.deltaTime);
        float moveX = Input.GetAxis("Horizontal") * 5f * Time.deltaTime;
        player.transform.Translate(Vector3.right * moveX);
    }

    // --- MOD 5: MORG ---
    void UpdateMorgueMode()
    {
        infoText.text = isAzraelLooking.Value ? "DON! KIPIRDAMA!" : "YÜRÜ (W)";
        infoText.color = isAzraelLooking.Value ? Color.red : Color.green;

        if (isAzraelLooking.Value)
        {
            if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
            {
                FailMorgueServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void FailMorgueServerRpc()
    {
        EndFight(false);
    }

    IEnumerator MorgueAzraelRoutine()
    {
        while (fightMode.Value == 5)
        {
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            isAzraelLooking.Value = true;
            yield return new WaitForSeconds(Random.Range(1f, 2f));
            isAzraelLooking.Value = false;
        }
    }

    // --- MOD 6: KAN NAKLÝ ---
    void UpdateBloodMode()
    {
        infoText.text = "BAS: " + currentQTEKey.ToString();
        qteTimer -= Time.deltaTime;

        if (qteTimer <= 0)
        {
            SubmitBloodServerRpc(false);
            return;
        }

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(currentQTEKey)) SubmitBloodServerRpc(true);
            else SubmitBloodServerRpc(false);
        }
    }

    void PickNewQTEKey()
    {
        KeyCode[] keys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Space };
        KeyCode newKey = keys[Random.Range(0, keys.Length)];
        SyncQTEClientRpc(newKey);
    }

    [ClientRpc]
    void SyncQTEClientRpc(KeyCode key)
    {
        currentQTEKey = key;
        qteTimer = 2.0f;
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitBloodServerRpc(bool success)
    {
        if (success)
        {
            bloodSuccessCount++;
            if (bloodSuccessCount >= 6) EndFight(true);
            else PickNewQTEKey();
        }
        else
        {
            EndFight(false);
        }
    }

    // --- ORTAK ---
    [ServerRpc(RequireOwnership = false)]
    void AttackServerRpc(float damage)
    {
        if (Random.Range(0, 100) > 80) EndFight(true);
    }

    public void WinByReachGoal()
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
        CloseAllProps();

        if (NetworkManager.Singleton.LocalClientId != targetId) return;

        var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        playerOriginalPos = playerObj.transform.position;
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        Transform targetPos = null;
        string msg = "";

        switch (mode)
        {
            case 1: targetPos = boxingSpawnPoint; boxingAzrael.SetActive(true); msg = "BOKS MAÇI!"; break;
            case 2: targetPos = gunSpawnPoint; gunAzrael.SetActive(true); msg = "ÇATIÞMA!"; break;
            case 3: targetPos = shockSpawnPoint; shockMachine.SetActive(true); msg = "ÞOK DÜELLOSU!"; break;
            case 4: targetPos = runnerSpawnPoint; runnerAzrael.SetActive(true); msg = "ÖLÜM TÜNELÝ!"; break;
            case 5: targetPos = morgueSpawnPoint; morgueAzrael.SetActive(true); msg = "MORGA HOÞ GELDÝN."; break;
            case 6: targetPos = bloodSpawnPoint; bloodBag.SetActive(true); msg = "KAN NAKLÝ!"; break;
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