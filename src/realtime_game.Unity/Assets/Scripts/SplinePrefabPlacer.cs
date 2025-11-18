using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SplinePrefabPlacerTool : MonoBehaviour
{
    [Header("Spline Settings")]
    public SplineContainer splineContainer;

    [Header("Prefab Settings")]
    public GameObject prefab;

    [Header("Placement Method")]
    [Tooltip("配置方法の選択")]
    public PlacementMode placementMode = PlacementMode.EvenSpacing;

    [Header("Even Spacing Settings")]
    [Tooltip("Prefab間の間隔")]
    [Range(0.1f, 50f)]
    public float spacing = 5f;

    [Header("Connection Settings")]
    [Tooltip("Prefabの長さを自動検出")]
    public bool autoDetectLength = true;

    [Tooltip("Prefabの長さ（手動設定）")]
    [Range(0.1f, 50f)]
    public float manualPrefabLength = 5f;

    [Tooltip("接続のオーバーラップ量")]
    [Range(-2f, 2f)]
    public float connectionOverlap = 0.1f;

    [Header("Transform Settings")]
    [Tooltip("Prefabのピボット位置（0=後端, 0.5=中央, 1=前端）")]
    [Range(0f, 1f)]
    public float pivotPosition = 0.5f;

    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    public Vector3 scale = Vector3.one;

    [Header("Alignment")]
    public bool alignToSpline = true;
    public bool alignUpVector = true;

    [Header("Adaptive Placement")]
    [Tooltip("カーブで配置を調整")]
    public bool adaptivePlacement = false;

    [Tooltip("カーブでの縮小率")]
    [Range(0.3f, 1f)]
    public float curveScaleMin = 0.7f;

    [Tooltip("カーブ検出感度")]
    [Range(1f, 45f)]
    public float curveSensitivity = 15f;

    [Header("Collision Prevention")]
    [Tooltip("重なりを防止")]
    public bool preventOverlap = true;

    [Tooltip("最小配置間隔")]
    [Range(0.1f, 10f)]
    public float minimumDistance = 1f;

    [Tooltip("角度差チェック")]
    public bool checkAngleDifference = false;

    [Tooltip("最大角度差")]
    [Range(5f, 90f)]
    public float maxAngleDifference = 30f;

    [Header("Preview")]
    public bool showPreview = true;
    public bool showConnections = true;
    public bool showDebugInfo = false;
    public Color previewColor = Color.green;

    private List<PlacementInfo> placements = new List<PlacementInfo>();

    public enum PlacementMode
    {
        EvenSpacing,      // 等間隔配置
        Connected         // 接続配置（隙間なし）
    }

    private struct PlacementInfo
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float distanceOnSpline;
        public bool isValid;
    }

    [ContextMenu("Place Prefabs")]
    public void PlacePrefabs()
    {
        if (!Validate()) return;

        ClearChildren();
        CalculatePlacements();

        int count = 0;
        foreach (var placement in placements)
        {
            if (!placement.isValid) continue;

            GameObject instance = Instantiate(prefab, placement.position, placement.rotation, transform);
            instance.transform.localScale = placement.scale;
            instance.name = $"{prefab.name}_{count:D3}";
            count++;
        }

        Debug.Log($"✓ {count}個のPrefabを配置しました");
    }

    private bool Validate()
    {
        if (splineContainer == null)
        {
            Debug.LogError("❌ SplineContainerが設定されていません");
            return false;
        }

        if (prefab == null)
        {
            Debug.LogError("❌ Prefabが設定されていません");
            return false;
        }

        return true;
    }

    private void CalculatePlacements()
    {
        placements.Clear();

        Spline spline = splineContainer.Spline;
        float splineLength = spline.GetLength();

        if (splineLength <= 0) return;

        float prefabLength = autoDetectLength ? GetPrefabLength() : manualPrefabLength;
        float actualSpacing = placementMode == PlacementMode.Connected
            ? prefabLength - connectionOverlap
            : spacing;

        float currentDistance = 0f;
        PlacementInfo lastPlacement = default;
        bool hasLast = false;

        while (currentDistance <= splineLength)
        {
            float t = Mathf.Clamp01(currentDistance / splineLength);

            // カーブ計算
            float curveAngle = CalculateCurveAngle(spline, t, prefabLength / splineLength);

            // スケール計算
            Vector3 currentScale = scale;
            if (adaptivePlacement && curveAngle > curveSensitivity)
            {
                float curveRatio = Mathf.Clamp01((curveAngle - curveSensitivity) / 30f);
                float scaleZ = Mathf.Lerp(1f, curveScaleMin, curveRatio);
                currentScale.z *= scaleZ;
            }

            // 位置と回転
            Vector3 position = CalculatePosition(spline, currentDistance, prefabLength);
            Quaternion rotation = CalculateRotation(spline, t);

            // 妥当性チェック
            bool isValid = true;

            if (hasLast && preventOverlap)
            {
                float dist = Vector3.Distance(position, lastPlacement.position);
                if (dist < minimumDistance)
                {
                    isValid = false;
                }
            }

            if (hasLast && checkAngleDifference && isValid)
            {
                float angleDiff = Quaternion.Angle(rotation, lastPlacement.rotation);
                if (angleDiff > maxAngleDifference)
                {
                    isValid = false;
                    if (showDebugInfo)
                    {
                        Debug.LogWarning($"⚠ 角度差: {angleDiff:F1}° at {currentDistance:F2}m");
                    }
                }
            }

            var placement = new PlacementInfo
            {
                position = position,
                rotation = rotation,
                scale = currentScale,
                distanceOnSpline = currentDistance,
                isValid = isValid
            };

            placements.Add(placement);

            if (isValid)
            {
                lastPlacement = placement;
                hasLast = true;
            }

            // 次の位置
            float nextSpacing = actualSpacing;
            if (adaptivePlacement && curveAngle > curveSensitivity)
            {
                nextSpacing *= currentScale.z;
            }

            currentDistance += nextSpacing;

            // 安全装置
            if (placements.Count > 10000)
            {
                Debug.LogError("❌ 配置数が多すぎます");
                break;
            }
        }

        if (showDebugInfo)
        {
            int validCount = 0;
            foreach (var p in placements)
            {
                if (p.isValid) validCount++;
            }
            Debug.Log($"📊 有効な配置: {validCount}/{placements.Count}");
        }
    }

    private Vector3 CalculatePosition(Spline spline, float distance, float prefabLength)
    {
        float splineLength = spline.GetLength();

        // ピボットオフセット
        float pivotOffset = (pivotPosition - 0.5f) * prefabLength;
        float adjustedDistance = Mathf.Clamp(distance - pivotOffset, 0f, splineLength);

        float t = adjustedDistance / splineLength;
        float3 posFloat3 = spline.EvaluatePosition(t);
        Vector3 pos = new Vector3(posFloat3.x, posFloat3.y, posFloat3.z);

        // オフセット適用
        if (positionOffset != Vector3.zero)
        {
            Quaternion rotation = CalculateRotation(spline, t);
            pos += rotation * positionOffset;
        }

        return pos;
    }

    private Quaternion CalculateRotation(Spline spline, float t)
    {
        if (!alignToSpline)
        {
            return Quaternion.Euler(rotationOffset);
        }

        float3 tangentFloat3 = spline.EvaluateTangent(t);
        Vector3 tangent = new Vector3(tangentFloat3.x, tangentFloat3.y, tangentFloat3.z);

        if (tangent.sqrMagnitude < 0.0001f)
        {
            return Quaternion.Euler(rotationOffset);
        }

        tangent.Normalize();
        Quaternion rotation;

        if (alignUpVector)
        {
            float3 upFloat3 = spline.EvaluateUpVector(t);
            Vector3 up = new Vector3(upFloat3.x, upFloat3.y, upFloat3.z);

            if (up.sqrMagnitude > 0.0001f)
            {
                up.Normalize();
                rotation = Quaternion.LookRotation(tangent, up);
            }
            else
            {
                rotation = Quaternion.LookRotation(tangent);
            }
        }
        else
        {
            rotation = Quaternion.LookRotation(tangent);
        }

        return rotation * Quaternion.Euler(rotationOffset);
    }

    private float CalculateCurveAngle(Spline spline, float t, float span)
    {
        float t1 = Mathf.Clamp01(t);
        float t2 = Mathf.Clamp01(t + span);

        float3 tangent1 = spline.EvaluateTangent(t1);
        float3 tangent2 = spline.EvaluateTangent(t2);

        Vector3 v1 = new Vector3(tangent1.x, tangent1.y, tangent1.z).normalized;
        Vector3 v2 = new Vector3(tangent2.x, tangent2.y, tangent2.z).normalized;

        return Vector3.Angle(v1, v2);
    }

    private float GetPrefabLength()
    {
        if (prefab == null) return 5f;

        Bounds bounds = new Bounds();
        bool hasBounds = false;

        // Rendererから
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        // Colliderから
        Collider[] colliders = prefab.GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
        {
            if (!hasBounds)
            {
                bounds = c.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }

        if (!hasBounds) return 5f;

        // Z軸の長さを返す
        return Mathf.Max(bounds.size.z, 1f);
    }

    [ContextMenu("Clear All")]
    public void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showPreview || splineContainer == null || prefab == null) return;

        CalculatePlacements();

        float prefabLength = autoDetectLength ? GetPrefabLength() : manualPrefabLength;

        for (int i = 0; i < placements.Count; i++)
        {
            var p = placements[i];

            // 無効な配置は赤で表示
            if (!p.isValid)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(p.position, 0.3f);
                continue;
            }

            // 位置マーカー
            Gizmos.color = previewColor;
            Gizmos.DrawWireSphere(p.position, 0.2f);

            // 向き
            Vector3 forward = p.rotation * Vector3.forward;
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(p.position, forward * 1f);

            // Prefabの範囲
            if (showConnections)
            {
                float actualLength = prefabLength * p.scale.z;
                Vector3 start = p.position - forward * (actualLength * pivotPosition);
                Vector3 end = p.position + forward * (actualLength * (1f - pivotPosition));

                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                Gizmos.DrawLine(start, end);

                // 接続点
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(start, 0.15f);
                Gizmos.DrawWireSphere(end, 0.15f);

                // 次との接続線
                if (i < placements.Count - 1)
                {
                    var next = placements[i + 1];
                    if (next.isValid)
                    {
                        Vector3 nextForward = next.rotation * Vector3.forward;
                        float nextLength = prefabLength * next.scale.z;
                        Vector3 nextStart = next.position - nextForward * (nextLength * pivotPosition);

                        float gap = Vector3.Distance(end, nextStart);

                        if (gap < 0.2f)
                            Gizmos.color = Color.green;
                        else if (gap < 1f)
                            Gizmos.color = Color.yellow;
                        else
                            Gizmos.color = Color.red;

                        Gizmos.DrawLine(end, nextStart);
                    }
                }
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SplinePrefabPlacerTool))]
public class SplinePrefabPlacerToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SplinePrefabPlacerTool tool = (SplinePrefabPlacerTool)target;

        EditorGUILayout.Space();

        // モード別のヒント
        if (tool.placementMode == SplinePrefabPlacerTool.PlacementMode.EvenSpacing)
        {
            EditorGUILayout.HelpBox(
                "📏 等間隔配置モード\n" +
                "Spacingで間隔を指定します",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "🔗 接続配置モード\n" +
                "Prefabを隙間なく繋げます\n" +
                "Connection Overlapで微調整",
                MessageType.Info);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Place Prefabs Along Spline", GUILayout.Height(35)))
        {
            tool.PlacePrefabs();
        }

        if (GUILayout.Button("Clear All", GUILayout.Height(25)))
        {
            tool.ClearChildren();
        }
    }
}
#endif