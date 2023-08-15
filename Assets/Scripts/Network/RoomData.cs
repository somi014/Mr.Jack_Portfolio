using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class RoomData : MonoBehaviour
{
    private TextMeshProUGUI roomName_txt;
    private RoomInfo roomInfo;

    private NetworkManager networkManager;

    public RoomInfo RoomInfo
    {
        get
        {
            return roomInfo;
        }
        set
        {
            roomInfo = value;
            roomName_txt.text = $"{roomInfo.Name} ({roomInfo.PlayerCount}/{roomInfo.MaxPlayers})";
            GetComponent<Button>().onClick.AddListener(() => OnEnterRoom(roomInfo.Name));

        }
    }

    void Awake()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        roomName_txt = transform.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnEnterRoom(string roomName)
    {    
        RoomOptions option = new RoomOptions();
        option.IsOpen = true;
        option.IsVisible = true;
        option.MaxPlayers = 2;

        PhotonNetwork.NickName = networkManager.userIdInput.text;
        PhotonNetwork.JoinOrCreateRoom(roomName, option, TypedLobby.Default);
    }
}