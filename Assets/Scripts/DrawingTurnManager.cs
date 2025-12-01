using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class DrawingTurnManager : MonoBehaviourPunCallbacks
{
    public int currentPlayerIndex = 0;
    //private Player[] players;
    public Player drawer;
    public Player guesser;
    [SerializeField]
    WordManager wordManager;
    [SerializeField]
    GameUIManager gameUIManager;[SerializeField]
    NetworkDrawManager networkDrawManager;
    void Start()
    {
        if (!PhotonNetwork.InRoom) return;
        //players = PhotonNetwork.PlayerList;
    }

    void BroadcastTurn()
    {
        //players = PhotonNetwork.PlayerList;
        photonView.RPC("RPC_SetCurrentTurn", RpcTarget.All, PhotonNetwork.PlayerList[currentPlayerIndex].ActorNumber);
    }

    public void EndTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % PhotonNetwork.PlayerList.Length;
        BroadcastTurn();
    }

    [PunRPC]
    void RPC_SetCurrentTurn(int actorNumber)
    {
        foreach (Player player in PhotonNetwork.PlayerList) { 
        
            if(player.ActorNumber == actorNumber)
            {
                drawer = player;
            }
            else
            {
                guesser = player;
            }
        }
       
        // Set role texts
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            gameUIManager.SetRoleText("You are DRAWER");
            string word = wordManager.GetRandomWord();
            gameUIManager.SetWord(word); // Show word only to drawer
        }
        else
        {
            gameUIManager.SetRoleText("You are GUESSER");
        }
        networkDrawManager.canDraw = PhotonNetwork.LocalPlayer.ActorNumber == actorNumber;

        gameUIManager.StartTurn();
    }

    

    public void AssignRoles()
    {
        Player[] players = PhotonNetwork.PlayerList;
      
        currentPlayerIndex = Random.Range(0, players.Length);
        BroadcastTurn();
        // Enable drawing for drawer
        //NetworkDrawManager.canDraw = (PhotonNetwork.LocalPlayer == drawer);
    }

    public int totalRounds = 3;
    private int currentRound = 0;


    /*public void NextRound()
    {
        currentRound++;
        if (currentRound >= totalRounds)
        {
            gameUIManager.EndGame(); // show final winner
        }
        else
        {
            AssignRoles();
            gameUIManager.StartTurn();
        }
    }*/
}
