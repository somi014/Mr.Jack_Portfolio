using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using CommonFunctions;

public enum PlayTypes
{
    COP,
    JACK,
    NONE
}

public class GameManager : MonoBehaviourPun
{
    public static GameManager Instance = null;

    [SerializeField] CanvasGroup state_cg;
    [SerializeField] CanvasGroup loading_cg;
    [SerializeField] TextMeshProUGUI state_txt;

    [SerializeField] UIGroup uiGroup;
    [SerializeField] PlayUIGroup playUIGroup;
    [SerializeField] Button lobby_btn;

    [SerializeField] public AnimationCurve curve;

    public PhotonView pv;

    public string userNickName;

    public enum GameState
    {
        START,
        PLAYTYPE,
        SELECT,
        CARDACTION,
        CHECK,
        DONE
    }

    public GameState gameState;
    private bool isPlaying = false;          //각 state 상태가 진행 중인지( 선택중, 이동 중 등)
    public bool IsPlaying
    {
        get => isPlaying;
        set => isPlaying = value;
    }

    private int round_cur;
    public int Round_Cur
    {
        get => round_cur;
        set => round_cur = value;
    }

    private int turn_cur;
    public int Turn_Cur
    {
        get => turn_cur;
        set => turn_cur = value;
    }

    public readonly int round_max = 4;      //임시
    public readonly int turn_max = 4;

    public int mapType_index = 0;                   //현재 선택해야하는 타일맵의 종류(ex. 가로등 선택)

    [SerializeField] private PlayTypes playType;    //내 플레이 타입(범인, 경찰)
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

    [SerializeField] private string jack_name;
    public string Jack_Name
    {
        get => jack_name;
        set => jack_name = value;
    }

    [SerializeField] private bool jack_exposed;
    public bool Jack_Exposed
    {
        get => jack_exposed;
        set => jack_exposed = value;
    }

    private bool myTurn = false;                        //내 차례인지 
    public bool MyTurn
    {
        get => myTurn;
        set => myTurn = value;
    }

    List<PlayTypes> odd_turn = new List<PlayTypes> { PlayTypes.COP, PlayTypes.JACK, PlayTypes.JACK, PlayTypes.COP };    //홀수 턴
    List<PlayTypes> even_turn = new List<PlayTypes> { PlayTypes.JACK, PlayTypes.COP, PlayTypes.COP, PlayTypes.JACK };   //짝수 턴

    public bool master() => PhotonNetwork.LocalPlayer.IsMasterClient;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
#if PLATFORM_STANDALONE_WIN
        Screen.SetResolution(1280, 720, false);
#elif UNITY_ANDROID || UNITY_IOS
         Screen.SetResolution(Screen.width, (Screen.width * 16) / 9, true);
#endif
        PhotonNetwork.AutomaticallySyncScene = true;

        pv = GetComponent<PhotonView>();

        loading_cg.alpha = 1f;          //플레이 인원 다 들어올때까지 활성화

        playType = PlayTypes.NONE;      //정해지기전은 아무것도 아닌 상태로

        round_cur = 1;
        turn_cur = 0;

        isPlaying = false;

        lobby_btn.gameObject.SetActive(false);
    }

    #region 플레이어 타입 선택
    [PunRPC]
    public void SetPlayType(int _type)
    {
        if (PhotonNetwork.IsMasterClient == true)
        {
            playType = (PlayTypes)_type;
        }
        else
        {
            playType = (PlayTypes)Mathf.Abs(_type - 1);
        }

        StartCoroutine(IEStateUpdate());
        isPlaying = true;
    }
    #endregion

    public void ChangeMapType(int _index)
    {
        mapType_index = _index;
    }

    #region 다음 라운드로 이동
    public void NextRound()
    {
        turn_cur++;
        if (Turn_Cur > turn_max)
        {
            turn_cur = 1;
            round_cur++;
        }
        uiGroup.SetRoundText(round_cur);

        if (round_cur % 2 == 1)   //홀수 라운드
        {
            myTurn = playType == odd_turn[turn_cur - 1];
        }
        else
        {
            myTurn = playType == even_turn[turn_cur - 1];
        }
        Debug.Log("next round " + round_cur + " turn " + turn_cur);
    }

    #endregion

    /// <summary>
    /// 게임 상태 업데이트
    /// </summary>
    /// <returns></returns>
    IEnumerator IEStateUpdate()
    {
        //대기 화면 알파 0
        yield return StartCoroutine(loading_cg.IEAlpha(curve, false, 3f));

        while (true)
        {
            yield return new WaitUntil(() => isPlaying == true);

            if (gameState == GameState.START)
            {
                NextRound();

                if (round_cur == 1 && turn_cur <= 1)
                {
                    state_txt.text = "카드를 하나 선택해 경찰 또는 범인을 선택하세요";
                    yield return StartCoroutine(IEStateTextUpdate());

                    playUIGroup.SetPlayTypeButton();        //첫 라운드 시작 전 타입 선택하기...
                    isPlaying = false;
                }
                else
                {
                    state_txt.text = "라운드 " + round_cur + "\n차례 " + turn_cur + " 번째 턴";     //범인인지 경찰인지 !!!
                    yield return StartCoroutine(IEStateTextUpdate());

                    gameState = GameState.SELECT;
                    isPlaying = true;
                }
            }
            else if (gameState == GameState.PLAYTYPE)
            {
                state_txt.text = "잭이 될 캐릭터를 선택해 주세요";
                yield return StartCoroutine(IEStateTextUpdate());

                playUIGroup.SetRedCard();
                isPlaying = false;
            }
            else if (gameState == GameState.SELECT)
            {
                state_txt.text = "이동할 캐릭터를 선택해 주세요";
                yield return StartCoroutine(IEStateTextUpdate());

                //초록 카드 캐릭터(이동) 선택하기
                playUIGroup.SetGreenCard();

                isPlaying = false;
            }
            else if (gameState == GameState.CARDACTION)
            {
                isPlaying = false;

            }
            else if (gameState == GameState.CHECK)
            {
                if (round_cur <= 4 && turn_cur == turn_max)
                {
                    state_txt.text = "목격된 캐릭터 확인 & 가로등 제거";
                }
                else
                {
                    state_txt.text = "목격된 캐릭터 확인";
                }
                yield return StartCoroutine(IEStateTextUpdate());

                //가로등 제거
                if (round_cur <= 4 && turn_cur == turn_max)
                {
                    TileManager.Instance.DeleteLight();     
                }

                //캐릭터 노출되었는지
                TileManager.Instance.CheckExposed();

                playUIGroup.SetJackImage();
                uiGroup.SetJackExposed(jack_exposed);

                isPlaying = false;
            }
            else if (gameState == GameState.DONE)
            {
                if (turn_cur <= 4)
                {
                    gameState = GameState.START;
                    isPlaying = true;
                }
                else
                {
                    if (round_cur == round_max)
                    {
                        //잭 승리 (끝날때까지 잡히지 않았음)
                        GameOver(true);

                        break;
                    }
                    else
                    {
                        state_txt.text = "턴 종료";
                        yield return StartCoroutine(IEStateTextUpdate());

                        isPlaying = false;
                    }
                }
            }
        }
    }


    #region 게임 진행 텍스트 표시
    private IEnumerator IEStateTextUpdate()
    {
        state_cg.blocksRaycasts = true;
        yield return StartCoroutine(state_cg.IEAlpha(curve, true, 3f));

        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(state_cg.IEAlpha(curve, false, 3f));

        state_cg.blocksRaycasts = false;

        yield break;
    }
    #endregion

    public void GameOver(bool _jackWin)
    {
        if (_jackWin == true)
        {
            //잭 승리
            state_txt.text = "잭 승리 \n잭 : " + jack_name;
        }
        else
        {
            state_txt.text = "경찰 승리 \n잭 : " + jack_name;
        }

        state_cg.blocksRaycasts = true;
        StartCoroutine(state_cg.IEAlpha(curve, true, 3f));

        //타이틀로 가기 버튼 활성화
        lobby_btn.gameObject.SetActive(true);
    }

    public void OnClickLobby()
    {
        pv.RPC(nameof(MoveLobby), RpcTarget.AllBufferedViaServer);
    }

    [PunRPC]
    private void MoveLobby()
    {
        Debug.Log("move lobby");
        PhotonNetwork.LeaveRoom();
    }

}
