using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 2f;
    
    [Header("References")]
    [Tooltip("Kamera transformu (Mouse ile etrafa bakmak için)")]
    public Transform cameraTransform;
    
    private Rigidbody rb;
    private float xRotation = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Eğer Rigidbody donmaları (kütle merkezi yuvarlanmaları) ayarlanmamışsa ayarla
        rb.freezeRotation = true;

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }
        
        // Fareyi ekrana kilitle ve gizle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Fare (Mouse Look) kontrolü
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        // Yukarı-Aşağı bakış
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        // Sağa-Sola dönüş (Karakterin tamamını döndür)
        transform.Rotate(Vector3.up * mouseX);
    }

    void FixedUpdate()
    {
        // Klavye WASD hareketleri
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Karakterin baktığı yöne göre hareket vektörü hesapla
        Vector3 move = transform.right * x + transform.forward * z;
        
        // Rigidbody hızını güncelle (Y eksenindeki hızı/yerçekimini koruyarak)
        rb.velocity = new Vector3(move.x * moveSpeed, rb.velocity.y, move.z * moveSpeed);
    }
}
