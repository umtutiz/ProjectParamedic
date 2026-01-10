using UnityEngine;
using Unity.Netcode;

public class FinishLine : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Sadece Server kontrol eder
        if (!IsServer) return;

        // Çarpan kiþi bizim oyuncu mu?
        if (other.CompareTag("Player"))
        {
            // Oyuncu hedefe ulaþtý, kazandý!
            AzraelArenaManager.Instance.WinByReachGoal();
        }
    }
}