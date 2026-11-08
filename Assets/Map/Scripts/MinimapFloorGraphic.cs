using UnityEngine;
using UnityEngine.UI;

// NavMesh 삼각형 중 밝혀진 것만 UI로 그림
[RequireComponent(typeof(CanvasRenderer))]
public class MinimapFloorGraphic : MaskableGraphic
{
    Vector3[] verts;
    int[] tris;
    bool[] revealed;
    float ppm = 1f;
    Color32 col = Color.white;

    public void SetMesh(Vector3[] v, int[] t, bool[] r, float pixelsPerMeter = 1f, Color c = default)
    {
        verts = v; tris = t; revealed = r; ppm = pixelsPerMeter; col = c;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (verts == null || tris == null || revealed == null) return;
        int count = 0;
        for (int t = 0; t < revealed.Length; t++)
        {
            if (!revealed[t]) continue;
            if (count + 3 > 64000) break; // UI 메쉬 정점 한도
            for (int k = 0; k < 3; k++)
            {
                Vector3 w = verts[tris[t * 3 + k]];
                vh.AddVert(new Vector3(w.x * ppm, w.z * ppm, 0f), col, Vector4.zero);
            }
            vh.AddTriangle(count, count + 1, count + 2);
            count += 3;
        }
    }
}
