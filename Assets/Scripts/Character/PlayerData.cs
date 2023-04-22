using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class PlayerData : Singleton<PlayerData>
{

    [SerializeField] private PlayTypes playType;
    #region Set Get
    public PlayTypes PlayType
    {
        get => playType;
        set => playType = value;
    }

    public void SetPlayerData(int _index)
    {
        playType = (PlayTypes)_index;

        Debug.Log(master() + " play type " + (PlayTypes)_index);
    }

    public bool master() => PhotonNetwork.LocalPlayer.IsMasterClient;

    public int actorNum(Player player = null)
    {
        if (player == null)
        {
            player = PhotonNetwork.LocalPlayer;
        }
        return player.ActorNumber;
    }

    public void destroy(List<GameObject> GO)
    {
        for (int i = 0; i < GO.Count; i++)
        {
            PhotonNetwork.Destroy(GO[i]);
        }
    }

    public void SetPos(Transform Tr, Vector3 target)
    {
        Tr.position = target;
    }

    public void SetTag(string key, object value, Player player = null)
    {
        if (player == null)
        {
            player = PhotonNetwork.LocalPlayer;
        }
        player.SetCustomProperties(new Hashtable { { key, value } });
    }

    public object GetTag(Player player, string key)
    {
        if (player.CustomProperties[key] == null)
        {
            return null;
        }
        return player.CustomProperties[key].ToString();
    }

    public bool AllhasTag(string key)
    {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            if (PhotonNetwork.PlayerList[i].CustomProperties[key] == null)
            {
                return false;
            }
        }
        return true;
    }
    #endregion
}
