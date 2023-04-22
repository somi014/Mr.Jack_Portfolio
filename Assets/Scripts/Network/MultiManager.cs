using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using static GameManager;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using CommonFunctions;

public class MultiManager : MonoBehaviourPunCallbacks
{
    PhotonView pv;

    bool received;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();

        received = false;

        Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        hash[CommonFuncs.redReceive] = false;
        hash[CommonFuncs.greenReceive] = false;
        hash[CommonFuncs.selectCharReceive] = "null";
        hash[CommonFuncs.checkRecive] = false;

        string temp_key_red;
        string temp_key_green;
        for (int i = 0; i < 8; i++)
        {
            temp_key_red = "RedCard" + i;
            hash[temp_key_red] = i;

            temp_key_green = "GreenCard" + i;
            hash[temp_key_green] = i;
        }
    }


    /// <summary>
    /// 뒤에 들어온 플레이어가 있을 경우
    /// </summary>
    /// <param name="newPlayer"></param>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.Log("networkManager enter room " + newPlayer.UserId);

        if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers &&
          PhotonNetwork.IsMasterClient == true)
        {
            int temp_rand = Random.Range(0, 2);
            Instance.pv.RPC(nameof(Instance.SetPlayType), RpcTarget.AllBufferedViaServer, temp_rand);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        if (!PhotonNetwork.IsMasterClient)
            return;
        if (!changedProps.ContainsKey("Word"))
            return;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        if (SceneManagerHelper.ActiveSceneName == "MainScene")
        {
            PhotonNetwork.LoadLevel("TitleScene");
        }
    }

}
