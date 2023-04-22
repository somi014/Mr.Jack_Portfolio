using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    private readonly string gameVersion = "ver 1.0";
    private string userId = "user";

    public TextMeshProUGUI statusText;
    public TMP_InputField roomInput, userIdInput;

    Dictionary<string, GameObject> room_dictionary = new Dictionary<string, GameObject>();
    public GameObject room_prefab;
    public Transform content_tr;

    void Awake()
    {
        Screen.SetResolution(1280, 720, false);
        
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;

        PhotonNetwork.ConnectUsingSettings();           //서버 접속
    }

    private void Start()
    {
        Debug.Log("NetworkManager start");
        if (string.IsNullOrEmpty(PlayerPrefs.GetString("USER_ID")) == false)
        {
            userId = PlayerPrefs.GetString("USER_ID", $"USER_{Random.Range(0, 100):00}");
            userIdInput.text = userId;
            PhotonNetwork.NickName = userId;
        }             
    }
  
    public override void OnConnectedToMaster()
    {
        Debug.Log("서버 접속 완료");

        PhotonNetwork.JoinLobby();
    }

    public void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("연결끊김");
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비접속완료");
    }

    public void OnMakeRoomClick()
    {
        RoomOptions option = new RoomOptions();
        option.IsOpen = true;
        option.IsVisible = true;
        option.MaxPlayers = 2;

        if (string.IsNullOrEmpty(roomInput.text))
        {
            roomInput.text = $"ROOM_{Random.Range(1, 100):000}";
        }

        PhotonNetwork.CreateRoom(roomInput.text, option);
    }

    /// <summary>
    /// 룸 생성, 자동 입장
    /// </summary>
    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom(roomInput.text, new RoomOptions { MaxPlayers = 2 });
    }
    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(roomInput.text);
    }
    public void JoinOrCreateRoom()
    {
        PhotonNetwork.JoinOrCreateRoom(roomInput.text, new RoomOptions { MaxPlayers = 2 }, null);               
    }
    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }
    public override void OnCreatedRoom()
    {
        Debug.Log("방만들기완료");
    }
    public override void OnJoinedRoom()
    {
        Debug.Log("방참가완료");

        if (PhotonNetwork.IsMasterClient == true)
        {
            PhotonNetwork.LoadLevel("MainScene");
        }
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("방만들기실패");
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("방참가실패");
    }
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("방랜덤참가실패");

        RoomOptions temp_room = new RoomOptions();
        temp_room.IsOpen = true;
        temp_room.IsVisible = true;
        temp_room.MaxPlayers = 2;

        roomInput.text = $"Room_{Random.Range(1, 100):000}";

        PhotonNetwork.CreateRoom(roomInput.text, temp_room);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("room list");
        base.OnRoomListUpdate(roomList);

        GameObject tempRoom = null;
        foreach (var room in roomList)
        {
            //룸이 삭제된 경우
            if (room.RemovedFromList == true)
            {
                room_dictionary.TryGetValue(room.Name, out tempRoom);
                Destroy(tempRoom);
                room_dictionary.Remove(room.Name);
            }
            else
            {
                //룸이 처음 생성된 경우
                if (room_dictionary.ContainsKey(room.Name) == false)
                {
                    GameObject clone = Instantiate(room_prefab, content_tr);
                    clone.GetComponent<RoomData>().RoomInfo = room;
                    room_dictionary.Add(room.Name, clone);
                }
                //룸 정보를 갱신하는 경우
                else
                {
                    room_dictionary.TryGetValue(room.Name, out tempRoom);
                    tempRoom.GetComponent<RoomData>().RoomInfo = room;
                }
            }
        }
    }


    //https://ojui.tistory.com/41
    public void OnRandomBtn()
    {
        if (string.IsNullOrEmpty(userIdInput.text))
        {
            userId = $"USER_{Random.Range(0, 100):00}";
            userIdInput.text = userId;
        }

        PlayerPrefs.SetString("USER_ID", userIdInput.text);
        PhotonNetwork.NickName = userIdInput.text;
        PhotonNetwork.JoinRandomRoom();
    }

    [ContextMenu("정보")]
    void Info()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("현재 방 이름 : " + PhotonNetwork.CurrentRoom.Name);
            Debug.Log("현재 방 인원수 : " + PhotonNetwork.CurrentRoom.PlayerCount);
            Debug.Log("현재 방 최대인원수 : " + PhotonNetwork.CurrentRoom.MaxPlayers);

            string playerStr = "방에 있는 플레이어 목록 : ";
            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            {
                playerStr += PhotonNetwork.PlayerList[i].NickName + ", ";
                Debug.Log(playerStr);
            }
        }
        else
        {
            Debug.Log("접속한 인원 수 : " + PhotonNetwork.CountOfPlayers);
            Debug.Log("방 개수 : " + PhotonNetwork.CountOfRooms);
            Debug.Log("모든 방에 있는 인원 수 : " + PhotonNetwork.CountOfPlayersInRooms);
            Debug.Log("로비에 있는지? : " + PhotonNetwork.InLobby);
            Debug.Log("연결됐는지? : " + PhotonNetwork.IsConnected);
        }
    }
}