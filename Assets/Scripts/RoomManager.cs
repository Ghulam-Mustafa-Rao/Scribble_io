using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    //public TMP_InputField roomNameInput;
    public Transform roomListParent;
    public RoomListItem roomListItemPrefab;
    List<RoomListItem> roomListItems;
    public TextMeshProUGUI statustext;
    public GameObject lobbyWaitPanel;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    void Start()
    {
        lobbyWaitPanel.SetActive(true);
        statustext.text = "Connecting to server....";
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings(); // Connect to Photon Master server
        }
        
    }

    #region Photon Callbacks

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings(); // Connect to Photon Master server
    }

    public override void OnConnectedToMaster()
    {
        statustext.text = "Joining Lobby....";
        Debug.Log("Connected to Photon Master Server");     

        string nickName = FirebaseManager.Instance.User.Email.Split('@')[0];
        Debug.Log("Before @ (Split): " + nickName);

        PhotonNetwork.NickName = nickName;
        PhotonNetwork.JoinLobby(); // Join the default lobby to get room list updates
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        statustext.text = "Lobby joined....";
        lobbyWaitPanel.SetActive(false);
        UpdateRoomListUI();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        cachedRoomList.Clear();

        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                    cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }

        UpdateRoomListUI();
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room Created: " + PhotonNetwork.CurrentRoom.Name);
        //PhotonNetwork.LoadLevel("GameplayScene"); // Load your game scene here
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.LoadLevel("GameplayScene"); // Load your game scene here
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Room creation failed: " + message);
    }

    #endregion

    #region Room Management

    /* void CreateRoom()
     {
         //string roomName = roomNameInput.text;
         if (string.IsNullOrEmpty(roomName))
         {
             Debug.LogWarning("Room name is empty!");
             return;
         }

         RoomOptions options = new()
         {
             MaxPlayers = 2 // Set max players per room
         };

         PhotonNetwork.CreateRoom(roomName, options);
     }
 */
    public void JoinRoom(string roomName)
    {
        if (cachedRoomList.ContainsKey(roomName))
        {
            PhotonNetwork.JoinRoom(roomName);
        }
        else
        {
            Debug.LogWarning("Room does not exist: " + roomName);
        }
    }

    public void QuickJoin()
    {
        // Try joining any available room
        foreach (var room in cachedRoomList.Values)
        {
            if (room.PlayerCount < room.MaxPlayers && room.IsOpen && room.IsVisible)
            {
                PhotonNetwork.JoinRoom(room.Name);
                return;
            }
        }

        // If no room available => create one automatically
        CreateRandomRoom();
    }

    public void CreateRandomRoom()
    {
        string roomName = FirebaseManager.Instance.User.Email;
        Debug.Log("No room found. Creating new room: " + roomName);

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, options);
    }


    #endregion

    #region UI

    public void UpdateRoomListUI()
    {
        // Clear old room list UI
        if (roomListItems != null)
        {
            foreach (RoomListItem roomListItem in roomListItems)
            {
                Destroy(roomListItem.gameObject);
            }
        }
        roomListItems = new List<RoomListItem>();

        foreach (var room in cachedRoomList.Values)
        {
            RoomListItem item = Instantiate(roomListItemPrefab, roomListParent);

            item.roomNameText.text = room.Name;
            item.playerCount.text = room.PlayerCount + "/" + room.MaxPlayers;
            string roomName = room.Name;
            item.joinButton.onClick.AddListener(() => JoinRoom(roomName));

            roomListItems.Add(item);
        }
    }

    #endregion
}
