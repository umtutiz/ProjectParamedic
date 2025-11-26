using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

public class SteamLobby : MonoBehaviour
{
    // --- YENÝ EKLENEN DEÐÝÞKEN ---
    // Þu an içinde bulunduðumuz lobiyi burada saklayacaðýz
    private Lobby? currentLobby;

    private void Start()
    {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
    }

    public async void HostLobby()
    {
        var lobbyResponse = await SteamMatchmaking.CreateLobbyAsync(4);

        if (!lobbyResponse.HasValue)
        {
            Debug.LogError("Lobi kurulamadý!");
            return;
        }

        Lobby lobby = lobbyResponse.Value;

        lobby.SetPublic();
        lobby.SetData("HostSteamId", SteamClient.SteamId.ToString());

        // --- BURASI EKLENDÝ ---
        currentLobby = lobby;

        Debug.Log("Lobi Kuruldu! ID: " + lobby.Id);
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK) return;

        // Host olarak baþlat
        NetworkManager.Singleton.StartHost();
        Debug.Log("Host Baþlatýldý.");
    }

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        // Davet gelince katýl
        Debug.Log("Lobiye katýlýnýyor...");
        lobby.Join();
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        // --- BURASI EKLENDÝ ---
        currentLobby = lobby; // Girdiðimiz lobiyi hafýzaya al

        if (NetworkManager.Singleton.IsHost) return;

        // Host'un ID'sini bul
        string hostIdString = lobby.GetData("HostSteamId");
        ulong hostId = ulong.Parse(hostIdString);

        // Transport'a hedefi göster
        var transport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
        transport.TargetSteamId = hostId;

        // Client olarak baðlan
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client Baþlatýldý! Host'a baðlanýlýyor: " + hostId);
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("STEAM LOBÝSÝ KUR (HOST)", GUILayout.Height(50)))
            {
                HostLobby();
            }
        }
        else
        {
            GUILayout.Label("Durum: " + (NetworkManager.Singleton.IsHost ? "Host" : "Client"));

            // --- HATA VEREN YER DÜZELDÝ ---
            // Sadece bir lobiye baðlýysak butonu göster
            if (currentLobby.HasValue)
            {
                if (GUILayout.Button("Arkadaþ Davet Et (Shift+Tab)", GUILayout.Height(40)))
                {
                    // Kaydettiðimiz lobinin ID'sini kullanýyoruz
                    SteamFriends.OpenGameInviteOverlay(currentLobby.Value.Id);
                }
            }
        }

        GUILayout.EndArea();
    }
}