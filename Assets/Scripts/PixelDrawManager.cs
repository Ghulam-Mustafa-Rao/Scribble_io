using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PixelDrawManager : MonoBehaviour
{
    [Header("UI References")]
    public RawImage drawArea;

    [Header("Brush Settings")]
    public Color selectedColor = Color.black;
    public int brushSize = 4;
    public Color eraserColor = Color.white;

    private Texture2D texture;
    private Color[] emptyColors;

    private Vector2Int prevPos;
    private bool isDrawing = false;

    // Undo / Redo
    private Stack<Color[]> undoStack = new Stack<Color[]>();
    private Stack<Color[]> redoStack = new Stack<Color[]>();

    // Tool Enum
    private enum Tool { Pencil, Eraser, Fill }
    private Tool currentTool = Tool.Pencil;

    void Start()
    {
        InitTexture();
    }

    void InitTexture()
    {
        RectTransform rt = drawArea.rectTransform;
        int w = Mathf.RoundToInt(rt.rect.width);
        int h = Mathf.RoundToInt(rt.rect.height);

        texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point; // pixel perfect
        emptyColors = new Color[w * h];
        for (int i = 0; i < emptyColors.Length; i++) emptyColors[i] = Color.white;

        texture.SetPixels(emptyColors);
        texture.Apply();
        drawArea.texture = texture;
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Vector2 touchPos = Input.GetTouch(0).position;
            HandleTouch(touchPos, Input.GetTouch(0).phase);
        }
        else if (Application.isEditor && Input.GetMouseButton(0)) // Editor testing
        {
            HandleTouch(Input.mousePosition, Input.GetMouseButtonDown(0) ? TouchPhase.Began : TouchPhase.Moved);
        }
    }

    void HandleTouch(Vector2 screenPos, TouchPhase phase)
    {
        Vector2 localMousePos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(drawArea.rectTransform, screenPos, null, out localMousePos))
            return;

        Vector2Int pixelPos = new Vector2Int(
            Mathf.RoundToInt(localMousePos.x + texture.width / 2),
            Mathf.RoundToInt(localMousePos.y + texture.height / 2)
        );

        if (phase == TouchPhase.Began)
        {
            SaveUndo();
            isDrawing = true;
            prevPos = pixelPos;

            if (currentTool == Tool.Fill)
                BucketFill(pixelPos.x, pixelPos.y, selectedColor);
            else
                DrawBrush(pixelPos);
        }
        else if ((phase == TouchPhase.Moved || phase == TouchPhase.Stationary) && isDrawing)
        {
            if (currentTool != Tool.Fill)
            {
                DrawLine(prevPos, pixelPos);
                prevPos = pixelPos;
            }
        }
        else if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
        {
            isDrawing = false;
        }
    }

    #region Drawing
    void DrawBrush(Vector2Int pos)
    {
        Color c = (currentTool == Tool.Eraser) ? eraserColor : selectedColor;

        for (int x = -brushSize / 2; x <= brushSize / 2; x++)
            for (int y = -brushSize / 2; y <= brushSize / 2; y++)
            {
                int px = Mathf.Clamp(pos.x + x, 0, texture.width - 1);
                int py = Mathf.Clamp(pos.y + y, 0, texture.height - 1);
                texture.SetPixel(px, py, c);
            }
        texture.Apply();
    }

    void DrawLine(Vector2Int start, Vector2Int end)
    {
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);

        int sx = (start.x < end.x) ? 1 : -1;
        int sy = (start.y < end.y) ? 1 : -1;

        int err = dx - dy;
        int x = start.x;
        int y = start.y;

        while (true)
        {
            DrawBrush(new Vector2Int(x, y));

            if (x == end.x && y == end.y) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx) { err += dx; y += sy; }
        }
    }
    #endregion

    #region Tools
    public void SetToolPencil() { currentTool = Tool.Pencil; }
    public void SetToolEraser() { currentTool = Tool.Eraser; }
    public void SetToolFill() { currentTool = Tool.Fill; }

    public void SetBrushSize(int size) { brushSize = size; }
    public void SetColor(Color c) { selectedColor = c; }
    #endregion

    #region Bucket Fill
    void BucketFill(int x, int y, Color fillColor)
    {
        Color targetColor = texture.GetPixel(x, y);
        if (targetColor == fillColor) return;

        Stack<Vector2Int> pixels = new Stack<Vector2Int>();
        pixels.Push(new Vector2Int(x, y));

        while (pixels.Count > 0)
        {
            Vector2Int p = pixels.Pop();
            if (p.x < 0 || p.y < 0 || p.x >= texture.width || p.y >= texture.height)
                continue;

            if (texture.GetPixel(p.x, p.y) != targetColor)
                continue;

            texture.SetPixel(p.x, p.y, fillColor);

            pixels.Push(new Vector2Int(p.x + 1, p.y));
            pixels.Push(new Vector2Int(p.x - 1, p.y));
            pixels.Push(new Vector2Int(p.x, p.y + 1));
            pixels.Push(new Vector2Int(p.x, p.y - 1));
        }

        texture.Apply();
    }
    #endregion

    #region Undo / Redo
    void SaveUndo()
    {
        undoStack.Push(texture.GetPixels());
        redoStack.Clear();
    }

    public void Undo()
    {
        if (undoStack.Count == 0) return;

        redoStack.Push(texture.GetPixels());
        Color[] colors = undoStack.Pop();
        texture.SetPixels(colors);
        texture.Apply();
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;

        undoStack.Push(texture.GetPixels());
        Color[] colors = redoStack.Pop();
        texture.SetPixels(colors);
        texture.Apply();
    }
    #endregion
}
