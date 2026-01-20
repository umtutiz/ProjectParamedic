using UnityEngine;
using Unity.Netcode;

public class TrafficSpawner : NetworkBehaviour
{
    public GameObject carPrefab;
    public float spawnInterval = 3f; // Kaç saniyede bir araba gelsin?

    private float timer;

    void Update()
    {
        if (!IsServer) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            SpawnCar();
            timer = spawnInterval;
        }
    }

    void SpawnCar()
    {
        if (carPrefab != null)
        {
            GameObject car = Instantiate(carPrefab, transform.position, transform.rotation);
            car.GetComponent<NetworkObject>().Spawn();
        }
    }
}