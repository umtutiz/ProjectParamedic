using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    void Update()
    {
        // Eðer sahnede Main Camera yoksa (henüz oyuncu doðmadýysa) iþlem yapma
        if (Camera.main == null) return;

        transform.LookAt(Camera.main.transform);
    }
}
