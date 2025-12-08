using Unity.Netcode.Components;
using UnityEngine;

// Standart transform yerine bunu kullanacaðýz ki oyuncu kendini hareket ettirebilsin.
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Yetkiyi sunucudan al, oyuncuya ver.
    }
}