using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI; // NavMesh için şart!

public class AdvancedMissionManager : NetworkBehaviour
{
    public static AdvancedMissionManager Instance;

    [System.Serializable]
    public struct MissionType
    {
        public string title;          // Örn: "TRAFİK KAZASI"
        public GameObject scenePrefab;// Örn: Çarpışmış Arabalar Prefabı (Yoksa boş bırak)
        public int minPatients;       // Kaç yaralı?
        public int maxPatients;
        public bool isFakeCall;       // Asılsız ihbar mı?
    }

    [Header("AYARLAR")]
    public GameObject patientPrefab; // Hasta Adam
    public float missionCooldown = 15f;
    public float spawnRadius = 50f; // Merkezden ne kadar uzağa kadar görev çıksın?

    [Header("SENARYOLAR")]
    public List<MissionType> missions; // Inspector'dan dolduracağız

    [Header("UI")]
    public TextMeshProUGUI alertText;

    private List<GameObject> activeObjects = new List<GameObject>(); // Hem hastaları hem dekorları tutar
    private bool isCooldownActive = true;
    private float cooldownTimer = 5f;
    private bool currentMissionIsFake = false;
    private Vector3 currentMissionPos;

    // Oyuncuyu takip edip ona yakın görev vermek için
    private Transform playerTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (!IsServer) return;

        // Player'ı bul (Mesafe ölçmek için)
        if (playerTransform == null && NetworkManager.Singleton.ConnectedClients.Count > 0)
        {
            if (NetworkManager.Singleton.ConnectedClients[0].PlayerObject != null)
                playerTransform = NetworkManager.Singleton.ConnectedClients[0].PlayerObject.transform;
        }

        // 1. BEKLEME SÜRESİ
        if (isCooldownActive)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                StartRandomMission();
            }
        }
        // 2. GÖREV TAKİBİ
        else
        {
            // Eğer asılsız ihbarsa (Fake Call)
            if (currentMissionIsFake)
            {
                // Oyuncu olay yerine yaklaştı mı? (10 metre)
                if (playerTransform != null && Vector3.Distance(playerTransform.position, currentMissionPos) < 10f)
                {
                    EndMission("İHBAR ASILSIZ ÇIKTI! MERKEZE DÖNÜN.");
                }
            }
            else
            {
                // Gerçek görevse: Hastalar bitti mi?
                activeObjects.RemoveAll(item => item == null);

                // Sadece hastalar temizlendi mi diye bakmalıyız (Dekorlar sayılmaz)
                // Basit çözüm: Eğer sahnede "Patient" tagli obje kalmadıysa bitir.
                // (Ama haritada başka görev yoksa bu çalışır)
                bool anyPatientLeft = false;
                foreach (var obj in activeObjects)
                {
                    if (obj.CompareTag("Patient")) anyPatientLeft = true;
                }

                if (!anyPatientLeft)
                {
                    EndMission("GÖREV BAŞARILI! BÖLGE TEMİZ.");
                }
            }
        }
    }

    void StartRandomMission()
    {
        // 1. Rastgele bir görev tipi seç
        MissionType selectedMission = missions[Random.Range(0, missions.Count)];

        // 2. NavMesh üzerinde rastgele bir nokta bul
        Vector3 randomPoint = GetRandomNavMeshPoint();
        if (randomPoint == Vector3.zero) return; // Yer bulamazsa iptal

        currentMissionPos = randomPoint;
        currentMissionIsFake = selectedMission.isFakeCall;
        isCooldownActive = false;

        // 3. Dekor (Scene) Kurulumu (Kaza arabaları vs.)
        if (selectedMission.scenePrefab != null)
        {
            GameObject sceneProp = Instantiate(selectedMission.scenePrefab, randomPoint, Quaternion.identity);
            sceneProp.GetComponent<NetworkObject>().Spawn();
            activeObjects.Add(sceneProp);
        }

        // 4. Hasta Spawn Et (Eğer fake call değilse)
        if (!selectedMission.isFakeCall)
        {
            int count = Random.Range(selectedMission.minPatients, selectedMission.maxPatients + 1);
            for (int i = 0; i < count; i++)
            {
                // Hastaları tam merkeze değil, biraz etrafa saçalım
                Vector3 offset = Random.insideUnitSphere * 3f;
                offset.y = 0; // Havada doğmasınlar
                Vector3 spawnPos = randomPoint + offset;

                GameObject patient = Instantiate(patientPrefab, spawnPos, Quaternion.identity);
                patient.GetComponent<NetworkObject>().Spawn();
                activeObjects.Add(patient);
            }
        }

        // 5. Bildirim Gönder
        string locationName = $"{Mathf.Round(randomPoint.x)}, {Mathf.Round(randomPoint.z)} KOORDİNATLARI";
        NewMissionClientRpc(selectedMission.title, locationName, selectedMission.isFakeCall);
    }

    // NavMesh üzerinde gidilebilecek rastgele nokta bulucu
    Vector3 GetRandomNavMeshPoint()
    {
        // Haritanın merkezinden (0,0,0) itibaren 'spawnRadius' kadar alanda arar
        Vector3 randomDir = Random.insideUnitSphere * spawnRadius;
        randomDir += Vector3.zero; // Veya playerTransform.position diyip oyuncuya yakın verebilirsin

        NavMeshHit hit;
        // NavMesh üzerinde en yakın geçerli noktayı bul
        if (NavMesh.SamplePosition(randomDir, out hit, 10f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero; // Bulamazsa
    }

    void EndMission(string message)
    {
        // Kalan dekorları (arabaları vs) temizle
        foreach (var obj in activeObjects)
        {
            if (obj != null && obj.GetComponent<NetworkObject>() != null)
                obj.GetComponent<NetworkObject>().Despawn();
        }
        activeObjects.Clear();

        EndMissionClientRpc(message);

        isCooldownActive = true;
        cooldownTimer = missionCooldown;
    }

    [ClientRpc]
    void NewMissionClientRpc(string title, string loc, bool isFake)
    {
        if (alertText == null) return;
        alertText.text = $"<size=120%><color=red>⚠️ ANONS: {title}</color></size>\n📍 KONUM: {loc}";

        // Ok işareti scriptin varsa hedefi güncellemek lazım (İstersen onu da ekleriz)
    }

    [ClientRpc]
    void EndMissionClientRpc(string msg)
    {
        if (alertText == null) return;
        alertText.text = $"<color=green>{msg}</color>";
    }
}