using UnityEngine;
using Photon.Pun;
public class DrawOnTextureOnline : MonoBehaviourPun/*, IPunObservable*/
{
    public RenderTexture renderTex;
    public Material brushMaterial;
    public float brushSize = 30f;

    RectTransform rect;
    Camera canvasCam;

    public bool canDraw = true;   // ADDED

    private bool drawing = false;
    private Vector2 lastUV;

    public int ActorNumber;

    void Start()
    {
        rect = GameUIManager.instance.drawingArea;

        canvasCam = Camera.main;

        brushMaterial.SetFloat("_BrushSize", brushSize);
        ClearRT();


        ActorNumber = GetComponent<PhotonView>().OwnerActorNr;

        GameUIManager.instance.playerScores[ActorNumber] = 0;
    }

    void Update()
    {
        if (!canDraw) return;         // ADDED
        if (!photonView.IsMine) return; // Only local player handles input

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

    void HandleInput(Vector2 screenPos, bool began, bool ended)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, canvasCam, out local))
            return;

        // Convert to UV
        Vector2 uv = new Vector2(
            Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, local.x),
            Mathf.InverseLerp(rect.rect.yMin, rect.rect.yMax, local.y)
        );

        uv.y = 1f - uv.y; // Flip vertically

        if (began)
        {
            drawing = true;
            lastUV = uv;
        }

        if (drawing && !ended)
        {
            // CALL RPC so everyone draws the same line
            photonView.RPC(nameof(RPC_DrawLine), RpcTarget.AllBuffered, lastUV, uv);
            lastUV = uv;
        }

        if (ended)
            drawing = false;
    }


    // ============================
    // DRAWING RPC (SYNC TO ALL)
    // ============================

    [PunRPC]
    void RPC_DrawLine(Vector2 a, Vector2 b)
    {
        DrawLine(a, b);
    }


    // ============================
    // REAL DRAWING
    // ============================

    void DrawLine(Vector2 a, Vector2 b)
    {
        brushMaterial.SetVector("_StartUV", a);
        brushMaterial.SetVector("_EndUV", b);
        brushMaterial.SetFloat("_BrushSize", brushSize);

        RenderTexture.active = renderTex;
        Graphics.Blit(null, renderTex, brushMaterial);
        RenderTexture.active = null;
    }


    // ============================
    // CLEAR TEXTURE (ALSO SYNCED)
    // ============================

    public void ClearRT()
    {
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = renderTex;
        GL.Clear(true, true, Color.white);
        RenderTexture.active = active;
    }

    [PunRPC]
    public void RPC_ClearRT()
    {
        ClearRT();
    }

    // call this for multiplayer clear
    public void ClearForAll()
    {
        photonView.RPC(nameof(RPC_ClearRT), RpcTarget.AllBuffered);
    }
}
