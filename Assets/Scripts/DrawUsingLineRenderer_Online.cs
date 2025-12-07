
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
public class DrawUsingLineRenderer_Online : MonoBehaviourPunCallbacks
{
    [Header("Brush Settings")]
    public Color brushColor = Color.black;
    public float brushSize = 0.05f;
    public Material brushMaterial;

    [Header("Draw Area")]
    public RectTransform drawArea; // UI element defining the drawable area

    [Header("Draw Area Padding")]
    public float paddingLeft = 0f;
    public float paddingRight = 0f;
    public float paddingTop = 0f;
    public float paddingBottom = 0f;

    private LineRenderer currentLine;
    private List<Vector3> points = new List<Vector3>();
    private bool drawing = false;
    public bool canDraw;
    void Update()
    {
        // Only master client can draw
        if (!canDraw) return;
        if (drawArea == null) return;

        Vector3 worldPos;
        bool inputDown = false;
        bool inputHeld = false;
        bool inputUp = false;

        // ---------- Mobile ----------
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            inputDown = t.phase == TouchPhase.Began;
            inputHeld = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
            inputUp = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            worldPos = GetWorldPoint(t.position);
        }
        // ---------- PC ----------
        else
        {
            inputDown = Input.GetMouseButtonDown(0);
            inputHeld = Input.GetMouseButton(0);
            inputUp = Input.GetMouseButtonUp(0);
            worldPos = GetWorldPoint(Input.mousePosition);
        }

        if (inputUp)
        {
            drawing = false;
            currentLine = null;
            points.Clear();
            return;
        }

        // Stop drawing if outside draw area
        if (!IsInsideDrawArea(worldPos))
        {
            drawing = false;
            currentLine = null;
            points.Clear();
            return;
        }

        worldPos.z--; // ensure in front of canvas

        if (inputDown)
        {
            StartNewLine(worldPos);
            photonView.RPC("RPC_StartLine", RpcTarget.OthersBuffered, worldPos);
        }

        if (drawing && inputHeld)
        {
            AddPoint(worldPos);
            photonView.RPC("RPC_AddPoint", RpcTarget.OthersBuffered, worldPos);
        }
    }

    Vector3 GetWorldPoint(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        Plane plane = new Plane(drawArea.forward, drawArea.position);
        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return Vector3.zero;
    }

    bool IsInsideDrawArea(Vector3 worldPos)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(drawArea, Camera.main.WorldToScreenPoint(worldPos), Camera.main, out localPoint))
        {
            float minX = drawArea.rect.xMin + paddingLeft;
            float maxX = drawArea.rect.xMax - paddingRight;
            float minY = drawArea.rect.yMin + paddingBottom;
            float maxY = drawArea.rect.yMax - paddingTop;

            return localPoint.x >= minX && localPoint.x <= maxX &&
                   localPoint.y >= minY && localPoint.y <= maxY;
        }
        return false;
    }

    void StartNewLine(Vector3 startPos)
    {
        drawing = true;

        GameObject lineObj = new GameObject("LineStroke");
        currentLine = lineObj.AddComponent<LineRenderer>();

        currentLine.positionCount = 0;
        currentLine.material = brushMaterial;
        currentLine.startColor = brushColor;
        currentLine.endColor = brushColor;
        currentLine.startWidth = brushSize;
        currentLine.endWidth = brushSize;
        currentLine.useWorldSpace = true;
        currentLine.numCapVertices = 5;

        points.Clear();
        AddPoint(startPos);
    }

    void AddPoint(Vector3 newPoint)
    {
        if (points.Count > 0 && Vector3.Distance(points[points.Count - 1], newPoint) < brushSize * 0.1f)
            return;

        points.Add(newPoint);
        currentLine.positionCount = points.Count;
        currentLine.SetPositions(points.ToArray());
    }

    // ---------- Photon RPCs ----------
    [PunRPC]
    void RPC_StartLine(Vector3 startPos)
    {
        drawing = true;

        GameObject lineObj = new GameObject("LineStroke_Remote");
        currentLine = lineObj.AddComponent<LineRenderer>();

        currentLine.positionCount = 0;
        currentLine.material = brushMaterial;
        currentLine.startColor = brushColor;
        currentLine.endColor = brushColor;
        currentLine.startWidth = brushSize;
        currentLine.endWidth = brushSize;
        currentLine.useWorldSpace = true;
        currentLine.numCapVertices = 5;

        points.Clear();
        AddPoint(startPos);
    }

    [PunRPC]
    void RPC_AddPoint(Vector3 newPoint)
    {
        AddPoint(newPoint);
    }
}