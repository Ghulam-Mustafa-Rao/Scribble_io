using UnityEngine;
using UnityEngine.UI;

public class DrawOnTexture : MonoBehaviour
{
    public RenderTexture renderTex;
    public Material brushMaterial;
    public float brushSize = 30f;

    public RectTransform rect;
    public Camera canvasCam;

    private bool drawing = false;
    private Vector2 lastUV;

    void Start()
    {
       

       /* // detect the correct camera
        canvasCam = GetComponentInParent<Canvas>().worldCamera;*/

        brushMaterial.SetFloat("_BrushSize", brushSize);

        ClearRT();
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            HandleInput(t.position, t.phase == TouchPhase.Began, t.phase == TouchPhase.Ended);
        }
        else if (Input.GetMouseButton(0))
        {
            HandleInput(Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0));
        }
        else
        {
            drawing = false;
        }
    }

    public bool IsInsideRawImage(/*RawImage rawImg,*/ Vector2 screenPos)
    {
        //RectTransform rt = rawImg.rectTransform;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);
    }


    void HandleInput(Vector2 screenPos, bool began, bool ended)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, canvasCam, out local))
            return;

        // convert to UV
        Vector2 uv = new Vector2(
            Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, local.x),
            Mathf.InverseLerp(rect.rect.yMin, rect.rect.yMax, local.y)
        );

        // FIX: For some Android devices + RawImage UV mismatch
        uv.y = 1f - uv.y;   // Vertical flip
        // If still mirrored sideways:
        // uv.x = 1f - uv.x;

        if (began)
        {
            drawing = true;
            lastUV = uv;
        }

        if (drawing && !ended)
        {
            DrawLine(lastUV, uv);
            lastUV = uv;
        }

        if (ended)
            drawing = false;
    }

    void DrawLine(Vector2 a, Vector2 b)
    {
        brushMaterial.SetVector("_StartUV", a);
        brushMaterial.SetVector("_EndUV", b);
        brushMaterial.SetFloat("_BrushSize", brushSize);

        RenderTexture.active = renderTex;
        Graphics.Blit(null, renderTex, brushMaterial);
        RenderTexture.active = null;
    }

    public void ClearRT(/*RenderTexture rt, Color clearColor*/)
    {
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = renderTex;
        GL.Clear(true, true, Color.white);
        RenderTexture.active = active;
    }

}
