using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;

public class DrawingTurnManager : MonoBehaviourPunCallbacks
{
    public int currentPlayerIndex = 0;

    [SerializeField] WordManager wordManager;
    [SerializeField] GameUIManager gameUIManager;
    [SerializeField] GameObject drawPrefab; // Your drawing prefab (must have PhotonView)

    public Player drawer;
    public Player guesser;

    public bool gameStarted;

    // Dictionary to store spawned draw prefabs per player
    public Dictionary<int, DrawOnTextureOnline> playerDrawers = new Dictionary<int, DrawOnTextureOnline>();
    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Spawn self prefab for the first player
            SpawnDrawPrefab(PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
    // -------------------------------
    // Player joins: MasterClient spawns prefab for everyone
    // -------------------------------
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Spawn prefab for the new player
            photonView.RPC(nameof(RPC_SpawnDrawPrefab), newPlayer, newPlayer.ActorNumber);

            if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
                Invoke(nameof(ShowStartButton), 1.5f);

            PhotonNetwork.CurrentRoom.IsVisible = !gameStarted;
            PhotonNetwork.CurrentRoom.IsOpen = !gameStarted;
        }
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        gameUIManager.lobbyInfoText.text = $"{otherPlayer.NickName} left the room.";

        gameUIManager.startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2);

        PhotonNetwork.CurrentRoom.IsVisible = !gameStarted;
        PhotonNetwork.CurrentRoom.IsOpen = !gameStarted;

    }
    void ShowStartButton()
    {
        GameUIManager.instance.startButton.gameObject.SetActive(true);
    }
    public void AddAllListForAll()
    {
        var drawers = FindObjectsByType<DrawOnTextureOnline>(FindObjectsSortMode.None);
      
        for (int i = 0; i < drawers.Length; i++)
        {
            playerDrawers[drawers[i].ActorNumber] = drawers[i];
        }      
            photonView.RPC(nameof(RPC_RegisterPrefab), RpcTarget.All);
    }


    // -------------------------------
    // Spawn draw prefab (MasterClient only)
    // -------------------------------

    [PunRPC]
    void RPC_SpawnDrawPrefab(int actorNumber)
    {
        GameObject go = PhotonNetwork.Instantiate(drawPrefab.name, Vector3.zero, Quaternion.identity);
     
        DrawOnTextureOnline drawerComp = go.GetComponent<DrawOnTextureOnline>();
        drawerComp.canDraw = false;

        playerDrawers[actorNumber] = drawerComp;
        // Add to dictionary for everyone (buffered via RPC)
        photonView.RPC(nameof(RPC_RegisterPrefab), RpcTarget.All);
    }


    void SpawnDrawPrefab(int actorNumber)
    {
        // Use PhotonNetwork.Instantiate to sync across all clients
        GameObject go = PhotonNetwork.Instantiate(drawPrefab.name, Vector3.zero, Quaternion.identity);
        DrawOnTextureOnline drawerComp = go.GetComponent<DrawOnTextureOnline>();

        // Initially disable drawing
        drawerComp.canDraw = false;
    }

    // -------------------------------
    // RPC: register prefab locally on all clients
    // -------------------------------
    [PunRPC]
    void RPC_RegisterPrefab()
    {
        var drawers = FindObjectsByType<DrawOnTextureOnline>(FindObjectsSortMode.None);
        for (int i = 0; i < drawers.Length; i++)
        {
            playerDrawers[drawers[i].ActorNumber] = drawers[i];
        }
    }

    // -------------------------------
    // Start first turn
    // -------------------------------
    public void AssignRoles()
    {
        Player[] players = PhotonNetwork.PlayerList;
        currentPlayerIndex = Random.Range(0, players.Length);
        BroadcastTurn();
    }

    // -------------------------------
    // Broadcast turn to all clients
    // -------------------------------
    void BroadcastTurn()
    {
        gameStarted = true;
        PhotonNetwork.CurrentRoom.IsVisible = !gameStarted;
        PhotonNetwork.CurrentRoom.IsOpen = !gameStarted;

        int actorNumber = PhotonNetwork.PlayerList[currentPlayerIndex].ActorNumber;
        photonView.RPC(nameof(RPC_SetCurrentTurn), RpcTarget.All, actorNumber);
    }

    // -------------------------------
    // RPC: Set current turn for everyone
    // -------------------------------
    [PunRPC]
    void RPC_SetCurrentTurn(int actorNumber)
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == actorNumber)
                drawer = p;
            else
                guesser = p;
        }

        // UI updates
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            gameUIManager.SetRoleText("You are DRAWER");
            string word = wordManager.GetRandomWord();
            photonView.RPC(nameof(RPC_SetWord), RpcTarget.All, word);
        }
        else
        {
            gameUIManager.SetRoleText("You are GUESSER");
        }

        // Enable drawer prefab only for current drawer
        foreach (var kvp in playerDrawers)
        {
            if (kvp.Key == actorNumber)
                kvp.Value.ClearForAll();
            kvp.Value.canDraw = (kvp.Key == actorNumber);
        }
        gameUIManager.miniWinPanel.SetActive(false);
        gameUIManager.StartTurn();
    }

    [PunRPC]
    void RPC_SetWord(string word)
    {
        gameUIManager.SetWord(word);
    }

    // -------------------------------
    // Move to next turn
    // -------------------------------
    public void EndTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % PhotonNetwork.PlayerList.Length;
        BroadcastTurn();
    }
}
