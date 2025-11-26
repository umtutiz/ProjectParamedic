using System;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using System.Runtime.InteropServices;

public class FacepunchTransport : NetworkTransport
{
    public override ulong ServerClientId => 0;

    private SocketManager serverSocket;
    private ConnectionManager clientConnection;
    public ulong TargetSteamId = 0;

    public override void Initialize(NetworkManager networkManager = null)
    {
    }

    public override bool StartClient()
    {
        if (TargetSteamId == 0)
        {
            Debug.LogError("Hedef Steam ID girilmedi!");
            return false;
        }

        // --- DÜZELTÝLEN SATIR ---
        // Artýk <FacepunchConnection> diyerek hangi yöneticiyi kullanacaðýný söylüyoruz.
        clientConnection = SteamNetworkingSockets.ConnectRelay<FacepunchConnection>((SteamId)TargetSteamId);

        Debug.Log($"Client: {TargetSteamId} sunucusuna baðlanýlýyor...");
        return clientConnection != null;
    }

    public override bool StartServer()
    {
        // Sunucu soketini oluþtur
        serverSocket = SteamNetworkingSockets.CreateRelaySocket<FacepunchSocket>(0);
        Debug.Log("Server: Steam Relay Soketi Açýldý.");
        return serverSocket != null;
    }

    public override void DisconnectRemoteClient(ulong clientId) { }

    public override void DisconnectLocalClient()
    {
        clientConnection?.Close();
        serverSocket?.Close();
    }

    public override ulong GetCurrentRtt(ulong clientId) => 0;

    public override void Shutdown()
    {
        clientConnection?.Close();
        serverSocket?.Close();
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
    {
        clientId = 0;
        receiveTime = Time.realtimeSinceStartup;
        payload = default;

        if (serverSocket != null) serverSocket.Receive(100);
        if (clientConnection != null) clientConnection.Receive(100);

        return NetworkEvent.Nothing;
    }

    public override void Send(ulong clientId, ArraySegment<byte> data, NetworkDelivery delivery)
    {
        if (clientConnection != null && clientConnection.Connected)
        {
            IntPtr ptr = Marshal.AllocHGlobal(data.Count);
            try
            {
                Marshal.Copy(data.Array, data.Offset, ptr, data.Count);
                clientConnection.Connection.SendMessage(ptr, data.Count, SendType.Reliable);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}

// --- YENÝ EKLENEN SINIFLAR ---

// Sunucu tarafýndaki olaylarý yöneten sýnýf
public class FacepunchSocket : SocketManager
{
    public override void OnConnected(Connection connection, ConnectionInfo info)
    {
        base.OnConnected(connection, info);
        Debug.Log($"[Server] Biri baðlandý: {info.Identity.SteamId}");
    }

    public override void OnDisconnected(Connection connection, ConnectionInfo info)
    {
        base.OnDisconnected(connection, info);
        Debug.Log($"[Server] Biri ayrýldý: {info.Identity.SteamId}");
    }
}

// Ýstemci (Oyuncu) tarafýndaki olaylarý yöneten sýnýf (HATANIN SEBEBÝ BUYDU, ARTIK VAR)
public class FacepunchConnection : ConnectionManager
{
    public override void OnConnected(ConnectionInfo info)
    {
        base.OnConnected(info);
        Debug.Log("[Client] Sunucuya baþarýyla baðlandýk!");
    }

    public override void OnDisconnected(ConnectionInfo info)
    {
        base.OnDisconnected(info);
        Debug.Log("[Client] Sunucudan koptuk.");
    }
}