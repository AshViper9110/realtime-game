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
    public Joystick joystick;

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

    [SerializeField] private float boostPower = 50f;
    [SerializeField] private float boostDuration = 3f;
    public int itemIndex = -1;

    private float currentSpeed;
    private float currentRoll = 0f;

    private float boostSpeed = 0f;
    private float boostTimer = 0f;

    public bool isStart = false;

    bool boostHeld = false;
    bool brakeHeld = false;
    int shotCount = 0;

    void Start()
    {
        engineAudio.volume = 0;
        currentSpeed = (maxSpeed + minSpeed) * 0.5f;
    }

    public void ResetForRace()
    {
        currentSpeed = (maxSpeed + minSpeed) * 0.5f;
        boostSpeed = 0f;
        boostTimer = 0f;
        itemIndex = -1;
        isStart = false;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void Update()
    {
        if (!isStart)
        {
            engineAudio.volume = 0;
            return; 
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            UseItem();
        }

        float inputX = GetHorizontal();
        float inputY = GetVertical();

        // --- Yaw（左右旋回） ---
        transform.Rotate(0f, inputX * yawSpeed * Time.deltaTime, 0f, Space.Self);

        // --- Pitch（上下旋回） ---
        transform.Rotate(inputY * pitchSpeed * Time.deltaTime, 0f, 0f, Space.Self);

        // --- 加速 / 減速 ---
        if (GetBoostInput())
            currentSpeed += acceleration * Time.deltaTime;

        if (GetBrakeInput())
            currentSpeed -= deceleration * Time.deltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
        image.fillAmount = currentSpeed / maxSpeed;

        // --- Boost 処理 ---
        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
            boostSpeed = boostPower;
        }
        else
        {
            boostSpeed = 0f;
        }

        // --- 前進 ---
        transform.position += transform.forward * (currentSpeed + boostSpeed) * Time.deltaTime;


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

    float GetHorizontal()
    {
        if (joystick != null)
            return joystick.Horizontal;

        return Input.GetAxis("Horizontal");
    }

    float GetVertical()
    {
        if (joystick != null)
            return joystick.Vertical;

        return Input.GetAxis("Vertical");
    }

    bool GetBoostInput()
    {
        // UI Button（押している間）
        if (boostHeld)
            return true;

        // キーボード
        return Input.GetKey(KeyCode.Space);
    }

    bool GetBrakeInput()
    {
        if (brakeHeld)
            return true;


        return Input.GetKey(KeyCode.LeftShift);
    }


    // ===== Boost =====
    public void BoostDown()
    {
        boostHeld = true;
    }

    public void BoostUp()
    {
        boostHeld = false;
    }

    // ===== Brake =====
    public void BrakeDown()
    {
        brakeHeld = true;
    }

    public void BrakeUp()
    {
        brakeHeld = false;
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

    public void UseItem()
    {
        Vector3 pos = new Vector3(transform.position.x, transform.position.y - 1, transform.position.z);
        switch (itemIndex)
        {
            case 0:
                boostTimer = boostDuration;
                gameDirector.ShotItem(pos, transform.rotation, new Vector3(5, 0, 0), 0);
                break;
            case 1:
                Debug.Log("rocket");
                gameDirector.ShotItem(pos, transform.rotation, new Vector3(5, 0, 0), 1);
                break;
            case 2:
                Debug.Log("rockets");
                shotCount++;
                if (shotCount > 2) 
                {
                    gameDirector.ShotItem(pos, transform.rotation, new Vector3(5, 0, 0), 2);
                }
                else
                {
                    gameDirector.ShotItem(pos, transform.rotation, new Vector3(5, 0, 0), 1);
                }
                break;
            case 3:
                Debug.Log("smoke");
                gameDirector.ShotItem(pos, transform.rotation, new Vector3(0, 0, 0), 3);
                break;
        }
        if (itemIndex != 2)
        {
            itemIndex = -1;
        }
        if (shotCount > 2)
        {
            itemIndex = -1;
        }
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

        if (collision.gameObject.tag == "Missile")
        {
            Debug.Log("Missile Hit");
            currentSpeed = 5;
        }
    }
}
