using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SplinePrefabPlacerTool : MonoBehaviour
{
    public enum PlacementMode
    {
        EvenSpacing,    // 等間隔配置 (Spacing指定)
        ConnectObjects  // オブジェクトを繋げる (Prefabの長さ基準)
    }

    [Header("Spline Settings")]
    public SplineContainer splineContainer;

    [Header("Prefab Settings")]
    public GameObject prefab;
    public PlacementMode placementMode = PlacementMode.EvenSpacing;

    [Header("Placement Settings")]
    public float spacing = 5f;          // EvenSpacing用
    public bool autoDetectLength = true;// ConnectObjects用: Prefabの長さを自動検出
    public float manualPrefabLength = 5f; // 手動指定の場合

    [Header("Alignment Settings")]
    public bool alignToSpline = true;   // スプラインの向きに合わせる
    public bool alignUpVector = true;   // UpVectorも合わせる
    public Vector3 rotationOffset = Vector3.zero;
    public Vector3 positionOffset = Vector3.zero;

    [Header("Pivot Adjustment (0.0 - 1.0)")]
    [Range(0f, 1f)] public float pivotPosition = 0.5f; // 0=後端, 0.5=中心, 1=前端

    [Header("Debug / Preview")]
    public bool showPreview = true;
    public Color previewColor = Color.yellow;
    public bool showConnections = true; // 連結モード時の接続確認

    // 内部計算用
    private struct Placement
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool isValid;
    }

    private List<Placement> placements = new List<Placement>();

    private void OnValidate()
    {
        // インスペクター変更時に再計算
        if (showPreview) CalculatePlacements();
    }

    [ContextMenu("Place Prefabs")]
    public void PlacePrefabs()
    {
        if (splineContainer == null || prefab == null)
        {
            Debug.LogError("SplineContainer または Prefab が設定されていません。");
            return;
        }

        CalculatePlacements();

        // 既存の子オブジェクトを削除（オプション）
        ClearChildren();

        // 配置実行
        foreach (var p in placements)
        {
            if (!p.isValid) continue;

            GameObject obj;
#if UNITY_EDITOR
            if (Application.isPlaying)
                obj = Instantiate(prefab, transform);
            else
                obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
#else
            obj = Instantiate(prefab, transform);
#endif
            obj.transform.position = p.position;
            obj.transform.rotation = p.rotation;
            obj.transform.localScale = p.scale;
        }

        Debug.Log($"配置完了: {placements.Count} 個のオブジェクトを生成しました。");
    }

    private void CalculatePlacements()
    {
        placements.Clear();

        if (splineContainer == null) return;

        foreach (var spline in splineContainer.Splines)
        {
            CalculateSplinePlacement(spline);
        }
    }

    private void CalculateSplinePlacement(Spline spline)
    {
        float splineLength = spline.GetLength();
        if (splineLength <= 0.001f) return;

        float currentDistance = 0f;

        // モード別計算
        if (placementMode == PlacementMode.EvenSpacing)
        {
            // --- 等間隔モード ---
            if (spacing <= 0.001f) spacing = 1f;

            while (currentDistance <= splineLength)
            {
                Vector3 pos = CalculatePosition(spline, currentDistance, 0f);
                Quaternion rot = CalculateRotation(spline, currentDistance / splineLength);

                placements.Add(new Placement
                {
                    position = pos,
                    rotation = rot,
                    scale = Vector3.one,
                    isValid = true
                });

                currentDistance += spacing;
            }
        }
        else
        {
            // --- 連結モード ---
            float length = autoDetectLength ? GetPrefabLength() : manualPrefabLength;
            if (length <= 0.001f) length = 1f;

            // 最初のオフセット（ピボット考慮）
            currentDistance = length * pivotPosition;

            while (currentDistance <= splineLength)
            {
                // 現在位置の計算
                float t = currentDistance / splineLength;
                Vector3 pos = CalculatePosition(spline, currentDistance, length);
                Quaternion rot = CalculateRotation(spline, t);

                // カーブによる隙間補正（簡易版）
                // カーブがきつい場合、少し詰めるなどの処理を入れる余地あり
                // ここでは単純配置

                placements.Add(new Placement
                {
                    position = pos,
                    rotation = rot,
                    scale = Vector3.one,
                    isValid = true
                });

                // 次の位置へ
                float nextSpacing = length;
                
                // オプション：カーブに応じて間隔を微調整するならここで計算
                // float curveAngle = CalculateCurveAngle(spline, t, 0.05f);
                // if (curveAngle > 10f) nextSpacing *= 0.95f; // 例

                currentDistance += nextSpacing;

                // 安全弁
                if (placements.Count > 10000)
                {
                    Debug.LogError("∞ 配置ループ検出");
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
                Debug.Log($"✓ 有効な配置: {validCount}/{placements.Count}");
            }
        }
    }

    private bool showDebugInfo = false;

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

        // Renderer検索
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

        // Collider検索
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

        // Zの長さを返す
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

            // 無効配置は赤で表示
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

                // 次との接続
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
                "ℹ 等間隔配置モード\n" +
                "Spacingで間隔を指定します",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "ℹ 連結配置モード\n" +
                "Prefabを繋なるように配置します\n" +
                "Connection Overlapで調整",
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