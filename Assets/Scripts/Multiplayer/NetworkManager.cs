using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private byte maxPlayer = 10;

    void Start()
    {
        MenuManager.instance.OpenMenu("loading");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby(); // Можна опустити, якщо не потрібен лоббі UI
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        MenuManager.instance.OpenMenu("title");
        MenuManager.instance.CloseMenu("loading");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.LoadLevel(1);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Create Room Failed: " + message);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Join Random Failed: " + message);
        CreateRoom(); // створюємо кімнату, якщо не знайшло
    }

    public void SearchRoom()
    {
        if (PhotonNetwork.NetworkClientState != ClientState.JoinedLobby)
        {
            Debug.LogWarning("Still connecting to lobby...");
            return;
        }

        PhotonNetwork.JoinRandomRoom(); 
        MenuManager.instance.OpenMenu("Find");
    }

    private void CreateRoom()
    {
        string roomName = "Room_" + Random.Range(1000, 9999); // випадкове ім’я
        RoomOptions roomOptions = new RoomOptions
        {
            IsVisible = true,
            IsOpen = true,
            MaxPlayers = maxPlayer
        };
        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
    }
}