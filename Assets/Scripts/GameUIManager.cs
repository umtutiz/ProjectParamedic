using TMPro;
using UnityEngine;
using UnityEngine.UI; // Bunu eklemeyi unutma!

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("ETKÝLEÞÝM UI")]
    public TextMeshProUGUI interactionText;

    // --- YENÝ EKLENEN KISIM ---
    [Header("STAMINA UI")]
    public CanvasGroup staminaCanvasGroup; // Görünmezlik kutusu
    public Image staminaBarFill;           // Yeþil dolan bar
    // ---------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (interactionText != null) interactionText.gameObject.SetActive(false);

        // Stamina barý baþlangýçta gizle
        if (staminaCanvasGroup != null) staminaCanvasGroup.alpha = 0;
    }
}