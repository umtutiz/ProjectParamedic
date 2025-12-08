using UnityEngine;

public interface IInteractable
{
    // Her etkileþime girilen obje bu fonksiyonu içermek ZORUNDA olacak.
    void Interact(ulong playerID);

    // Ekrana "Kapýyý Aç" veya "Hastayý Al" yazdýrmak için
    string GetInteractText();
}