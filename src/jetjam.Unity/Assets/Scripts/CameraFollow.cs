using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 10f;
    public float height = 4f;
    public float extraHeight = 0f;   // カメラ位置の追加オフセット
    public float lookUpOffset = 2f;  // 見上げオフセット
    public float smooth = 5f;

    void LateUpdate()
    {
        if (!target) return;

        // カメラの位置
        Vector3 behindPos =
            target.position
            - target.forward * distance
            + Vector3.up * (height + extraHeight);

        transform.position = Vector3.Lerp(
            transform.position,
            behindPos,
            Time.deltaTime * smooth
        );

        // ----- ターゲットを見る -----
        Vector3 lookTarget =
            target.position + Vector3.up * lookUpOffset;

        Quaternion look = Quaternion.LookRotation(lookTarget - transform.position);

        // Rollは固定して水平にする
        Quaternion level = Quaternion.Euler(look.eulerAngles.x, look.eulerAngles.y, 0);

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            level,
            Time.deltaTime * smooth
        );
    }
}
