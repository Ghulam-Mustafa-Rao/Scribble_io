/*using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class UILine : Graphic
{
    [Header("Brush Settings")]
    public float thickness = 10f;
    [Range(3, 12)]
    public int circleSegments = 6;
    public int maxPoints = 500;

    [HideInInspector]
    public List<Vector2> points = new List<Vector2>();

    public void AddPoint(Vector2 newPoint)
    {
        if (points.Count >= maxPoints) return;

        if (points.Count == 0 || Vector2.Distance(points[points.Count - 1], newPoint) > 0.01f)
        {
            points.Add(newPoint);
            SetVerticesDirty();
        }
    }

    public void UpdateLine()
    {
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points.Count < 2) return;

        float radius = thickness / 2f;
        float maxDistance = thickness * 1.5f;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 center = points[i];
            DrawCircle(vh, center, radius);

            if (i == 0) continue;
            Vector2 prev = points[i - 1];

            if (Vector2.Distance(prev, center) <= maxDistance)
            {
                DrawQuadBetween(vh, prev, center, radius);
            }
        }
    }

    void DrawCircle(VertexHelper vh, Vector2 center, float radius)
    {
        for (int j = 0; j < circleSegments; j++)
        {
            float angle1 = (j / (float)circleSegments) * Mathf.PI * 2f;
            float angle2 = ((j + 1) / (float)circleSegments) * Mathf.PI * 2f;

            Vector2 p1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
            Vector2 p2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;

            UIVertex v0 = UIVertex.simpleVert; v0.position = center; v0.color = color;
            UIVertex v1 = UIVertex.simpleVert; v1.position = p1; v1.color = color;
            UIVertex v2 = UIVertex.simpleVert; v2.position = p2; v2.color = color;

            int idx = vh.currentVertCount;
            vh.AddVert(v0); vh.AddVert(v1); vh.AddVert(v2);
            vh.AddTriangle(idx, idx + 1, idx + 2);
        }
    }

    void DrawQuadBetween(VertexHelper vh, Vector2 p1, Vector2 p2, float halfWidth)
    {
        Vector2 dir = (p2 - p1).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * halfWidth;

        UIVertex v1 = UIVertex.simpleVert; v1.position = p1 + normal; v1.color = color;
        UIVertex v2 = UIVertex.simpleVert; v2.position = p1 - normal; v2.color = color;
        UIVertex v3 = UIVertex.simpleVert; v3.position = p2 - normal; v3.color = color;
        UIVertex v4 = UIVertex.simpleVert; v4.position = p2 + normal; v4.color = color;

        int idx = vh.currentVertCount;
        vh.AddVert(v1); vh.AddVert(v2); vh.AddVert(v3); vh.AddVert(v4);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }
}
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class UILine : Graphic
{
    public float thickness = 10f;
    [Range(3, 12)]
    public int circleSegments = 6;
    public int maxPoints = 500;
    public int lineID;

    [HideInInspector]
    public List<Vector2> points = new List<Vector2>();

    public void AddPoint(Vector2 newPoint)
    {
        if (points.Count >= maxPoints) return;

        if (points.Count == 0 || Vector2.Distance(points[points.Count - 1], newPoint) > 0.01f)
        {
            points.Add(newPoint);
            SetVerticesDirty();
        }
    }

    public void UpdateLine()
    {
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2) return;

        float radius = thickness / 2f;
        float maxDistance = thickness * 1.5f;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 center = points[i];
            DrawCircle(vh, center, radius);

            if (i == 0) continue;
            Vector2 prev = points[i - 1];
            if (Vector2.Distance(prev, center) <= maxDistance)
                DrawQuadBetween(vh, prev, center, radius);
        }
    }

    void DrawCircle(VertexHelper vh, Vector2 center, float radius)
    {
        for (int j = 0; j < circleSegments; j++)
        {
            float angle1 = (j / (float)circleSegments) * Mathf.PI * 2f;
            float angle2 = ((j + 1) / (float)circleSegments) * Mathf.PI * 2f;

            Vector2 p1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
            Vector2 p2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;

            UIVertex v0 = UIVertex.simpleVert; v0.position = center; v0.color = color;
            UIVertex v1 = UIVertex.simpleVert; v1.position = p1; v1.color = color;
            UIVertex v2 = UIVertex.simpleVert; v2.position = p2; v2.color = color;

            int idx = vh.currentVertCount;
            vh.AddVert(v0); vh.AddVert(v1); vh.AddVert(v2);
            vh.AddTriangle(idx, idx + 1, idx + 2);
        }
    }

    void DrawQuadBetween(VertexHelper vh, Vector2 p1, Vector2 p2, float halfWidth)
    {
        Vector2 dir = (p2 - p1).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * halfWidth;

        UIVertex v1 = UIVertex.simpleVert; v1.position = p1 + normal; v1.color = color;
        UIVertex v2 = UIVertex.simpleVert; v2.position = p1 - normal; v2.color = color;
        UIVertex v3 = UIVertex.simpleVert; v3.position = p2 - normal; v3.color = color;
        UIVertex v4 = UIVertex.simpleVert; v4.position = p2 + normal; v4.color = color;

        int idx = vh.currentVertCount;
        vh.AddVert(v1); vh.AddVert(v2); vh.AddVert(v3); vh.AddVert(v4);
        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }
}
