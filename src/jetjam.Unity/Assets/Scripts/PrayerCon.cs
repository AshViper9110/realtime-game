using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

public class PrayerCon : MonoBehaviour
{
    [Header("References")]
    public Transform planeModel;
    public Image image;
    public LayerMask targetLayer;
    public LayerMask goalLayer;
    public GameDirector gameDirector;
    public Joystick joystick;

    [Header("Movement Settings")]
    public float yawSpeed = 60f;
    public float pitchSpeed = 45f;
    public float acceleration = 20f;
    public float deceleration = 20f;
    public float maxSpeed = 30f;
    public float minSpeed = 1f;

    [Header("Visual Roll")]
    public float maxRollAngle = 35f;
    public float rollSmooth = 5f;

    [Header("Spline")]
    public SplineContainer respawnSpline;

    [Header("Audio")]
    public AudioSource engineAudio;
    public float minVolume = 0.2f;
    public float maxVolume = 1.0f;

    [Header("Boost")]
    [SerializeField] private float boostPower = 50f;
    [SerializeField] private float boostDuration = 3f;

    public int itemIndex = -1;
    public bool isStart = false;

    float currentSpeed;
    float currentRoll;
    float boostSpeed;
    float boostTimer;

    bool boostHeld;
    bool brakeHeld;
    int shotCount;

    // =============================
    // Unity Lifecycle
    // =============================

    void Start()
    {
        engineAudio.volume = 0f;
        currentSpeed = (maxSpeed + minSpeed) * 0.5f;
    }

    void Update()
    {
        if (!isStart)
        {
            engineAudio.volume = 0f;
            return;
        }

        if (IsUseItemPressed())
            UseItem();

        float inputX = GetMoveX();
        float inputY = GetMoveY();

        // Rotation
        transform.Rotate(0f, inputX * yawSpeed * Time.deltaTime, 0f, Space.Self);
        transform.Rotate(inputY * pitchSpeed * Time.deltaTime, 0f, 0f, Space.Self);

        // Speed
        if (IsBoost())
            currentSpeed += acceleration * Time.deltaTime;

        if (IsBrake())
            currentSpeed -= deceleration * Time.deltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
        image.fillAmount = currentSpeed / maxSpeed;

        // Boost
        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
            boostSpeed = boostPower;
        }
        else
        {
            boostSpeed = 0f;
        }

        // Move
        transform.position += transform.forward * (currentSpeed + boostSpeed) * Time.deltaTime;

        // Visual Roll
        float targetRoll = -inputX * maxRollAngle;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSmooth);
        planeModel.localRotation = Quaternion.Euler(0, 0, currentRoll);

        // Z固定
        Vector3 e = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(e.x, e.y, 0f);

        // Audio
        float speed01 = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
        engineAudio.volume = Mathf.Lerp(minVolume, maxVolume, speed01);
    }

    // =============================
    // Input (Unified)
    // =============================

    float GetMoveX()
    {
        float pc = Input.GetAxis("Horizontal");
        float touch = joystick != null ? joystick.Horizontal : 0f;
        return Mathf.Abs(touch) > Mathf.Abs(pc) ? touch : pc;
    }

    float GetMoveY()
    {
        float pc = Input.GetAxis("Vertical");
        float touch = joystick != null ? joystick.Vertical : 0f;
        return Mathf.Abs(touch) > Mathf.Abs(pc) ? touch : pc;
    }

    bool IsBoost()
    {
        return boostHeld || Input.GetKey(KeyCode.Space);
    }

    bool IsBrake()
    {
        return brakeHeld || Input.GetKey(KeyCode.LeftShift);
    }

    bool IsUseItemPressed()
    {
        return Input.GetKeyDown(KeyCode.E);
    }

    // =============================
    // UI Button Callbacks
    // =============================

    public void BoostDown() => boostHeld = true;
    public void BoostUp() => boostHeld = false;

    public void BrakeDown() => brakeHeld = true;
    public void BrakeUp() => brakeHeld = false;

    public void UseItemButton() => UseItem();

    // =============================
    // Item
    // =============================

    public void UseItem()
    {
        if (itemIndex < 0) return;

        Vector3 pos = transform.position + Vector3.down;

        switch (itemIndex)
        {
            case 0:
                boostTimer = boostDuration;
                gameDirector.ShotItem(pos, transform.rotation, Vector3.right * 5, 0);
                break;

            case 1:
                gameDirector.ShotItem(pos, transform.rotation, Vector3.right * 5, 1);
                break;

            case 2:
                shotCount++;
                int type = shotCount > 2 ? 2 : 1;
                gameDirector.ShotItem(pos, transform.rotation, Vector3.right * 5, type);
                if (shotCount > 2) itemIndex = -1;
                return;

            case 3:
                gameDirector.ShotItem(pos, transform.rotation, Vector3.zero, 3);
                break;
        }

        itemIndex = -1;
    }

    // =============================
    // Collision
    // =============================

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            currentSpeed = 5f;
            transform.position = GetNearestPointOnSpline();
        }

        if (collision.gameObject.tag == "Finish")
            gameDirector.SendGoal();

        if (collision.gameObject.tag == "Missile")
            currentSpeed = 5f;
    }

    // =============================
    // Utility
    // =============================

    Vector3 GetNearestPointOnSpline()
    {
        float nearestT = 0f;
        float minDist = float.MaxValue;

        for (int i = 0; i <= 1000; i++)
        {
            float t = i / 1000f;
            Vector3 p = respawnSpline.EvaluatePosition(t);
            float d = (transform.position - p).sqrMagnitude;

            if (d < minDist)
            {
                minDist = d;
                nearestT = t;
            }
        }
        return respawnSpline.EvaluatePosition(nearestT);
    }

    public void ResetForRace()
    {
        currentSpeed = (maxSpeed + minSpeed) * 0.5f;
        boostSpeed = 0f;
        boostTimer = 0f;
        itemIndex = -1;
        shotCount = 0;
        isStart = false;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
