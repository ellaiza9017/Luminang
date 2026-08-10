using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class DottedOutlineUI : Graphic
{
    [Header("Dotted Line Settings")]
    public float dashSize = 10f;
    public float dashSpacing = 10f;
    public float thickness = 4f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        
        // Top edge
        DrawDashedLine(vh, new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMax, rect.yMax));
        // Right edge
        DrawDashedLine(vh, new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMin));
        // Bottom edge
        DrawDashedLine(vh, new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMin, rect.yMin));
        // Left edge
        DrawDashedLine(vh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax));
    }

    private void DrawDashedLine(VertexHelper vh, Vector2 start, Vector2 end)
    {
        float length = Vector2.Distance(start, end);
        Vector2 dir = (end - start).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);
        
        int dashCount = Mathf.FloorToInt(length / (dashSize + dashSpacing));
        float currentDist = 0f;

        for (int i = 0; i < dashCount; i++)
        {
            Vector2 p1 = start + dir * currentDist;
            Vector2 p2 = p1 + dir * dashSize;

            AddQuad(vh, p1, p2, normal);

            currentDist += (dashSize + dashSpacing);
        }
    }

    private void AddQuad(VertexHelper vh, Vector2 start, Vector2 end, Vector2 normal)
    {
        int startIndex = vh.currentVertCount;
        Vector2 offset = normal * (thickness * 0.5f);

        UIVertex vert = UIVertex.simpleVert;
        vert.color = color; // Uses the Color property from the base Graphic class

        vert.position = start - offset;
        vh.AddVert(vert);

        vert.position = start + offset;
        vh.AddVert(vert);

        vert.position = end + offset;
        vh.AddVert(vert);

        vert.position = end - offset;
        vh.AddVert(vert);

        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
}
