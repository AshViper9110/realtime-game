using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    [SerializeField]
    int index;

    public Vector3 forward;
    public CheckPoint nextCheckPoint;

    static List<CheckPoint> list = new List<CheckPoint>();

    static public CheckPoint StartPoint => list[0];

    //自分自身をlistにつなげていきます
    void Awake()
    {
        list.Add(this);
    }

    static public void SetForward()
    {
        CheckPoint prev = null;
        list.Sort((a, b) => a.index - b.index);

        foreach (var cp in list)
        {
            if (prev != null)
            {
                //次のチェックポイントへの向き（＝コースの進行方向）
                //一個前のチェックポイントにこのチェックポイントへの向きを入れる。
                prev.nextCheckPoint = cp;
                prev.forward = (cp.transform.position - prev.transform.position).normalized;
            }

            prev = cp;
        }

        //最後の一個をスタート地点に向かせる
        list[list.Count - 1].forward = (list[0].transform.position - list[list.Count - 1].transform.position).normalized;

        //スタート地点につなぐ
        list[list.Count - 1].nextCheckPoint = list[0];
    }

    public float GetProgress(Vector3 p)
    {
        var v = p - transform.position;
        return Vector3.Dot(forward, v);
    }


    //通過判定
    //次のチェックポイントをP、1つ前のフレームの座標をA、今回のフレームの座標をBとすると
    //次のチェックポイントの平面を突き抜けてれば通過している。
    //その平面の法線はつまり進行方向なので、
    //進行方向とPAの内積、進行方向とPBの内積　の符号が違ったら通過している。
    public bool CheckIfPassed(Vector3 p0, Vector3 p1)
    {
        return Vector3.Dot(nextCheckPoint.forward, p0 - nextCheckPoint.transform.position) * Vector3.Dot(nextCheckPoint.forward, p1 - nextCheckPoint.transform.position) < 0 ? true : false;
    }

    //逆走チェック
    //いまのチェックポイントの正しい進行方向に対して90度以上角度があったら逆走しているとみなす。
    public bool CheckWrongWay(Vector3 p0, Vector3 p1)
    {
        return Vector3.Dot(forward, p1 - p0) < 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2.0f);
    }
}