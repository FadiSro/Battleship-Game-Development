using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkControlar : MonoBehaviourPunCallbacks
{
    public void ConnectToServer()
    {
        PhotonNetwork.OfflineMode = false;
        
        PhotonNetwork.AutomaticallySyncScene = true; // Ensures all players load the same scene
        PhotonNetwork.ConnectUsingSettings();/*photonnetwork.cs*/
        Debug.Log("Connecting to Photon...");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        JoinRandomRoom();
    }

    private void JoinRandomRoom()
    {
        Debug.Log("Attempting to join a random room...");
        PhotonNetwork.JoinRandomRoom();
    }
    #region JoinRandomFailed
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Failed to join a random room, creating a new room...");
        CreateRoom();
    }

    private void CreateRoom()
    {
        string roomName = "Room_" + Random.Range(1000, 10000); // Generate a random room name
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // Battleship is a 1v1 game, so limit to 2 players
            
        PhotonNetwork.CreateRoom(roomName, roomOptions);
        Debug.Log("Created a new room: " + roomName);
    }
    #endregion

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined a room. Waiting for another player...");
        GameManager.Instance.PlayerTurn = PhotonNetwork.IsMasterClient;
        // Start the game if both players are in the room
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            StartGame();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Another player joined the room.");

        // Start the game as soon as the second player joins
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        Debug.Log("Starting the game...");
        if (PhotonNetwork.IsMasterClient)
            GameManager.Instance.PlayerTurn = true;
        else
            GameManager.Instance.PlayerTurn = false;
        GameManager.Instance.CurrentGameState = GameState.WaitingForPlayers;
        GameManager.Instance.CurrentGameMode = GameMode.PvP;
        GameManager.Instance.StartCoroutine(GameManager.Instance.SetupGameManagersCoroutine());
        PhotonNetwork.LoadLevel(1); // Ensure all players load the game board scene
    }

    public void DisconnectFromServer()
    {
        PhotonNetwork.OfflineMode = true;
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            Debug.Log("Disconnected from Photon.");
        }
    }
}
