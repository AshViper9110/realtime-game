using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

public class RaceManager : MonoBehaviour
{
    public List<Racer> racers = new();
    public SplineContainer spline;

    void Update()
    {
        foreach (var r in racers)
        {
            r.UpdateProgress(spline);
        }

        // ここで順位ソート
        racers = racers
            .OrderByDescending(r => r.progress)
            .ToList();
    }
}

[System.Serializable]
public class Racer
{
    public Transform tf;
    public float progress;

    public void UpdateProgress(SplineContainer splineContainer)
    {
        var spline = splineContainer.Spline;
        float3 nearestPos;
        float t;

        // これが正しい最近点取得
        SplineUtility.GetNearestPoint(spline, tf.position, out nearestPos, out t);

        float length = spline.GetLength();
        progress = length * t;
    }
}
