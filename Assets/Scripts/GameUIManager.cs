/*using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections;

public class GameUIManager : MonoBehaviourPunCallbacks
{
    [Header("Lobby UI")]
    public GameObject lobbyPanel;            // Panel for lobby chat / waiting
    public TMP_InputField chatInput;         // Chat input field
    public TMP_Text chatContent;             // Chat display
    public Button startButton;               // Start game button (only owner)

    [Header("Game UI")]
    public GameObject gamePanel;             // Panel shown during game
    public TMP_Text timerText;               // Countdown timer
    public GameObject winPanel;              // Win panel
    public TMP_Text winText;                 // Text to show winner
    public Button endTurnButton;             // Optional: end turn button

    [Header("Game Settings")]
    public float turnDuration = 30f;         // Turn time in seconds
    private float currentTimer = 0f;
    private bool isTurnActive = false;

    void Start()
    {
        // Only the room owner can start the game
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        startButton.onClick.AddListener(StartGame);

        // Hide game panel initially
        gamePanel.SetActive(false);
        winPanel.SetActive(false);

        // Chat input listener
        chatInput.onEndEdit.AddListener(OnSendChatMessage);
    }

    #region Lobby
    void OnSendChatMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;

        photonView.RPC("RPC_SendChatMessage", RpcTarget.All, PhotonNetwork.NickName, msg);
        chatInput.text = "";
    }

    [PunRPC]
    void RPC_SendChatMessage(string sender, string message)
    {
        chatContent.text += $"<b>{sender}:</b> {message}\n";
    }

    public void StartGame()
    {
        // Hide lobby panel, show game panel
        lobbyPanel.SetActive(false);
        gamePanel.SetActive(true);

        // Notify all clients that game started
        photonView.RPC("RPC_GameStarted", RpcTarget.All);
    }

    [PunRPC]
    void RPC_GameStarted()
    {
        currentTimer = turnDuration;
        isTurnActive = true;
        StartCoroutine(TurnTimer());
    }
    #endregion

    #region Timer
    IEnumerator TurnTimer()
    {
        while (isTurnActive)
        {
            if (currentTimer > 0)
            {
                currentTimer -= Time.deltaTime;
                timerText.text = $"Time: {Mathf.CeilToInt(currentTimer)}";
            }
            else
            {
                // Time over, end turn
                isTurnActive = false;
                timerText.text = "Time's Up!";
                DrawingTurnManager turnManager = FindObjectOfType<DrawingTurnManager>();
                if (turnManager != null)
                    turnManager.EndTurn();

                yield break;
            }
            yield return null;
        }
    }
    #endregion

    #region Win Panel
    public void ShowWinPanel(string winnerName)
    {
        winPanel.SetActive(true);
        winText.text = $"Winner: {winnerName}";
    }

    public void HideWinPanel()
    {
        winPanel.SetActive(false);
    }
    #endregion

    #region Optional: End Turn Button (only active for drawer)
    public void OnEndTurnButtonClicked()
    {
        DrawingTurnManager turnManager = FindObjectOfType<DrawingTurnManager>();
        if (turnManager != null)
            turnManager.EndTurn();
    }
    #endregion

    #region PUN Callbacks
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        chatContent.text += $"<i>{newPlayer.NickName} joined the room</i>\n";
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        chatContent.text += $"<i>{otherPlayer.NickName} left the room</i>\n";
    }
    #endregion
}
*/

using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using System.Threading.Tasks;
using Photon.Realtime;
using Firebase.Firestore;
using System;

public class GameUIManager : MonoBehaviourPunCallbacks
{
    [Header("Panels")]
    public GameObject lobbyPanel;
    public GameObject gamePanel;
    public GameObject winPanel;

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
    public TMP_InputField guessInput;
    public Button submitGuessButton;

    [Header("Win UI")]
    public TMP_Text winnerText;
    public Button playAgainButton;

    [Header("Timer Settings")]
    public float turnDuration = 60f;
    private float timer;
    private bool isTurnActive = false;

    private string currentWord;
    [SerializeField] DrawingTurnManager drawingTurnManager;
    void Start()
    {
        lobbyPanel.SetActive(true);
        gamePanel.SetActive(false);
        winPanel.SetActive(false);

        startButton.onClick.AddListener(StartGame);

        //chatInput.onEndEdit.AddListener(OnSendChat);
        submitGuessButton.onClick.AddListener(OnSubmitGuess);
    }

    void Update()
    {
        if (isTurnActive)
        {
            timer -= Time.deltaTime;
            timerText.text = "Time: " + Mathf.CeilToInt(timer);
            if (timer <= 0) EndTurn();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName} joined the room.");
        lobbyInfoText.text = $"{newPlayer.NickName} joined the room.";

        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} left the room.");
        lobbyInfoText.text = $"{otherPlayer.NickName} left the room.";

        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2);

    }

    public void SetRoleText(string text)
    {
        roleText.text = text;
    }

    public void SetWord(string word)
    {
        currentWord = word;
        wordText.text = word;
    }

    public void OnSendChat()
    {
        string msg = chatInput.text;
        if (string.IsNullOrEmpty(msg)) return;
        photonView.RPC("RPC_SendChatMessage", RpcTarget.All, PhotonNetwork.NickName, msg);
        chatInput.text = "";
    }

    [PunRPC]
    void RPC_SendChatMessage(string sender, string msg)
    {
        chatContent.text += $"<b>{sender}:</b> {msg}\n";

        Canvas.ForceUpdateCanvases();
        scrollRectChat.verticalNormalizedPosition = 0f;   // scroll to bottom
    }


    void OnSubmitGuess()
    {
        if (string.IsNullOrEmpty(guessInput.text)) return;
        string guess = guessInput.text.Trim().ToLower();
        guessInput.text = "";

        if (guess == currentWord.ToLower())
        {
            // Guesser wins
            EndTurn(winner: PhotonNetwork.LocalPlayer);
        }
    }

    public void StartGame()
    {
        photonView.RPC("RPC_GameStarted", RpcTarget.All);
    }

    [PunRPC]
    void RPC_GameStarted()
    {
        lobbyPanel.SetActive(false);
        gamePanel.SetActive(true);

        if (PhotonNetwork.IsMasterClient)
            drawingTurnManager.AssignRoles();

    }

    public void StartTurn()
    {
        timer = turnDuration;
        isTurnActive = true;
        guessInput.gameObject.SetActive(PhotonNetwork.LocalPlayer != drawingTurnManager.drawer);
        submitGuessButton.gameObject.SetActive(PhotonNetwork.LocalPlayer != drawingTurnManager.drawer);
        wordText.gameObject.SetActive(PhotonNetwork.LocalPlayer == drawingTurnManager.drawer);
    }

    public async void EndTurn(Player winner = null)
    {
        isTurnActive = false;

        string winnerName = winner != null ? winner.NickName : drawingTurnManager.drawer.NickName;
        winPanel.SetActive(true);
        gamePanel.SetActive(false);
        winnerText.text = $"Winner: {winnerName}";

        if (winner != null)
        {
            // Increment Firebase wins
            await IncrementWins(winner.UserId);
        }
    }

    public void PlayAgain()
    {
        PhotonNetwork.LoadLevel(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public async Task IncrementWins(string userId)
    {
        var db = FirebaseManager.Instance.Firestore;
        DocumentReference doc = db.Collection("users")
                                  .Document(userId);

        try
        {
            await doc.UpdateAsync("wins", FieldValue.Increment(1));
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to increment wins: " + e);
        }
    }
}
