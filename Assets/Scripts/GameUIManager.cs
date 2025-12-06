using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using System;

public class GameUIManager : MonoBehaviourPunCallbacks
{
    public static GameUIManager instance;

    [Header("Panels")]
    public GameObject lobbyPanel;
    public GameObject gamePanel;
    public GameObject winPanel;
    public GameObject miniWinPanel;
    public TMP_Text miniWinText;

    [Header("Lobby UI")]
    public TMP_Text lobbyInfoText;
    public TMP_InputField chatInput;
    public TMP_Text chatContent;
    public Button startButton;
    public ScrollRect scrollRectChat;

    [Header("Game UI")]
    public TMP_Text timerText;
    public TMP_Text roleText;
    public TMP_Text wordText; // Drawer sees the word
    public GameObject wordTitle;
    public TMP_InputField guessInput;
    public Button submitGuessButton;

    [Header("Win UI")]
    public TMP_Text winnerText;
    public GameObject goBackToLobbyButton;

    [Header("Timer Settings")]
    public float turnDuration = 60f;
    private float timer;
    private bool isTurnActive = false;

    private string currentWord;
    public DrawingTurnManager drawingTurnManager;
    public RectTransform drawingArea;
    [Header("Game Settings")]
    public int totalRounds = 3;
    private int currentRound = 1;

    public Dictionary<int, int> playerScores = new Dictionary<int, int>(); // actorNumber -> score

    private void Awake() => instance = this;

    void Start()
    {
        lobbyPanel.SetActive(true);
        gamePanel.SetActive(false);
        winPanel.SetActive(false);
        miniWinPanel.SetActive(false);
        //chatInput.onEndEdit.AddListener(OnSendChat);
        startButton.onClick.AddListener(StartGame);
        submitGuessButton.onClick.AddListener(OnSubmitGuess);


    }

    void Update()
    {
        if (isTurnActive)
        {
            timer -= Time.deltaTime;
            timerText.text = $"Time: {Mathf.CeilToInt(timer)}";
            if (PhotonNetwork.IsMasterClient && timer <= 0)
            {
                photonView.RPC(nameof(RPC_EndTurn), null, false);
                isTurnActive = false;
            }
        }
    }

    public void OnSendChat()
    {
        string msg = chatInput.text;
        if (string.IsNullOrEmpty(msg)) return;
        photonView.RPC(nameof(RPC_SendChatMessage), RpcTarget.All, PhotonNetwork.NickName, msg);
        chatInput.text = "";
    }

    [PunRPC]
    void RPC_SendChatMessage(string sender, string msg)
    {
        chatContent.text += $"<b>{sender}:</b> {msg}\n";

        Canvas.ForceUpdateCanvases();
        scrollRectChat.verticalNormalizedPosition = 0f;   // scroll to bottom
    }

    public void SetRoleText(string text) => roleText.text = text;
    public void SetWord(string word)
    {
        currentWord = word;
        wordText.text = word;
    }

    void OnSubmitGuess()
    {
        if (string.IsNullOrEmpty(guessInput.text)) return;
        string guess = guessInput.text.Trim().ToLower();
        guessInput.text = "";

        if (guess == currentWord.ToLower())
        {
            photonView.RPC(nameof(RPC_EndTurn),RpcTarget.All, PhotonNetwork.LocalPlayer, false);
        }
    }

    public void StartGame()
    {
        drawingTurnManager.AddAllListForAll();
        photonView.RPC(nameof(RPC_GameStarted), RpcTarget.All);
    }

    [PunRPC]
    void RPC_GameStarted()
    {
        lobbyPanel.SetActive(false);
        gamePanel.SetActive(true);
        currentRound = 1;
        playerScores.Clear();

        if (PhotonNetwork.IsMasterClient)
            drawingTurnManager.AssignRoles(); // Master starts the first turn


    }

    public void StartTurn()
    {
        timer = turnDuration;
        isTurnActive = true;

        bool isDrawer = PhotonNetwork.LocalPlayer == drawingTurnManager.drawer;
        guessInput.gameObject.SetActive(!isDrawer);
        submitGuessButton.gameObject.SetActive(!isDrawer);
        wordText.gameObject.SetActive(isDrawer);
        wordTitle.SetActive(isDrawer);

        // Enable drawer prefab
        foreach (var kvp in drawingTurnManager.playerDrawers)
            kvp.Value.canDraw = kvp.Key == drawingTurnManager.drawer.ActorNumber;
    }

    [PunRPC]
    public void RPC_EndTurn(Player winner, bool timedOut)
    {
        isTurnActive = false;

        // Disable drawing
        foreach (var kvp in drawingTurnManager.playerDrawers)
            kvp.Value.canDraw = false;

        // Determine winner
        Player actualWinner;

        if (winner != null)
        {
            actualWinner = winner;
        }
        else
        {
            actualWinner = drawingTurnManager.drawer;
        }

        // Update drawer score if someone guessed correctly
        if (!playerScores.ContainsKey(actualWinner.ActorNumber))
            playerScores[actualWinner.ActorNumber] = 0;

        if (actualWinner != null)
            playerScores[actualWinner.ActorNumber] += 1;

        // Show mini panel
        string miniText = timedOut ? $"Time Over! Drawer: {drawingTurnManager.drawer.NickName}"
                                   : $"{actualWinner.NickName} won this turn!";
        photonView.RPC(nameof(RPC_ShowMiniWinner), RpcTarget.All, miniText, actualWinner.ActorNumber);

        Invoke(nameof(NextTurn), 1f);
    }

    [PunRPC]
    void RPC_ShowMiniWinner(string text, int winnerActor)
    {
        miniWinPanel.SetActive(true);
        miniWinText.text = text;
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
                    int score = playerScores.ContainsKey(p.ActorNumber) ? playerScores[p.ActorNumber] : 0;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        finalWinner = p;
                    }
                }

                string winnerName = finalWinner != null ? finalWinner.NickName : "No Winner";
                photonView.RPC(nameof(RPC_EndGame), RpcTarget.All, winnerName, finalWinner != null ? finalWinner.ActorNumber : -1);
            }
            else
            {
                currentRound++;
                drawingTurnManager.EndTurn(); // Next drawer
            }
        }
    }

    [PunRPC]
    void RPC_EndGame(string winnerName, int winnerActor)
    {
        gamePanel.SetActive(false);
        winPanel.SetActive(true);
        winnerText.text = $"Winner: {winnerName}";
        goBackToLobbyButton.SetActive(true);

        if (winnerActor == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            _ = IncrementWins(FirebaseManager.Instance.User.UserId);
        }
    }

    public async Task<int> IncrementWins(string userId)
    {
        var db = FirebaseManager.Instance.Firestore;
        DocumentReference doc = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            int wins = 0;
            Dictionary<string, object> statsMap;

            if (snap.Exists)
            {
                if (!snap.TryGetValue("stats", out statsMap))
                    statsMap = new Dictionary<string, object>();

                if (statsMap.TryGetValue("wins", out object w))
                    wins = Convert.ToInt32(w);

                wins++;
            }
            else
            {
                statsMap = new Dictionary<string, object>();
                wins = 1;
            }

            statsMap["wins"] = wins;
            await doc.SetAsync(new Dictionary<string, object> { { "stats", statsMap } }, SetOptions.MergeAll);

            return wins;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to increment wins for user {userId}: {e}");
            return 0;
        }
    }

    public void DisconnectFromPhoton()
    {
        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Disconnected from Photon: {cause}");

        GoBackToLobby();
    }

    public void LeaveGame()
    {
        DisconnectFromPhoton();
    }

    public void GoBackToLobby()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }
  
}

