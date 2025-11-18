
using UnityEngine;

public class PrayerCon : MonoBehaviour
{
    [Header("References")]
    public Transform planeModel;  // 見た目用オブジェクト

    [Header("Movement Settings")]
    public float yawSpeed = 60f;      // 左右旋回
    public float pitchSpeed = 45f;    // 上下
    public float acceleration = 20f;  // 加速量
    public float deceleration = 20f;  // 減速量
    public float maxSpeed = 50f;      // 最大速度
    public float minSpeed = 10f;      // 最低速度

    [Header("Visual Roll Settings")]
    public float maxRollAngle = 35f;  // 見た目の傾き
    public float rollSmooth = 5f;     // ロール追従速度

    private float currentSpeed;
    private float currentRoll = 0f;

    void Start()
    {
        currentSpeed = (maxSpeed + minSpeed) * 0.5f;
    }

    void Update()
    {
        if (!GameDirector.isJoin) return;
        float inputX = Input.GetAxis("Horizontal"); // A/D or ←→
        float inputY = Input.GetAxis("Vertical");   // W/S or ↑↓

        // --- Yaw：左右旋回 ---
        transform.Rotate(0f, inputX * yawSpeed * Time.deltaTime, 0f, Space.Self);

        // --- Pitch：上下旋回 ---
        transform.Rotate(inputY * pitchSpeed * Time.deltaTime, 0f, 0f, Space.Self);

        // --- Shift で加速 / Space で減速 ---
        if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed -= acceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.Space))
            currentSpeed += deceleration * Time.deltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // --- 前進 ---
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // --- 見た目だけロール（PlaneModel）---
        float targetRoll = -inputX * maxRollAngle;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSmooth);

        Vector3 visualEuler = planeModel.localEulerAngles;
        planeModel.localRotation = Quaternion.Euler(visualEuler.x, visualEuler.y, currentRoll);
    }
}