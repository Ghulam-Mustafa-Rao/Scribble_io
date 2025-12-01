using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;

public class NetworkDrawManager : MonoBehaviourPun
{
    public RectTransform drawArea;
    public UILine linePrefab;

    public float pointDistance = 0.5f;
    public bool eraserMode = false;
    public float eraserRadius = 20f;

    private UILine currentLine;
    private List<UILine> lines = new List<UILine>();
    private List<UILine> currentStroke = new List<UILine>();

    public bool canDraw = false; // only active for the player whose turn it is

    private Stack<List<UILine>> undoStack = new Stack<List<UILine>>();
    private Stack<List<UILine>> redoStack = new Stack<List<UILine>>();

    void Update()
    {
        if (!canDraw) return;

        Vector2 inputPos;
        bool inputDown = false, inputHeld = false, inputUp = false;

        /*if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPos = touch.position;
            inputDown = touch.phase == TouchPhase.Began;
            inputHeld = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            inputUp = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
        }
        else
        {*/
            inputPos = Input.mousePosition;
            inputDown = Input.GetMouseButtonDown(0);
            inputHeld = Input.GetMouseButton(0);
            inputUp = Input.GetMouseButtonUp(0);
        //}

        if (inputDown) StartStroke(inputPos);
        if (inputHeld) Draw(inputPos);
        if (inputUp) EndStroke();
    }

    void StartStroke(Vector2 inputPos)
    {
        if (!IsInsideArea(inputPos)) return;

        currentStroke.Clear();
        StartNewLine(inputPos);
        redoStack.Clear();
    }

    void Draw(Vector2 inputPos)
    {
        if (currentLine == null) return;
        if (!IsInsideArea(inputPos)) return;

        if (eraserMode)
            EraseAtPosition(inputPos);
        else
            AddPointInterpolated(inputPos);

        if (!eraserMode && currentLine.points.Count >= currentLine.maxPoints)
            StartNewLine(inputPos);
    }

    void EndStroke()
    {
        if (!eraserMode && currentStroke.Count > 0)
            undoStack.Push(new List<UILine>(currentStroke));

        currentLine = null;
    }

    void StartNewLine(Vector2 inputPos)
    {
        GameObject obj = PhotonNetwork.Instantiate(linePrefab.name, drawArea.position, Quaternion.identity);
        currentLine = obj.GetComponent<UILine>();
        currentLine.points.Clear();
        AddPointInterpolated(inputPos);

        lines.Add(currentLine);
        currentStroke.Add(currentLine);
    }

    private int lineCounter = 0;
    private int currentLineID = -1;

    void CreateLine()
    {
        currentLineID = lineCounter++;
        var obj = Instantiate(linePrefab, drawArea);

        UILine line = obj.GetComponent<UILine>();
        line.lineID = currentLineID;

        lines.Add(line);

        // Tell others to create the same line
        photonView.RPC("RPC_CreateLine", RpcTarget.Others, currentLineID);
    }
    [PunRPC]
    void RPC_CreateLine(int id)
    {
        UILine obj = Instantiate(linePrefab, drawArea);


        obj.lineID = id;

        lines.Add(obj);
    }
    [PunRPC]
    void RPC_AddPoint(int id, float x, float y)
    {
        UILine targetLine = lines.Find(l => l.lineID == id);
        if (targetLine != null)
            targetLine.points.Add(new Vector2(x, y));
    }


    void AddPointInterpolated(Vector2 inputPos)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(drawArea, inputPos, null, out localPos);

        if (currentLine.points.Count == 0)
        {
            currentLine.AddPoint(localPos);
            //photonView.RPC("RPC_AddPoint", RpcTarget.OthersBuffered, currentLine.GetComponent<PhotonView>().ViewID, localPos);
            photonView.RPC("RPC_AddPoint", RpcTarget.Others, currentLineID, localPos.x, localPos.y);

            return;
        }

        Vector2 prev = currentLine.points[currentLine.points.Count - 1];
        float distance = Vector2.Distance(prev, localPos);
        int steps = Mathf.CeilToInt(distance / pointDistance);

        for (int i = 1; i <= steps; i++)
        {
            Vector2 interp = Vector2.Lerp(prev, localPos, i / (float)steps);
            currentLine.AddPoint(interp);
            //photonView.RPC("RPC_AddPoint", RpcTarget.OthersBuffered, currentLine.GetComponent<PhotonView>().ViewID, interp);
            photonView.RPC("RPC_AddPoint", RpcTarget.Others, currentLineID, localPos.x, localPos.y);

        }
    }

    bool IsInsideArea(Vector2 pos) => RectTransformUtility.RectangleContainsScreenPoint(drawArea, pos);

    #region Eraser
    void EraseAtPosition(Vector2 inputPos)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(drawArea, inputPos, null, out localPos);

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            UILine line = lines[i];
            bool changed = false;

            for (int j = line.points.Count - 1; j >= 0; j--)
            {
                if (Vector2.Distance(line.points[j], localPos) <= eraserRadius)
                {
                    line.points.RemoveAt(j);
                    changed = true;
                }
            }

            if (line.points.Count == 0)
            {
                Destroy(line.gameObject);
                lines.RemoveAt(i);
            }
            else if (changed)
                line.UpdateLine();
        }
    }

    public void SetEraserMode(bool enable) => eraserMode = enable;
    #endregion

    #region Undo/Redo
    [ContextMenu("Undo")]
    public void Undo()
    {
        if (undoStack.Count == 0) return;
        List<UILine> lastStroke = undoStack.Pop();
        foreach (var line in lastStroke)
        {
            lines.Remove(line);
            if (line.GetComponent<PhotonView>().IsMine)
                PhotonNetwork.Destroy(line.gameObject);
        }
        redoStack.Push(lastStroke);
    }

    [ContextMenu("Redo")]
    public void Redo()
    {
        if (redoStack.Count == 0) return;
        List<UILine> stroke = redoStack.Pop();
        foreach (var line in stroke)
        {
            line.gameObject.SetActive(true);
            lines.Add(line);
        }
        undoStack.Push(stroke);
    }
    #endregion

    #region RPC
  /*  [PunRPC]
    void RPC_AddPoint(int viewID, Vector2 point)
    {
        PhotonView pv = PhotonView.Find(viewID);
        if (pv == null) return;
        UILine line = pv.GetComponent<UILine>();
        line.AddPoint(point);
    }*/
    #endregion
}
