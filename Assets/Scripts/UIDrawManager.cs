/*using UnityEngine;
using System.Collections.Generic;

public class DrawingManager : MonoBehaviour
{
    [Header("Line Settings")]
    public GameObject linePrefab;
    public float minDistance = 0.02f;

    private LineRenderer currentLine;
    private List<Vector3> points = new List<Vector3>();
    private Camera cam;

    public Vector2 minBounds;  // bottom-left corner
    public Vector2 maxBounds;  // top-right corner

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            BeginLine();

        if (Input.GetMouseButton(0))
            DrawLine();

        if (Input.GetMouseButtonUp(0))
            EndLine();
    }

    void BeginLine()
    {
        GameObject newLine = Instantiate(linePrefab);
        currentLine = newLine.GetComponent<LineRenderer>();

        points.Clear();
        AddPoint(GetMouseWorldPosition());
    }

    *//*  void DrawLine()
      {
          Vector3 mousePos = GetMouseWorldPosition();

          if (Vector3.Distance(points[points.Count - 1], mousePos) >= minDistance)
          {
              AddPoint(mousePos);
          }
      }*//*

    void DrawLine()
    {
        Vector3 mousePos = GetMouseWorldPosition();

        // LIMIT drawing inside region
        if (!IsInsideBounds(mousePos))
            return;

        if (Vector3.Distance(points[points.Count - 1], mousePos) >= minDistance)
            AddPoint(mousePos);
    }

    bool IsInsideBounds(Vector3 pos)
    {
        return pos.x >= minBounds.x && pos.x <= maxBounds.x &&
               pos.y >= minBounds.y && pos.y <= maxBounds.y;
    }

    void EndLine()
    {
        currentLine = null;
    }

    void AddPoint(Vector3 point)
    {
        points.Add(point);
        currentLine.positionCount = points.Count;
        currentLine.SetPositions(points.ToArray());
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        return cam.ScreenToWorldPoint(mousePos);
    }
}
*/
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIDrawManager : MonoBehaviour
{
    public RectTransform drawArea;
    public UILine linePrefab;

    private UILine currentLine;
    private List<UILine> lines = new List<UILine>();


    // Stroke tracking
    private List<UILine> currentStroke = new List<UILine>();
    public float pointDistance = 0.5f; // interpolation step

    void Update()
    {
        Vector2 inputPos;
        bool inputDown = false;
        bool inputHeld = false;
        bool inputUp = false;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPos = touch.position;
            inputDown = touch.phase == TouchPhase.Began;
            inputHeld = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            inputUp = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
        }
        else
        {
            inputPos = Input.mousePosition;
            inputDown = Input.GetMouseButtonDown(0);
            inputHeld = Input.GetMouseButton(0);
            inputUp = Input.GetMouseButtonUp(0);
        }

        if (inputDown) StartStroke(inputPos);
        if (inputHeld) Draw(inputPos);
        if (inputUp) EndStroke();
    }

    void StartStroke(Vector2 inputPos)
    {
        if (!IsInsideArea(inputPos)) return;

        currentStroke.Clear();
        StartNewLine(inputPos);
    }

    void Draw(Vector2 inputPos)
    {
        if (currentLine == null) return;
        if (!IsInsideArea(inputPos)) return;

      
            AddPointInterpolated(inputPos);

        if (currentLine.points.Count >= currentLine.maxPoints)
            StartNewLine(inputPos);
    }

    void EndStroke()
    {
        currentLine = null;
    }

    void StartNewLine(Vector2 inputPos)
    {
        currentLine = Instantiate(linePrefab, drawArea);
        currentLine.points.Clear();
        AddPointInterpolated(inputPos);

        lines.Add(currentLine);
        currentStroke.Add(currentLine);
    }

    void AddPointInterpolated(Vector2 inputPos)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(drawArea, inputPos, null, out localPos);

        if (currentLine.points.Count == 0)
        {
            currentLine.AddPoint(localPos);
            return;
        }

        Vector2 prev = currentLine.points[currentLine.points.Count - 1];
        float distance = Vector2.Distance(prev, localPos);
        int steps = Mathf.CeilToInt(distance / pointDistance);

        for (int i = 1; i <= steps; i++)
        {
            Vector2 interp = Vector2.Lerp(prev, localPos, i / (float)steps);
            currentLine.AddPoint(interp);
        }
    }

    bool IsInsideArea(Vector2 pos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(drawArea, pos);
    }
  

}
