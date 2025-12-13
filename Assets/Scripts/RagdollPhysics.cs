using UnityEngine;
using System.Collections.Generic;

public class RagdollPhysics : MonoBehaviour
{
    void Start()
    {
        // 1. Bu sosis adamýn bütün parçalarýndaki Collider'larý bul
        Collider[] myColliders = GetComponentsInChildren<Collider>();

        // 2. Hepsini birbirine düþman et (Birbirlerini görmesinler)
        for (int i = 0; i < myColliders.Length; i++)
        {
            for (int j = i + 1; j < myColliders.Length; j++)
            {
                Physics.IgnoreCollision(myColliders[i], myColliders[j]);
            }
        }
    }
}