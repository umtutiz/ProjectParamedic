using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class MissionManager : NetworkBehaviour
{
    public static MissionManager Instance;

    [Header("AYARLAR")]
    public GameObject patientPrefab; // Hasta Prefabı
    public float delayBetweenMissions = 5f; // Teslimden kaç sn sonra yenisi gelsin?

    [Header("DOĞUŞ NOKTALARI")]
    public List<Transform> spawnPoints; // Haritadaki boş objeler (Spawn noktaları)

    // Şu anki aktif hastayı tutar (Ok işareti buna bakacak)
    public NetworkVariable<ulong> currentPatientId = new NetworkVariable<ulong>();
    private bool missionActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Oyun başlar başlamaz ilk hastayı çağır
            StartCoroutine(SpawnRoutine(1f));
        }
    }

    // AmbulansArea.cs bu fonksiyonu çağıracak (Hasta teslim edilince)
    public void MissionComplete()
    {
        if (!IsServer) return;

        missionActive = false;
        Debug.Log("GÖREV TAMAMLANDI! Yenisi hazırlanıyor...");

        // Biraz bekle sonra yeni hasta yarat
        StartCoroutine(SpawnRoutine(delayBetweenMissions));
    }

    IEnumerator SpawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (spawnPoints.Count > 0 && patientPrefab != null)
        {
            // 1. Rastgele bir nokta seç
            int randomIndex = Random.Range(0, spawnPoints.Count);
            Transform spawnPoint = spawnPoints[randomIndex];

            // 2. Hastayı yarat
            GameObject newPatient = Instantiate(patientPrefab, spawnPoint.position, spawnPoint.rotation);
            NetworkObject netObj = newPatient.GetComponent<NetworkObject>();
            netObj.Spawn();

            // 3. ID'yi kaydet (Herkes bu ID'yi görüp ok işaretini ona çevirecek)
            currentPatientId.Value = netObj.NetworkObjectId;
            missionActive = true;

            Debug.Log($"YENİ GÖREV: Nokta {randomIndex} konumunda hasta var!");
        }
    }
}