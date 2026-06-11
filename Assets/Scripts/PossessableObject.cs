using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PossessableObject : MonoBehaviour
{
    [Header("Possess Properties")]
    [Tooltip("Bu nesne kontrol edilirken hız çarpanı (örn: 1.2f hızlı, 0.8f yavaş)")]
    public float speedMultiplier = 1f;

    [Tooltip("Güvenlik taramasında nesnenin şüphe yarıçapı")]
    public float suspicionRadius = 5f;
    
    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        // Kutuların yuvarlanmaması için X ve Z eksenlerinde dönüşlerini donduralım.
        rb.freezeRotation = true; 
    }
}
