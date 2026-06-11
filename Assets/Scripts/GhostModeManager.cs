using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class GhostModeManager : MonoBehaviour
{
    [Header("Ghost Mode Settings")]
    public KeyCode ghostModeKey = KeyCode.Z;
    public float ghostDuration = 5f;
    public float ghostCooldown = 10f;

    [Header("Effects")]
    public float speedMultiplier = 2f;
    public float ghostFOV = 40f;
    public float ghostVisionRange = 20f; // Ghost modunda görüş mesafesi sınırı

    // Security AI bu değişkeni okuyacak
    public bool isGhostModeActive { get; private set; } = false;

    // Sayaçlar
    private float ghostTimer = 0f;
    private float cooldownTimer = 0f;

    // Referanslar
    private PlayerController activeController;
    private float originalSpeed;
    private Camera mainCamera;
    private float originalFOV;
    private float originalFarClip;

    // Efektler
    private TrailRenderer ghostTrail;

    void Start()
    {
        mainCamera = GetComponentInChildren<Camera>();
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
        {
            originalFOV = mainCamera.fieldOfView;
            originalFarClip = mainCamera.farClipPlane;
        }

        // Görsel efekt için TrailRenderer oluştur
        SetupGhostTrail();
    }

    private void SetupGhostTrail()
    {
        ghostTrail = gameObject.AddComponent<TrailRenderer>();
        ghostTrail.time = 0.5f; // İzin silinme süresi
        ghostTrail.startWidth = 0.8f;
        ghostTrail.endWidth = 0f;
        ghostTrail.emitting = false; // Başlangıçta kapalı

        // Şeffaf, hayaletimsi mavi bir renk
        ghostTrail.startColor = new Color(0f, 0.8f, 1f, 0.5f);
        ghostTrail.endColor = new Color(0f, 0.8f, 1f, 0f);

        // Standart materyal (Sprite/Default shader genelde iyidir)
        ghostTrail.material = new Material(Shader.Find("Sprites/Default"));
    }

    void Update()
    {
        // Yetenek bekleme süresi
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // Ghost Modu Başlatma (Aktif değilse, Z'ye basıldıysa ve Cooldown bittiyse)
        if (!isGhostModeActive && Input.GetKeyDown(ghostModeKey) && cooldownTimer <= 0)
        {
            ActivateGhostMode();
        }

        // Ghost Modu Aktifken Süre Sayımı
        if (isGhostModeActive)
        {
            ghostTimer -= Time.deltaTime;
            if (ghostTimer <= 0)
            {
                DeactivateGhostMode();
            }
        }
    }

    private void ActivateGhostMode()
    {
        isGhostModeActive = true;
        ghostTimer = ghostDuration;

        // Sahnedeki o an aktif olan denetleyiciyi bul
        // Bu sayede kutu içindeyken bile hızlanabiliriz.
        PlayerController[] controllers = FindObjectsOfType<PlayerController>();
        foreach (var pc in controllers)
        {
            if (pc.enabled)
            {
                activeController = pc;
                break;
            }
        }

        if (activeController != null)
        {
            originalSpeed = activeController.moveSpeed;
            activeController.moveSpeed = originalSpeed * speedMultiplier;
        }

        // Görüş alanını daralt (Klostrofobik etki) ve görüş menzilini kıs
        if (mainCamera != null)
        {
            mainCamera.fieldOfView = ghostFOV;
            mainCamera.farClipPlane = ghostVisionRange;
        }

        // İz efektini aç
        if (ghostTrail != null)
        {
            ghostTrail.emitting = true;
        }

        Debug.Log("Ghost Mode Aktif! Hız arttı, görünmez oldunuz.");
    }

    private void DeactivateGhostMode()
    {
        isGhostModeActive = false;
        cooldownTimer = ghostCooldown;

        // Eski hıza geri dön
        if (activeController != null)
        {
            activeController.moveSpeed = originalSpeed;
        }

        // Kamerayı normale döndür
        if (mainCamera != null)
        {
            mainCamera.fieldOfView = originalFOV;
            mainCamera.farClipPlane = originalFarClip;
        }

        // İz efektini kapat
        if (ghostTrail != null)
        {
            ghostTrail.emitting = false;
        }

        Debug.Log("Ghost Mode Sona Erdi! Cooldown başladı.");
    }
}