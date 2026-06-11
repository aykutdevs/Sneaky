using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SecurityAI : MonoBehaviour
{
    [Header("Devriye (Patrol) Ayarları")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    [Header("Görüş (Vision) Ayarları")]
    public float viewRadius = 15f;
    [Range(0, 360)]
    public float viewAngle = 90f;

    [Header("İşitme (Hearing) Ayarları")]
    public float hearingRadius = 25f; // Sesi duyabileceği mesafe (Sesin yarıçapı da üzerine eklenir)
    public float investigateTime = 4f; // Sese gidince kaç saniye bakacağı

    // AI'ın takip durumu
    private bool isChasing = false;
    private bool isInvestigating = false;
    private float investigateTimer = 0f;

    // Referanslar
    private NavMeshAgent agent;
    private GhostModeManager ghostManager;
    private Transform playerTransform; // Oyuncunun o an kontrol ettiği aktif beden

    [Header("Görsel Ayarlar")]
    public Light viewConeLight;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ghostManager = FindObjectOfType<GhostModeManager>();

        // İlk waypoint'e git
        if (waypoints != null && waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[0].position);
        }

        SetupViewConeVisual();
    }

    private void SetupViewConeVisual()
    {
        if (viewConeLight == null)
        {
            GameObject lightObj = new GameObject("ViewConeLight");
            lightObj.transform.SetParent(this.transform);
            lightObj.transform.localPosition = new Vector3(0, 1.5f, 0); // Göz hizası
            lightObj.transform.localRotation = Quaternion.identity;

            viewConeLight = lightObj.AddComponent<Light>();
            viewConeLight.type = LightType.Spot;
            viewConeLight.color = new Color(1f, 0.9f, 0.2f, 0.5f); // Sarımtırak devriye ışığı
            viewConeLight.shadows = LightShadows.Hard;
        }

        viewConeLight.spotAngle = viewAngle;
        viewConeLight.range = viewRadius;

        // Intensity ve Range değerleri oyunun ışıklandırma yapısına göre inspector'dan da tweak'lenebilir.
        viewConeLight.intensity = 5f;
    }

    void Update()
    {
        DetectPlayer();

        // Takip durumuna göre davranış karar ağacı
        if (isChasing)
        {
            isInvestigating = false; // Kovalamaca varken ses aranmaz
        }
        else if (isInvestigating)
        {
            Investigate();
        }
        else if (waypoints != null && waypoints.Length > 0)
        {
            Patrol();
        }
    }

    public void HearNoise(Vector3 noiseLocation, float noiseVolumeRadius)
    {
        // Kovalamaca yaparken gelen sesle ilgilenme
        if (isChasing) return;

        // Ghost Mode aktifse tamamen salır/duymaz diyebiliriz, ya da sadece görmez. Biz duymasını da engelleyelim.
        if (ghostManager != null && ghostManager.isGhostModeActive) return;

        float distance = Vector3.Distance(transform.position, noiseLocation);

        // Güvenliğin işitme mesafesi + Sesin gürültü mesafesi
        if (distance <= hearingRadius + noiseVolumeRadius)
        {
            isInvestigating = true;
            agent.SetDestination(noiseLocation); // Duyduğu sese doğru git
            investigateTimer = investigateTime;

            Debug.Log(gameObject.name + " şüpheli bir ses duydu! Araştırmaya gidiyor.");
            UpdateViewConeVisual();
        }
    }

    void Investigate()
    {
        // Hedefe (sesin geldiği yere) ulaşıldığında etrafa bakın
        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            investigateTimer -= Time.deltaTime;

            // Etrafına bakınma efekti (Kendi ekseni etrafında yavaşça dönüş)
            transform.Rotate(Vector3.up * 45f * Time.deltaTime);

            if (investigateTimer <= 0)
            {
                isInvestigating = false;
                Debug.Log("Bir şey bulamadım, devriyeye dönüyorum.");
                UpdateViewConeVisual();

                // Kaldığı devriyeden devam et
                if (waypoints.Length > 0)
                    agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
        }
    }

    void Patrol()
    {
        // Hedefe ulaşıldıysa bir sonrakine geç
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    void DetectPlayer()
    {
        // Eğer oyuncu Ghost Mode'daysa: BİZİ KESİNLİKLE GÖREMEZ!
        if (ghostManager != null && ghostManager.isGhostModeActive)
        {
            if (isChasing)
            {
                Debug.Log("Hedef Ghost Moduna geçti! İzi kaybettim.");
                isChasing = false;
                UpdateViewConeVisual();

                // Devriyeye geri dön
                if (waypoints.Length > 0)
                    agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
            return;
        }

        // Sahnedeki aktif denetleyicisi olan (Yani oyuncunun kontrolündeki) objeyi bulalım
        PlayerController[] controllers = FindObjectsOfType<PlayerController>();
        PlayerController activePc = null;
        foreach (var pc in controllers)
        {
            if (pc.enabled)
            {
                activePc = pc;
                break;
            }
        }

        if (activePc == null) return;
        playerTransform = activePc.transform;

        // 1. Mesafe Kontrolü
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= viewRadius)
        {
            // 2. Açı (Görüş Konisi) Kontrolü
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToPlayer) < viewAngle / 2f)
            {
                // 3. Görüş Engel (Duvar vb.) Kontrolü
                RaycastHit hit;
                // AI'ın gözünden hedefin merkezine ışın atalım
                if (Physics.Raycast(transform.position, directionToPlayer, out hit, viewRadius))
                {
                    if (hit.transform == playerTransform)
                    {
                        // 4. Oyuncu Kutu Kılığında mı? Hareketsizse kamuflajdır!
                        PossessableObject possObj = playerTransform.GetComponent<PossessableObject>();
                        Rigidbody targetRb = playerTransform.GetComponent<Rigidbody>();

                        // Kutuysa ve hızı çok düşükse (hareketsizse)
                        bool isMoving = targetRb != null && targetRb.velocity.magnitude > 0.1f;

                        if (possObj != null && !isMoving)
                        {
                            // Kamuflaj başarılı! AI bunu normal bir kutu zannediyor.
                            return;
                        }

                        // Eğer buraya kadar geldiysek, oyuncuyu gördük! (Ya ana bedende, ya da kutu kılığında hareket ediyor)
                        if (!isChasing)
                        {
                            Debug.Log("Güvenlik YAKALADI: " + playerTransform.name);
                            isChasing = true;
                            UpdateViewConeVisual();
                        }

                        agent.SetDestination(playerTransform.position);
                    }
                }
            }
        }
        else
        {
            // Eğer görüş alanından çıkarsa takibi bırakabilir (İsterseniz belirli bir süre daha arasın diye eklenebilir)
            if (isChasing)
            {
                isChasing = false;
                UpdateViewConeVisual();
                if (waypoints.Length > 0)
                    agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
        }
    }

    private void UpdateViewConeVisual()
    {
        if (viewConeLight != null)
        {
            if (isChasing)
            {
                viewConeLight.color = Color.red;
                viewConeLight.intensity = 15f; // Çok Parlak Kırmızı (Tehlike)
            }
            else if (isInvestigating)
            {
                viewConeLight.color = new Color(1f, 0.5f, 0f, 0.8f); // Turuncu (Şüphe)
                viewConeLight.intensity = 8f;
            }
            else
            {
                viewConeLight.color = new Color(1f, 0.9f, 0.2f, 0.5f); // Sarımtırak (Normal Devriye)
                viewConeLight.intensity = 5f;
            }
        }
    }

    // Unity Editörü içinde görüş konisini çizdirmek (görselleştirme) için iyi bir pratik
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
