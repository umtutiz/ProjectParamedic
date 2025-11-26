using Unity.Netcode;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host (Oyunu Kur)")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client (Oyuna Katýl)")) NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server (Sadece Sunucu)")) NetworkManager.Singleton.StartServer();
        }
        else
        {
            GUILayout.Label("Mod: " + (NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client"));
        }

        GUILayout.EndArea();
    }
}