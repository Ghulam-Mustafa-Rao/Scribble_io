using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;

public class DrawingTurnManager : MonoBehaviourPunCallbacks
{
    public bool gameStarted;
    public int currentPlayerIndex = 0;

    [SerializeField] WordManager wordManager;
    [SerializeField] GameUIManager gameUIManager;
    [SerializeField] GameObject drawPrefab; // Your drawing prefab (must have PhotonView)

    public Player drawer;
    public Player guesser;

    
    [Header("Game Settings")]
    public int totalRounds = 3;
    public int currentRound = 1;
    public float turnDuration = 60f;
    public float timer;
    public bool isTurnActive = false;

    public string currentWord;
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

    void Update()
    {
        if (isTurnActive)
        {
            timer -= Time.deltaTime;
            gameUIManager.timerText.text = $"Time: {Mathf.CeilToInt(timer)}";
            if (PhotonNetwork.IsMasterClient && timer <= 0)
            {
                photonView.RPC(nameof(RPC_EndTurn), null, false);
                isTurnActive = false;
            }
        }
    }


    public void EndTurn(Player winner, bool timedOut)
    {
        photonView.RPC(nameof(RPC_EndTurn), RpcTarget.All, winner, timedOut);
    }

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

    [PunRPC]
    void RPC_RegisterPrefab()
    {
        var drawers = FindObjectsByType<DrawOnTextureOnline>(FindObjectsSortMode.None);
        for (int i = 0; i < drawers.Length; i++)
        {
            playerDrawers[drawers[i].ActorNumber] = drawers[i];
        }
    }

   
    public void AssignRoles()
    {
        Player[] players = PhotonNetwork.PlayerList;
        currentPlayerIndex = Random.Range(0, players.Length);
        BroadcastTurn();
    }

    void BroadcastTurn()
    {
        gameStarted = true;
        PhotonNetwork.CurrentRoom.IsVisible = !gameStarted;
        PhotonNetwork.CurrentRoom.IsOpen = !gameStarted;

        int actorNumber = PhotonNetwork.PlayerList[currentPlayerIndex].ActorNumber;
        photonView.RPC(nameof(RPC_SetCurrentTurn), RpcTarget.All, actorNumber);
    }

  
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

    public void EndTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % PhotonNetwork.PlayerList.Length;
        BroadcastTurn();
    }
    [PunRPC]
    public void RPC_EndTurn(Player winner, bool timedOut)
    {
        isTurnActive = false;

        // Disable drawing
        foreach (var kvp in playerDrawers)
            kvp.Value.canDraw = false;

        // Determine winner
        Player actualWinner;

        if (winner != null)
        {
            actualWinner = winner;
        }
        else
        {
            actualWinner = drawer;
        }

        // Update drawer score if someone guessed correctly
        if (!gameUIManager.playerScores.ContainsKey(actualWinner.ActorNumber))
            gameUIManager.playerScores[actualWinner.ActorNumber] = 0;

        if (actualWinner != null)
            gameUIManager.playerScores[actualWinner.ActorNumber] += 1;

        // Show mini panel
        string miniText = timedOut ? $"Time Over! Drawer: {drawer.NickName}"
                                   : $"{actualWinner.NickName} won this turn!";
        gameUIManager.ShowMiniWinner(miniText, actualWinner.ActorNumber);
        

        Invoke(nameof(NextTurn), 1f);
    }
    void NextTurn()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (currentRound >= totalRounds)
            {
                // Game over
                int maxScore = -1;
                Player finalWinner = null;
                foreach (var p in PhotonNetwork.PlayerList)
                {
                    int score = gameUIManager.playerScores.ContainsKey(p.ActorNumber) ? gameUIManager.playerScores[p.ActorNumber] : 0;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        finalWinner = p;
                    }
                }

                string winnerName = finalWinner != null ? finalWinner.NickName : "No Winner";
                gameUIManager.EndGame(winnerName, finalWinner != null ? finalWinner.ActorNumber : -1);
                
            }
            else
            {
                currentRound++;
                EndTurn(); // Next drawer
            }
        }
    }
}
