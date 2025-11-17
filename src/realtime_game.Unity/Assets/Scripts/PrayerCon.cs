using UnityEngine;

public class PrayerCon : MonoBehaviour
{
    [Header("Speed")]
        public float baseSpeed = 30f;        // 常時前進速度
    public float boostSpeed = 15f;       // Space で加速
    public float slowSpeed = 10f;        // Shift で減速
    float currentSpeed;

    [Header("Control")]
    public float pitchPower = 40f;       // 機首の上下
    public float rollPower = 60f;        // 左右の傾き
    public float autoLevel = 2f;         // 自動水平補正

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;   // レースなので重力 OFF（安定）
        rb.linearDamping = 1f;
        currentSpeed = baseSpeed;
    }

    void Update()
    {
        HandleSpeedControl();
        HandleRotation();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = transform.forward * currentSpeed;   // 常時前進
    }

    // -----------------------------
    // Speed Control (Space / Shift)
    // -----------------------------
    void HandleSpeedControl()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed + boostSpeed, 5f * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed - slowSpeed, 5f * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed, 2f * Time.deltaTime);
        }
    }

    // -----------------------------
    // Rotation (WASD)
    // -----------------------------
    void HandleRotation()
    {
        float pitch = 0f;
        if (Input.GetKey(KeyCode.W)) pitch = 1f;
        if (Input.GetKey(KeyCode.S)) pitch = -1f;

        float roll = 0f;
        if (Input.GetKey(KeyCode.A)) roll = 1f;
        if (Input.GetKey(KeyCode.D)) roll = -1f;

        // Pitch : 上下
        transform.Rotate(pitch * pitchPower * Time.deltaTime, 0f, 0f, Space.Self);

        // Roll : 左右傾き
        transform.Rotate(0f, 0f, roll * rollPower * Time.deltaTime, Space.Self);
    }
}