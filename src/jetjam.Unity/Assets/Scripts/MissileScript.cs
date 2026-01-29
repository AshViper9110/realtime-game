using UnityEngine;

public class MissileScript : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] string targetTag = "Enemy";
    public LayerMask targetLayer;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        rb.linearVelocity = transform.forward * speed;

        // Åö 5ïbå„Ç…çÌèú
        Destroy(gameObject, 5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag) ||
            ((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}
