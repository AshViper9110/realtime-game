using UnityEngine;
using UnityEngine.UI;

public class PrayerCon : MonoBehaviour
{
    [Header("References")]
    public Transform planeModel;  // 見た目用オブジェクト
    public Image image;
    public LayerMask targetLayer;

    [Header("Movement Settings")]
    public float yawSpeed = 60f;      // 左右旋回
    public float pitchSpeed = 45f;    // 上下
    public float acceleration = 20f;  // 加速量
    public float deceleration = 20f;  // 減速量
    public float maxSpeed = 40f;      // 最大速度
    public float minSpeed = 1f;      // 最低速度

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

        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");

        // --- Yaw（左右旋回） ---
        transform.Rotate(0f, inputX * yawSpeed * Time.deltaTime, 0f, Space.Self);

        // --- Pitch（上下旋回） ---
        transform.Rotate(inputY * pitchSpeed * Time.deltaTime, 0f, 0f, Space.Self);

        // --- Shift で加速 / Space で減速 ---
        if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed -= acceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.Space))
            currentSpeed += deceleration * Time.deltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
        image.fillAmount = currentSpeed / 40f;

        // --- 前進 ---
        transform.position += transform.forward * currentSpeed * Time.deltaTime;


        // --- 見た目だけロール（Model） ---
        float targetRoll = -inputX * maxRollAngle;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSmooth);
        planeModel.localRotation = Quaternion.Euler(0, 0, currentRoll);

        // ===========================================
        // ※ここで本体の Z軸回転を強制的に 0 に保つ
        // ===========================================
        Vector3 e = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(e.x, e.y, 0f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 衝突相手の layer が targetLayer に含まれているか判定
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            currentSpeed = 5;
            Debug.Log("特定レイヤーと衝突した！");
        }
    }
}
