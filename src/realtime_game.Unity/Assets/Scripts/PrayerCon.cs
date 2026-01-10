using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

public class PrayerCon : MonoBehaviour
{
    [Header("References")]
    public Transform planeModel;  // 見た目用オブジェクト
    public Image image;
    public LayerMask targetLayer;
    public LayerMask goalLayer;
    public GameDirector gameDirector;

    [Header("Movement Settings")]
    public float yawSpeed = 60f;      // 左右旋回
    public float pitchSpeed = 45f;    // 上下
    public float acceleration = 20f;  // 加速量
    public float deceleration = 20f;  // 減速量
    public float maxSpeed = 30f;      // 最大速度
    public float minSpeed = 1f;      // 最低速度

    [Header("Visual Roll Settings")]
    public float maxRollAngle = 35f;  // 見た目の傾き
    public float rollSmooth = 5f;     // ロール追従速度

    [Header("Spline")]
    public SplineContainer respawnSpline;

    [Header("Audio")]
    public AudioSource engineAudio;

    [Header("Audio Settings")]
    public float minVolume = 0.2f;
    public float maxVolume = 1.0f;

    private float currentSpeed;
    private float currentRoll = 0f;

    void Start()
    {
        engineAudio.volume = 0;
        currentSpeed = (maxSpeed + minSpeed) * 0.5f;
    }

    void Update()
    {
        if (!GameDirector.isStart) return;

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
        image.fillAmount = currentSpeed / maxSpeed;

        // --- 前進 ---
        transform.position += transform.forward * currentSpeed * Time.deltaTime;


        // --- 見た目だけロール（Model） ---
        float targetRoll = -inputX * maxRollAngle;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSmooth);
        planeModel.localRotation = Quaternion.Euler(0, 0, currentRoll);

        // ===========================================
        // ※ここで本体の Z軸回転を強制的に 0 に保つ
        // ===========================================

        float speed01 = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
        engineAudio.volume = Mathf.Lerp(minVolume, maxVolume, speed01);

        Vector3 e = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(e.x, e.y, 0f);
    }

    Vector3 GetNearestPointOnSpline()
    {
        float nearestT = 0f;
        float minDist = float.MaxValue;

        // Splineを0〜1でサンプリング
        for (int i = 0; i <= 1000; i++)
        {
            float t = i / 1000f;
            Vector3 p = respawnSpline.EvaluatePosition(t);
            float d = Vector3.SqrMagnitude(transform.position - p);

            if (d < minDist)
            {
                minDist = d;
                nearestT = t;
            }
        }

        return respawnSpline.EvaluatePosition(nearestT);
    }


    private void OnCollisionEnter(Collision collision)
    {
        // 衝突相手の layer が targetLayer に含まれているか判定
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            currentSpeed = 5;

            Vector3 warpPos = GetNearestPointOnSpline();
            transform.position = warpPos;

            Debug.Log("Spline にワープ");
        }

        if (collision.gameObject.tag == "Finish")
        {
            Debug.Log("ゴール");
            gameDirector.SendGoal();
        }
    }
}
