using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CanyonGenerator : MonoBehaviour
{
    public Terrain terrain;
    public SplineContainer spline;

    [Range(5f, 200f)] public float canyonWidth = 50f;
    [Range(1f, 200f)] public float canyonDepth = 20f;
    [Range(0.1f, 10f)] public float step = 1f;

#if UNITY_EDITOR
    [ContextMenu("Generate Canyon")]
    public void Generate()
    {
        if (terrain == null || spline == null)
        {
            Debug.LogError("Terrain Ç‹ÇΩÇÕ Spline Ç™ê›íËÇ≥ÇÍÇƒÇ¢Ç‹ÇπÇÒÅB");
            return;
        }

        TerrainData data = terrain.terrainData;

        int resW = data.heightmapResolution;
        int resH = data.heightmapResolution;

        float[,] heights = data.GetHeights(0, 0, resW, resH);

        Spline s = spline.Spline;
        float4x4 localToWorld = (float4x4)spline.transform.localToWorldMatrix;

        float splineLength = SplineUtility.CalculateLength(s, localToWorld);

        for (float dist = 0; dist <= splineLength; dist += step)
        {
            float t = dist / splineLength;

            float3 pos = SplineUtility.EvaluatePosition(s, t);
            float3 tangent = SplineUtility.EvaluateTangent(s, t);

            float3 side = math.normalize(new float3(-tangent.z, 0, tangent.x));

            ModifyTerrain(heights, data, pos, side);
        }

        data.SetHeights(0, 0, heights);

        Debug.Log("Canyon Generated (Unity 6)");
    }
#endif

    private void ModifyTerrain(float[,] heights, TerrainData data, float3 center, float3 side)
    {
        int w = data.heightmapResolution;
        int h = data.heightmapResolution;

        Vector3 terrainPos = (Vector3)center - terrain.transform.position;

        float nx = terrainPos.x / data.size.x;
        float nz = terrainPos.z / data.size.z;

        int hx = Mathf.RoundToInt(nx * (w - 1));
        int hz = Mathf.RoundToInt(nz * (h - 1));

        int radius = Mathf.RoundToInt((canyonWidth / data.size.x) * w);

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                int tx = hx + x;
                int tz = hz + z;

                if (tx < 0 || tz < 0 || tx >= w || tz >= h) continue;

                float dist = Mathf.Abs(x) / (float)radius;

                float depth = Mathf.Lerp(canyonDepth, 0, dist);

                heights[tz, tx] -= depth / data.size.y;
            }
        }
    }
}
