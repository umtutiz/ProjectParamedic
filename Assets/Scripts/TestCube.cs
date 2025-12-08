using Unity.Netcode;
using UnityEngine;

public class TestCube : NetworkBehaviour, IInteractable
{
    // Varsayýlan rengi Beyaz yapýyoruz
    private NetworkVariable<Color> netColor = new NetworkVariable<Color>(Color.white);

    public override void OnNetworkSpawn()
    {
        netColor.OnValueChanged += OnColorChanged;
        // Baþlangýçta rengi eþitle
        GetComponent<Renderer>().material.color = netColor.Value;
    }

    public string GetInteractText()
    {
        return "Lambayý Yak/Söndür";
    }

    public void Interact(ulong playerID)
    {
        // KONSOLA BAK: Eðer bu yazý çýkýyorsa sistem çalýþýyordur, sorun materyaldedir.
        Debug.Log($"[SERVER] Oyuncu {playerID} küpe dokundu!");

        // Basit Mantýk: Kýrmýzýysa Yeþil yap, deðilse Kýrmýzý yap.
        if (netColor.Value == Color.red)
        {
            netColor.Value = Color.green;
        }
        else
        {
            netColor.Value = Color.red;
        }
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        Debug.Log("[CLIENT] Renk güncellendi!");
        GetComponent<Renderer>().material.color = newColor;
    }
}