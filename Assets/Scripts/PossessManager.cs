using UnityEngine;
using System.Collections;

public class PossessManager : MonoBehaviour
{
    [Header("Possess Settings")]
    [Tooltip("Ele geçirme menzili (Raycast mesafesi)")]
    public float maxPossessRange = 15f;
    [Tooltip("Sadece bu layer'daki objeleri algılamasını isterseniz burdan seçebilirsiniz.")]
    public LayerMask possessableLayer = Physics.DefaultRaycastLayers;

    [Header("Controls")]
    public KeyCode possessKey = KeyCode.E;
    public KeyCode exitKey = KeyCode.Q;

    [Header("References")]
    public Camera mainCamera;

    [Header("UI & Indicators")]
    [Tooltip("Ekranın ortasındaki sabit nişangah (sürükle-bırak için boş bırakılabilir)")]
    public UnityEngine.UI.Image crosshairImage;
    [Tooltip("Kutunun üzerinde çıkacak olan işaretçi objesi")]
    public GameObject targetIndicator;

    [Header("Camera Transition")]
    public float cameraTransitionDuration = 0.5f;
    [Tooltip("Kutu halindeyken 3. şahıs kamera ofseti")]
    public Vector3 thirdPersonOffset = new Vector3(0, 2f, -3f);
    [Tooltip("Ana karakterdeyken 1. şahıs kamera ofseti")]
    public Vector3 firstPersonOffset = new Vector3(0, 0.5f, 0);
    private Coroutine cameraTransitionCoroutine;

    // Orijinal (hayalet) oyuncu bedeni
    private GameObject originalBody;
    private PlayerController originalController;

    // Şu an kontrol edilen obje (kutu veya ana beden)
    private GameObject currentPossessedObject;

    // Şu an bakılan hedeflenebilir obje
    private PossessableObject currentTarget;

    void Start()
    {
        originalBody = this.gameObject;
        originalController = GetComponent<PlayerController>();
        currentPossessedObject = originalBody;

        if (mainCamera == null)
        {
            mainCamera = GetComponentInChildren<Camera>(); // Ana karakterin içindeki kamerayı bul
        }

        if (targetIndicator != null)
        {
            targetIndicator.SetActive(false); // Başlangıçta işaretçiyi gizle
        }
    }

    void Update()
    {
        CheckTarget();

        // 1. Ele Başka Bir Nesneyi Ele Geçirme (E)
        if (Input.GetKeyDown(possessKey))
        {
            if (currentTarget != null)
            {
                Possess(currentTarget.gameObject, currentTarget.speedMultiplier);
            }
        }

        // 2. Kontrol Edilen Nesneden Çıkıp Ana Bedene Dönme (Q)
        if (Input.GetKeyDown(exitKey) && currentPossessedObject != originalBody)
        {
            ReturnToOriginalBody();
        }
    }

    private void CheckTarget()
    {
        // Önce hedefi sıfırla
        currentTarget = null;
        if (targetIndicator != null) targetIndicator.SetActive(false);
        if (crosshairImage != null) crosshairImage.color = Color.white;

        // Kameranın merkezinden ileriye doğru bir ışın gönder
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxPossessRange, possessableLayer))
        {
            PossessableObject possessable = hit.collider.GetComponent<PossessableObject>();

            if (possessable != null && possessable.gameObject != currentPossessedObject)
            {
                currentTarget = possessable;

                if (targetIndicator != null)
                {
                    // Kutunun boyutlarını (Bounds) alarak en üst noktasını bulalım
                    Renderer targetRenderer = possessable.GetComponentInChildren<Renderer>();
                    if (targetRenderer != null)
                    {
                        // Objnin en üst noktası (bounds.max.y) + biraz boşluk (0.5f)
                        float topY = targetRenderer.bounds.max.y + 0.5f;
                        targetIndicator.transform.position = new Vector3(possessable.transform.position.x, topY, possessable.transform.position.z);
                    }
                    else
                    {
                        // Eğer Renderer bulamazsa fallback olarak objenin merkezinden yukarı sabitle
                        targetIndicator.transform.position = possessable.transform.position + Vector3.up * 1.5f;
                    }

                    targetIndicator.SetActive(true);
                }

                if (crosshairImage != null)
                {
                    crosshairImage.color = Color.green;
                }
            }
        }
    }

    private void Possess(GameObject targetObject, float speedMult)
    {
        // A) Eğer ana bedenden kutuya geçiyorsak, ana bedeni görünmez yap ve kontrollerini kapat
        if (currentPossessedObject == originalBody)
        {
            originalController.enabled = false;
            SetBodyActiveState(originalBody, false);
        }
        // B) Eğer bir kutudan başka bir kutuya geçiyorsak, eski kutudaki kontrolleri sil
        else
        {
            PlayerController oldPc = currentPossessedObject.GetComponent<PlayerController>();
            if (oldPc != null) Destroy(oldPc);
        }

        // 2. Yeni objeye PlayerController ekle ve ayarlarını aktar
        PlayerController newPc = targetObject.GetComponent<PlayerController>();
        if (newPc == null)
        {
            newPc = targetObject.AddComponent<PlayerController>();
        }

        newPc.moveSpeed = originalController.moveSpeed * speedMult;
        newPc.rotationSpeed = originalController.rotationSpeed;
        newPc.cameraTransform = mainCamera.transform; // Kamerayı yeni kontrolcüye tanıt

        // 3. Kamerayı yeni objenin içine/üstüne pürüzsüz taşı
        mainCamera.transform.SetParent(targetObject.transform);
        if (cameraTransitionCoroutine != null) StopCoroutine(cameraTransitionCoroutine);
        cameraTransitionCoroutine = StartCoroutine(SmoothCameraTransition(thirdPersonOffset));

        // Kontrol edilen objeyi güncelle
        currentPossessedObject = targetObject;

        // Görsel efekt: Hem ana bedende hem de yeni bedende bir patlama efekti oluştur
        CreatePossessEffect(originalBody.transform.position);
        CreatePossessEffect(targetObject.transform.position);

        // Possession gerçekleştikten sonra hedef ve görünümü sıfırla ki, sahip olunan objede işaretçi kalmasın.
        currentTarget = null;
        if (targetIndicator != null) targetIndicator.SetActive(false);
        if (crosshairImage != null) crosshairImage.color = Color.white;

        Debug.Log("Ele geçirildi: " + targetObject.name + " (3rd Person View)");
    }

    private void ReturnToOriginalBody()
    {
        if (currentPossessedObject == originalBody) return;

        // 1. Bulunduğumuz kutudaki kontrolcü scriptini kaldır
        PlayerController currentPc = currentPossessedObject.GetComponent<PlayerController>();
        if (currentPc != null) Destroy(currentPc);

        // 2. Ana bedeni şu anki kutunun bir tık üstüne/yanına ışınla
        // Bu sayede "saklambaç" mekaniğine uygun şekilde, çıkış yaptığımız kutunun yanında belirmiş oluruz.
        originalBody.transform.position = currentPossessedObject.transform.position + Vector3.up * 1.5f;
        originalBody.transform.rotation = currentPossessedObject.transform.rotation;

        // 3. Ana bedeni geri aktif et
        SetBodyActiveState(originalBody, true);
        originalController.enabled = true;

        // 4. Kamerayı ana bedene pürüzsüz taşı
        mainCamera.transform.SetParent(originalBody.transform);
        if (cameraTransitionCoroutine != null) StopCoroutine(cameraTransitionCoroutine);
        cameraTransitionCoroutine = StartCoroutine(SmoothCameraTransition(firstPersonOffset));

        currentPossessedObject = originalBody;

        // Görsel efekt: Çıkış yapılan yerde ve ana bedende efekt oluştur
        CreatePossessEffect(currentPossessedObject.transform.position);
        CreatePossessEffect(originalBody.transform.position);

        Debug.Log("Ana bedene (Ghost'a) dönüldü. (1st Person View)");
    }

    private void CreatePossessEffect(Vector3 position)
    {
        // Kod üzerinden dinamik bir partikül efekti yaratıyoruz
        GameObject effectObj = new GameObject("PossessParticleEffect");
        effectObj.transform.position = position;

        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = 0.5f;
        main.startSpeed = 8f;
        main.startSize = 0.3f;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.cyan, Color.white);
        main.loop = false;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        // Standart partikül materyali
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        // İşlem bitince çöpe at
        Destroy(effectObj, 2f);
    }

    private IEnumerator SmoothCameraTransition(Vector3 targetLocalPos)
    {
        Vector3 startLocalPos = mainCamera.transform.localPosition;
        Quaternion startLocalRot = mainCamera.transform.localRotation;
        Quaternion targetLocalRot = Quaternion.identity;

        float timer = 0f;
        while (timer < cameraTransitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / cameraTransitionDuration;
            // SmoothStep ile daha yumuşak geçiş
            t = t * t * (3f - 2f * t);

            mainCamera.transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            mainCamera.transform.localRotation = Quaternion.Lerp(startLocalRot, targetLocalRot, t);
            yield return null;
        }

        mainCamera.transform.localPosition = targetLocalPos;
        mainCamera.transform.localRotation = targetLocalRot;
    }

    private void SetBodyActiveState(GameObject obj, bool isVisible)
    {
        // Objede ve altındaki mesh'leri aç/kapat
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            r.enabled = isVisible;
        }

        // Orijinal bedenin çarpışmalarını aç/kapat, böylece görünmezken bir yerlere takılmaz
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = isVisible;

        // Görünmez formda yerçekiminden etkilenmemesi (örneğin haritadan düşmemesi) için
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = !isVisible;
    }
}
