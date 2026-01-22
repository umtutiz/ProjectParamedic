using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class ObjectiveIndicator : MonoBehaviour
{
    [Header("AYARLAR")]
    public Transform player; // Oyuncunun kendisi (Otomatik bulacağız)
    public Image arrowImage; // Ekranda dönecek ok resmi
    public float hideDistance = 5f; // 5 metre yaklaşınca ok kaybolsun

    private Transform target; // Hedef (Hasta veya Ambulans)

    void Update()
    {
        // Oyuncuyu bulamadıysak bul
        if (player == null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            player = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
        }

        if (player == null) return;

        // HEDEF BELİRLEME (Server'daki ID'den objeyi bul)
        if (MissionManager.Instance != null)
        {
            ulong targetId = MissionManager.Instance.currentPatientId.Value;

            // NetworkManager o ID'ye sahip objeyi bulabilir mi?
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetNetObj))
            {
                target = targetNetObj.transform;
            }
        }

        // HEDEF YOKSA VEYA HASTA ÖLDÜYSE OKU GİZLE
        if (target == null)
        {
            if (arrowImage != null) arrowImage.enabled = false;
            return;
        }

        // HEDEF VARSA OKU GÖSTER VE DÖNDÜR
        if (arrowImage != null)
        {
            float distance = Vector3.Distance(player.position, target.position);

            // Çok yakındaysak oku gizle (Kafa karıştırmasın)
            arrowImage.enabled = (distance > hideDistance);

            if (arrowImage.enabled)
            {
                // Hedefin oyuncuya göre yönünü hesapla
                Vector3 dir = target.position - player.position;

                // Sadece Y ekseninde (sağa sola) değil, ekran düzleminde açı hesaplamamız lazım.
                // Basit yöntem: Oyuncunun forward'ı ile hedef yönü arasındaki açı.

                // Kameraya göre hesaplamak en doğrusu:
                Transform cam = Camera.main.transform;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);

                // Hedef arkamızda mı?
                if (screenPos.z < 0)
                {
                    screenPos.x = Screen.width - screenPos.x;
                    screenPos.y = Screen.height - screenPos.y;
                }

                // Ekranın ortasına göre yön
                Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0) / 2;
                Vector3 screenDir = screenPos - screenCenter;

                float angle = Mathf.Atan2(screenDir.y, screenDir.x) * Mathf.Rad2Deg;

                // Oku döndür (Ok görselinin sağa baktığını varsayıyoruz, değilse +90 ekle)
                arrowImage.rectTransform.rotation = Quaternion.Euler(0, 0, angle - 90);

                // Oku ekranın kenarlarına sabitle (Clamp) - İstersen bu kısmı ekleyebilirim, şimdilik sadece dönsün.
            }
        }
    }
}