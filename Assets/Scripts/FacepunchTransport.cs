using System;
using System.Collections.Generic;
using System.Linq; // <-- BU EKLENDÝ (Hata Çözümü)
using System.Runtime.InteropServices;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

public class FacepunchTransport : NetworkTransport
{
    // --- TEMEL AYARLAR ---
    public override ulong ServerClientId => 0;
    public ulong TargetSteamId = 0;

    // --- YAPILAR ---
    private SocketManager serverSocket;
    private ConnectionManager clientConnection;

    private struct NetworkEventData
    {
        public ulong ClientId;
        public ArraySegment<byte> Payload;
        public float ReceiveTime;
        public NetworkEvent Type;
    }

    private readonly Queue<NetworkEventData> messageQueue = new Queue<NetworkEventData>();

    // --- UNITY STARTUP ---
    public override void Initialize(NetworkManager networkManager = null)
    {
    }

    public override void Shutdown()
    {
        clientConnection?.Close();
        serverSocket?.Close();
        clientConnection = null;
        serverSocket = null;
        messageQueue.Clear();
        Debug.Log("Transport Kapatýldý.");
    }

    // --- CLIENT (OYUNCU) KISMI ---
    public override bool StartClient()
    {
        if (TargetSteamId == 0)
        {
            Debug.LogError("Hedef Steam ID girilmedi!");
            return false;
        }

        clientConnection = SteamNetworkingSockets.ConnectRelay<FacepunchConnection>((SteamId)TargetSteamId);
        ((FacepunchConnection)clientConnection).Transport = this;

        Debug.Log($"Client baþlatýldý, Hedef: {TargetSteamId}");
        return clientConnection != null;
    }

    // --- SERVER (HOST) KISMI ---
    public override bool StartServer()
    {
        serverSocket = SteamNetworkingSockets.CreateRelaySocket<FacepunchSocket>(0);
        ((FacepunchSocket)serverSocket).Transport = this;

        Debug.Log("Server baþlatýldý (Relay Socket).");
        return serverSocket != null;
    }

    // --- MESAJ ALMA VE GÖNDERME DÖNGÜSÜ ---
    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
    {
        if (serverSocket != null) serverSocket.Receive();
        if (clientConnection != null) clientConnection.Receive();

        if (messageQueue.Count > 0)
        {
            var data = messageQueue.Dequeue();
            clientId = data.ClientId;
            payload = data.Payload;
            receiveTime = data.ReceiveTime;
            return data.Type;
        }

        clientId = 0;
        payload = default;
        receiveTime = Time.realtimeSinceStartup;
        return NetworkEvent.Nothing;
    }

    public override void Send(ulong clientId, ArraySegment<byte> data, NetworkDelivery delivery)
    {
        IntPtr ptr = Marshal.AllocHGlobal(data.Count);
        try
        {
            Marshal.Copy(data.Array, data.Offset, ptr, data.Count);

            if (serverSocket != null)
            {
                // HATA BURADAYDI DÜZELDÝ: .Find yerine .FirstOrDefault
                Connection conn = serverSocket.Connected.FirstOrDefault(x => x.Id == (uint)clientId);
                if (conn.Id != 0)
                {
                    conn.SendMessage(ptr, data.Count, delivery == NetworkDelivery.Reliable ? SendType.Reliable : SendType.Unreliable);
                }
            }
            else if (clientConnection != null)
            {
                clientConnection.Connection.SendMessage(ptr, data.Count, delivery == NetworkDelivery.Reliable ? SendType.Reliable : SendType.Unreliable);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public override void DisconnectRemoteClient(ulong clientId)
    {
        if (serverSocket != null)
        {
            // HATA BURADAYDI DÜZELDÝ: .Find yerine .FirstOrDefault
            Connection conn = serverSocket.Connected.FirstOrDefault(x => x.Id == (uint)clientId);
            if (conn.Id != 0) conn.Close();
        }
    }

    public override void DisconnectLocalClient()
    {
        Shutdown();
    }

    public override ulong GetCurrentRtt(ulong clientId) => 0;

    public void AddToQueue(ulong clientId, NetworkEvent type, ArraySegment<byte> payload = default)
    {
        messageQueue.Enqueue(new NetworkEventData
        {
            ClientId = clientId,
            Type = type,
            Payload = payload,
            ReceiveTime = Time.realtimeSinceStartup
        });
    }
}

// --- STEAM CALLBACK SINIFLARI ---

public class FacepunchSocket : SocketManager
{
    public FacepunchTransport Transport;

    public override void OnConnected(Connection connection, ConnectionInfo info)
    {
        base.OnConnected(connection, info);
        Debug.Log($"[Server] Oyuncu baðlandý ID: {connection.Id}");
        Transport.AddToQueue(connection.Id, NetworkEvent.Connect);
    }

    public override void OnDisconnected(Connection connection, ConnectionInfo info)
    {
        base.OnDisconnected(connection, info);
        Debug.Log($"[Server] Oyuncu koptu ID: {connection.Id}");
        Transport.AddToQueue(connection.Id, NetworkEvent.Disconnect);
    }

    // Server için doðru OnMessage (Connection parametreli)
    public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        byte[] managedArray = new byte[size];
        Marshal.Copy(data, managedArray, 0, size);

        Transport.AddToQueue(connection.Id, NetworkEvent.Data, new ArraySegment<byte>(managedArray));
    }
}

public class FacepunchConnection : ConnectionManager
{
    public FacepunchTransport Transport;

    public override void OnConnected(ConnectionInfo info)
    {
        base.OnConnected(info);
        Debug.Log("[Client] Sunucuya baðlandý!");
        Transport.AddToQueue(0, NetworkEvent.Connect);
    }

    public override void OnDisconnected(ConnectionInfo info)
    {
        base.OnDisconnected(info);
        Debug.Log("[Client] Sunucudan koptu.");
        Transport.AddToQueue(0, NetworkEvent.Disconnect);
    }

    public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        byte[] managedArray = new byte[size];
        Marshal.Copy(data, managedArray, 0, size);

        Transport.AddToQueue(0, NetworkEvent.Data, new ArraySegment<byte>(managedArray));
    }
}