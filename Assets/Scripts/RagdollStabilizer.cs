using UnityEngine;

public class RagdollPhysics : MonoBehaviour
{
    void Start()
    {
        // Bu objenin altýndaki tüm parçalarý bul
        Collider[] myColliders = GetComponentsInChildren<Collider>();

        // Hepsinin birbiriyle çarpýþmasýný kapat (Sadece dýþ dünyaya çarpsýnlar)
        for (int i = 0; i < myColliders.Length; i++)
        {
            for (int j = i + 1; j < myColliders.Length; j++)
            {
                Physics.IgnoreCollision(myColliders[i], myColliders[j]);
            }
        }
    }
}