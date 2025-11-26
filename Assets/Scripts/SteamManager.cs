using System;
using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    private void Awake()
    {
        try
        {
            // Steam'i baþlat (480 = SpaceWar test oyunu ID'si)
            SteamClient.Init(480);
            Debug.Log("Steam Baþlatýldý!");
        }
        catch (Exception e)
        {
            Debug.LogError("Steam HATA: " + e.Message);
        }
    }

    private void Update()
    {
        // Steam'den gelen mesajlarý dinle
        SteamClient.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        SteamClient.Shutdown();
    }
}