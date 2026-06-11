using System.Collections;
using UnityEngine;

public class DistractionAbility : MonoBehaviour
{
    [Header("Dikkat Dağıtma Ayarları")]
    public KeyCode distractKey = KeyCode.F;
    [Tooltip("Öğenin fırlatılma hızı")]
    public float throwVelocity = 15f;
    [Tooltip("Sesin uyaracağı güvenlik alanı çapı")]
    public float noiseRadius = 15f;
    [Tooltip("Yetenek bekleme süresi")]
    public float cooldown = 4f;

    private float timer = 0f;
    private Camera mainCamera;

    void Start()
    {
        // Doğru ve güncel kamerayı (sürekli oynadığın kamerayı) PossessManager'dan bulalım.
        // Çünkü sahnede Camera.main dediğimizde sabit duran yanlış bir kamerayı seçiyor olabilir!
        PossessManager pm = FindObjectOfType<PossessManager>();
        if (pm != null)
        {
            mainCamera = pm.mainCamera;
        }

        // Eğer hala bulunamadıysa yedeği kullan
        if (mainCamera == null) mainCamera = GetComponentInChildren<Camera>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        if (timer > 0) timer -= Time.deltaTime;

        if (Input.GetKeyDown(distractKey) && timer <= 0f)
        {
            ThrowDistraction();
        }
    }

    void ThrowDistraction()
    {
        if (mainCamera == null) return;

        // Aktif olarak kontrol ettiğimiz bedeni bulalım (Ghost veya Kutunun kendisi)
        PlayerController activePc = null;
        PlayerController[] allPcs = FindObjectsOfType<PlayerController>();
        foreach (var pc in allPcs)
        {
            if (pc.enabled)
            {
                activePc = pc;
                break;
            }
        }

        // 1) Fırlatılacak objeyi oluştur (Sarı bir kapsül ya da küp yapalım ki senin mor küreyle karışmasın!)
        GameObject ping = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ping.name = "DikkatDagiticiObje";

        // Objenin çıkış noktasını tam olarak kontrol ettiğimiz bedenin biraz üstü ve önü yapalım
        if (activePc != null)
        {
            ping.transform.position = activePc.transform.position + Vector3.up * 1f + mainCamera.transform.forward * 1f;
        }
        else
        {
            ping.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 1.5f;
        }

        ping.transform.localScale = Vector3.one * 0.3f;

        // Rengi sarı yapalım, kesinlikle TargetIndicator'dan farklı olduğu anlaşılsın
        Renderer rend = ping.GetComponent<Renderer>();
        if (rend != null) rend.material.color = Color.yellow;

        // 2) Kendi karakterimizle (Veya içine girdiğimiz MAVİ/KIRMIZI kutuyla) çarpışmamasını
        // KESİN OLARAK garantiye almalıyız. Yoksa içinden çıkamaz ve karakteri dondurur.
        Collider pingCol = ping.GetComponent<Collider>();
        if (pingCol != null)
        {
            // Sahnedeki (o an içinde bulunduğun KUTU dahil) tüm oyuncu kontrollerini bul
            PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();
            foreach (var pc in allPlayers)
            {
                Collider[] myCols = pc.GetComponentsInChildren<Collider>();
                foreach (var c in myCols)
                {
                    Physics.IgnoreCollision(pingCol, c);
                }
            }

            // Eğer TargetIndicator (Hedef İşaretçisi) adında bir objeye takılıyorsa, onu da es geçelim.
            PossessManager pm = FindObjectOfType<PossessManager>();
            if (pm != null && pm.targetIndicator != null)
            {
                Collider indCol = pm.targetIndicator.GetComponent<Collider>();
                if (indCol != null) Physics.IgnoreCollision(pingCol, indCol);
            }
        }

        // 3) Fiziğini ekle ve ileri doğru fırlat (Kavisli düşsün diye biraz yukarı kuvvet ver)
        Rigidbody rb = ping.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Çizgisel hızla duvardan geçmesin
        rb.velocity = mainCamera.transform.forward * throwVelocity + Vector3.up * 3f;

        // Fırlatıldıktan sonra süreci yönetecek Coroutine'i başlat
        StartCoroutine(DistractionRoutine(ping));

        timer = cooldown;
    }

    private IEnumerator DistractionRoutine(GameObject ping)
    {
        // Küre 1.5 saniye boyunca uçsun (veya yere düşene kadar beklensin)
        yield return new WaitForSeconds(1.5f);

        if (ping == null) yield break;

        // --- ARTİK SES DALGASI YAYILMA ANI ---
        Vector3 soundOrigin = ping.transform.position;

        // Kapsül olduğu yerde kilitlensin ve büyüsün (Ses dalgası efektini temsil eder)
        Destroy(ping.GetComponent<Rigidbody>()); // Fiziği sil (hareketi dursun)
        Destroy(ping.GetComponent<Collider>()); // Çarpışmayı sil (kimseye takılmasın)

        ping.transform.localScale = Vector3.one * 1.0f; // Boyutunu büyüt (ses dalgası)

        // Yakındaki güvenlikleri uyar
        SecurityAI[] guards = FindObjectsOfType<SecurityAI>();
        foreach (var guard in guards)
        {
            guard.HearNoise(soundOrigin, noiseRadius);
        }

        Debug.Log("Gürültü çıkarıldı! Konum: " + soundOrigin);

        // Küre (Ses dalgası) 3 saniye orada kalsın
        yield return new WaitForSeconds(3f);

        if (ping != null) Destroy(ping);
    }
}
