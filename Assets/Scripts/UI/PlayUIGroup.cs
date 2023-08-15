using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CommonFunctions;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayUIGroup : MonoBehaviourPun
{
    public static PlayUIGroup Instance = null;

    private PhotonView pv;

    private CanvasGroup backgroud_cg;

    private Transform select_playType_tr;
    private Button[] playType_btn;

    private Transform redCard_tr;
    private Button[] redCard_btn;
    private Image[] redCard_img;

    private Transform greenCard_tr;
    private Button[] greenCard_btn;
    private Image[] greenCard_img;

    private Transform jackImage_tr;
    private Image jack_img;

    private Button move_btn;
    private Button skill_btn;

    private int[] redCard_index;                    //알리바이 카드 순서
    public int[] RedCard_Index
    {
        get => redCard_index;
        set => redCard_index = value;
    }

    private int[] greenCard_index;                  //캐릭터 카드 순서    
    public int[] GreenCard_Index
    {
        get => greenCard_index;
        set => greenCard_index = value;
    }

    public string selectChar_name;
    public string SelectChar_Name
    {
        get => selectChar_name;
        set => selectChar_name = value;
    }

    private int count = 0;                          //버튼 클릭된 개수 (플레이어 범인, 경찰 카드 등)
    private int max_move;                           //선택한 캐릭터의 최대 이동 가능한 수

    private bool redCard_open = false;
    private bool greenCard_open = false;

    private bool move_done;                         //캐릭터 이동 완료했는지
    public bool Move_Done
    {
        get => move_done;
        set => move_done = value;
    }

    private bool skill_done;                        //캐릭터 스킬 사용 했는지
    public bool Skill_Done
    {
        get => skill_done;
        set => skill_done = value;
    }

    [SerializeField] private Sprite[] innocentCard_img;
    [SerializeField] private Sprite[] characterCard_img;
    [SerializeField] private Sprite[] jackCard_img;         //0 exposed 1 unexposed
    [SerializeField] private CharacterInfo[] charInfo;

    [SerializeField] private Sprite[] playType_img;         //0 cop,  1 jack
    [SerializeField] public AnimationCurve curve;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        pv = GetComponent<PhotonView>();

        backgroud_cg = transform.FindGameObject<CanvasGroup>("Background_Black");
        backgroud_cg.alpha = 0f;
        backgroud_cg.blocksRaycasts = false;

        select_playType_tr = transform.FindGameObject("Select_PlayerType").transform;
        select_playType_tr.localScale = Vector3.zero;

        playType_btn = new Button[2];
        for (int i = 0; i < playType_btn.Length; i++)
        {
            playType_btn[i] = transform.FindGameObject<Button>("PlayerType_" + i);
            playType_btn[i].interactable = false;
        }

        redCard_tr = transform.FindGameObject("RedCardGroup").transform;
        redCard_tr.localScale = Vector3.zero;

        redCard_btn = new Button[8];
        redCard_img = new Image[8];
        redCard_index = new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = 0; i < redCard_btn.Length; i++)
        {
            redCard_btn[i] = redCard_tr.FindGameObject<Button>("Card_" + i);
            redCard_btn[i].interactable = false;
            redCard_img[i] = redCard_btn[i].transform.GetChild(0).GetComponent<Image>();
            redCard_img[i].color = new Vector4(1f, 1f, 1f, 0);
        }

        greenCard_tr = transform.FindGameObject("GreenCardGroup").transform;
        greenCard_tr.localScale = Vector3.zero;

        greenCard_btn = new Button[4];
        greenCard_img = new Image[4];
        greenCard_index = new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = 0; i < greenCard_btn.Length; i++)
        {
            greenCard_btn[i] = greenCard_tr.FindGameObject<Button>("Card_" + i);
            greenCard_btn[i].interactable = false;
            greenCard_img[i] = greenCard_btn[i].transform.GetComponent<Image>();
        }

        jackImage_tr = transform.FindGameObject<Transform>("JackCardGroup");
        jack_img = transform.FindGameObject<Image>("JackImage");
        jackImage_tr.localScale = Vector3.zero;

        Transform temp_tr = FindObjectOfType<UIGroup>().transform;
        move_btn = temp_tr.transform.FindGameObject<Button>("MoveButton");
        skill_btn = temp_tr.transform.FindGameObject<Button>("SkillButton");

        move_btn.interactable = false;
        skill_btn.interactable = false;
    }

    #region jack 목격되었는지 표시
    public void SetJackImage()
    {
        StartCoroutine(IESetJackImage());
    }

    IEnumerator IESetJackImage()
    {
        bool temp_expoesd = GameManager.Instance.Jack_Exposed;
        int exposed_index = temp_expoesd == true ? 0 : 1;
        jack_img.sprite = jackCard_img[exposed_index];

        yield return StartCoroutine(jackImage_tr.IESetScale(curve, Vector3.one, 5f));
        yield return new WaitForSeconds(2.5f);
        yield return StartCoroutine(jackImage_tr.IESetScale(curve, Vector3.zero, 2.5f));

        GameManager.Instance.gameState = GameManager.GameState.DONE;
        GameManager.Instance.IsPlaying = true;

    }
    #endregion

    #region 플레이 타입 선택
    public void SetPlayTypeButton()
    {
        StartCoroutine(IEPlayType());
    }

    IEnumerator IEPlayType()
    {
        //까만 배경
        yield return StartCoroutine(backgroud_cg.IEAlpha(curve, true, 3f));

        //버튼 팝업
        yield return StartCoroutine(select_playType_tr.transform.IESetScale(curve, Vector3.one, 5f));

        //버튼 활성화
        for (int i = 0; i < playType_btn.Length; i++)
        {
            playType_btn[i].interactable = true;
        }
        yield break;
    }

    /// <summary>
    /// 경찰 또는 범인 카드 클릭시
    /// </summary>
    public void OnClickPlayType(int _index)
    {
        playType_btn[_index].transform.GetComponentInChildren<TextMeshProUGUI>().text = GameManager.Instance.PlayType.ToString();
        playType_btn[_index].GetComponent<Image>().sprite = playType_img[(int)GameManager.Instance.PlayType];

        pv.RPC(nameof(SyncTypeCard), RpcTarget.AllBufferedViaServer, _index);
    }

    /// <summary>
    /// 다른 플레이어가 선택한 버튼은 비활성화
    /// </summary>
    [PunRPC]
    public void SyncTypeCard(int _index)
    {
        playType_btn[_index].interactable = false;

        count++;
        if (count >= 2)
        {
            backgroud_cg.blocksRaycasts = false;         //선택 끝나면 true(다른 UI 클릭 가능하도록)

            //플레이어 타입 고르기 끝
            StartCoroutine(IEDoneType());
        }
    }

    IEnumerator IEDoneType()
    {
        //까만 배경   
        yield return StartCoroutine(backgroud_cg.IEAlpha(curve, false, 3f));

        //버튼 팝업 닫기
        yield return StartCoroutine(select_playType_tr.IESetScale(curve, Vector3.zero, 2.5f));

        if (PhotonNetwork.IsMasterClient == true)
        {
            ShuffleArray(redCard_index);

            Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
            string temp_key_red;

            for (int i = 0; i < redCard_index.Length; i++)
            {
                temp_key_red = "RedCard" + i;
                hash[temp_key_red] = redCard_index[i];
            }
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }

        GameManager.Instance.gameState = GameManager.GameState.PLAYTYPE;
        GameManager.Instance.IsPlaying = true;

        yield break;
    }
    #endregion

    #region Red Card(알리바이 카드)   
    /// <summary>
    /// 범인 정할 때, 셜록 카드 능력 쓸 때
    /// </summary>
    public void SetRedCard()
    {
        StartCoroutine(IERedCard());
    }

    [PunRPC]
    private void SetRedCardRPC()
    {
        StartCoroutine(IERedCard());
    }

    IEnumerator IERedCard()
    {
        if (GameManager.Instance.gameState == GameManager.GameState.PLAYTYPE)
        {
            //yield return new WaitUntil(() => (bool)PhotonNetwork.CurrentRoom.CustomProperties[CommonFuncs.redReceive] == true);  //상대 플레이어에 값이 변경된게 전달될 텀 주기

            Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
            string temp_key_red;
            int temp_index;

            //카드에 랜덤으로 섞은 카드 이미지, 정보 넣기
            for (int i = 0; i < redCard_index.Length; i++)
            {
                temp_key_red = "RedCard" + i;
                temp_index = (int)hash[temp_key_red];

                redCard_index[i] = temp_index;
                redCard_img[i].sprite = innocentCard_img[redCard_index[i]];
                redCard_btn[i].GetComponent<CharaterCard>().CharInfo = charInfo[redCard_index[i]];
            }
        }

        //까만 배경
        yield return StartCoroutine(backgroud_cg.IEAlpha(curve, true, 3f));

        //버튼 팝업
        yield return StartCoroutine(redCard_tr.IESetScale(curve, Vector3.one, 5f));

        if (GameManager.Instance.gameState == GameManager.GameState.PLAYTYPE)     //플레이어 정하기일때
        {
            if (GameManager.Instance.PlayType == PlayTypes.JACK)
            {
                for (int i = 0; i < redCard_btn.Length; i++)
                {
                    redCard_btn[i].interactable = true;
                }
            }
        }
        else                                                                        //셜록 카드 스킬
        {
            bool myTurn = GameManager.Instance.MyTurn;
            for (int i = 0; i < redCard_btn.Length; i++)
            {
                redCard_btn[i].interactable = myTurn;

                if (myTurn == true)
                {
                    bool temp_select = redCard_btn[i].GetComponent<CharaterCard>().IsSelected;
                    redCard_btn[i].interactable = !temp_select;
                }
            }
        }

        redCard_open = true;
        yield break;
    }

    public void OnClickRedCard(int _index)
    {
        //선택했던 카드는 클릭 안되도록
        if (redCard_btn[_index].GetComponent<CharaterCard>().IsSelected == true)
        {
            return;
        }

        pv.RPC(nameof(SyncRedCard), RpcTarget.AllBufferedViaServer, _index);

        //클릭한 사람은 카드 볼수 있게
        redCard_img[_index].color = new Vector4(1f, 1f, 1f, 1f);

        int temp_index = redCard_btn[_index].GetComponent<CharaterCard>().CharInfo.CharacterIndex;

        if (GameManager.Instance.gameState == GameManager.GameState.PLAYTYPE)
        {
            UIGroup.Instance.SetJackImage(temp_index);
        }
        else
        {
            UIGroup.Instance.SetRedCardPopup(temp_index);
        }
    }

    [PunRPC]
    public void SyncRedCard(int _index)
    {
        redCard_open = false;

        redCard_btn[_index].interactable = false;
        redCard_btn[_index].GetComponent<CharaterCard>().IsSelected = true;

        //범인 정보 GameManager에 저장
        if (GameManager.Instance.gameState == GameManager.GameState.PLAYTYPE)
        {
            string temp_name = redCard_btn[_index].GetComponent<CharaterCard>().CharInfo.CharacterName;
            GameManager.Instance.Jack_Name = temp_name;
        }
        StartCoroutine(IEDoneRedCard());
    }

    IEnumerator IEDoneRedCard()
    {
        //까만 배경 
        yield return StartCoroutine(backgroud_cg.IEAlpha(curve, false, 3f));

        //버튼 팝업 닫기
        yield return StartCoroutine(redCard_tr.transform.IESetScale(curve, Vector3.zero, 2.5f));

        GameManager.Instance.IsPlaying = true;

        if (GameManager.Instance.gameState == GameManager.GameState.CARDACTION)
        {
            backgroud_cg.blocksRaycasts = false;                                    //선택 끝나면 true(다른 UI 클릭 가능하도록)
            GameManager.Instance.gameState = GameManager.GameState.CHECK;           //셜록 카드 스킬 썼을 때   
        }
        else
        {
            backgroud_cg.blocksRaycasts = true;
            GameManager.Instance.gameState = GameManager.GameState.SELECT;
        }

        yield break;
    }
    #endregion

    #region Green Card(캐릭터 이동 카드)
    public void SetGreenCard()
    {
        Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        string temp_key_green;

        //라운드 시작이면 카드 셔플, 홀수 라운드 마다 셔플
        int round = GameManager.Instance.Round_Cur;
        int turn = GameManager.Instance.Turn_Cur;
        if (round % 2 == 1 && turn == 1)
        {
            if (PhotonNetwork.IsMasterClient == true)
            {
                ShuffleArray(greenCard_index);

                for (int i = 0; i < greenCard_index.Length; i++)
                {
                    temp_key_green = "GreenCard" + i;
                    hash[temp_key_green] = greenCard_index[i];
                }
                hash[CommonFuncs.greenReceive] = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
            }
        }

        backgroud_cg.blocksRaycasts = true;
        StartCoroutine(IEGreenCard());
    }

    IEnumerator IEGreenCard()
    {
        Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        string temp_key_green = string.Empty;

        //라운드 시작이면 카드 셔플, 홀수 라운드 마다 셔플
        int round = GameManager.Instance.Round_Cur;
        int turn = GameManager.Instance.Turn_Cur;
        if (round % 2 == 1 && turn == 1)
        {
            yield return new WaitUntil(() => (bool)PhotonNetwork.CurrentRoom.CustomProperties[CommonFuncs.greenReceive] == true);  //상대 플레이어에 값이 변경된게 전달될 텀 주기

            for (int i = 0; i < greenCard_index.Length; i++)
            {
                temp_key_green = "GreenCard" + i;
                greenCard_index[i] = (int)hash[temp_key_green];
            }
        }

        //카드에 랜덤으로 섞은 카드 이미지, 정보 넣기
        if (turn == 1)
        {
            for (int i = 0; i < greenCard_btn.Length; i++)
            {
                int temp_greenCard = i;
                if (round % 2 == 0)
                {
                    temp_greenCard = i + 4;           //홀수 라운드는 0 ~ 3, 짝수는 나머지
                }

                greenCard_img[i].sprite = innocentCard_img[greenCard_index[temp_greenCard]];
                greenCard_btn[i].GetComponent<CharaterCard>().CharInfo = charInfo[greenCard_index[temp_greenCard]];
                greenCard_btn[i].GetComponent<CharaterCard>().IsSelected = false;

                UIGroup.Instance.SetGreenCardPopup(greenCard_index[temp_greenCard], i);       //ui 팝업 이미지 교체하기
            }
        }

        //버튼 활성화(현재 차례에 따라)
        bool myTurn = GameManager.Instance.MyTurn;
        for (int i = 0; i < greenCard_btn.Length; i++)
        {
            greenCard_btn[i].interactable = myTurn;
            if (greenCard_btn[i].GetComponent<CharaterCard>().IsSelected == true)   //이미 클릭했던 카드일 경우
            {
                greenCard_btn[i].interactable = false;
            }
        }

        //까만 배경
        yield return StartCoroutine(backgroud_cg.IEAlpha(curve, true, 3f));

        //버튼 팝업
        yield return StartCoroutine(greenCard_tr.IESetScale(curve, Vector3.one, 5f));

        //이동&스킬 완료 초기화
        move_done = false;
        skill_done = false;

        greenCard_open = true;
        yield break;
    }

    public void OnClickGreenCard(int _index)
    {
        bool myTurn = GameManager.Instance.MyTurn;
        if (myTurn == false)
        {
            return;
        }

        if (greenCard_btn[_index].GetComponent<CharaterCard>().IsSelected == true)
        {
            return;
        }

        //이동할 캐릭터 동기화 위해 이름 저장
        selectChar_name = greenCard_btn[_index].GetComponent<CharaterCard>().CharInfo.CharacterName;

        Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        hash[CommonFuncs.selectCharReceive] = selectChar_name;

        hash[CommonFuncs.greenReceive] = false;                     //초기화
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

        pv.RPC(nameof(SyncGreenCard), RpcTarget.AllBufferedViaServer, _index);
    }

    [PunRPC]
    public void SyncGreenCard(int _index)
    {
        greenCard_open = false;

        greenCard_btn[_index].interactable = false;
        greenCard_btn[_index].GetComponent<CharaterCard>().IsSelected = true;

        //캐릭터의 최대 이동 가능한 칸수
        max_move = greenCard_btn[_index].GetComponent<CharaterCard>().CharInfo.MaxMove;
        SkillOrder order = greenCard_btn[_index].GetComponent<CharaterCard>().CharInfo.Order;

        bool myTurn = GameManager.Instance.MyTurn;
        Hashtable hash = PhotonNetwork.CurrentRoom.CustomProperties;
        if (myTurn == false)
        {
            selectChar_name = (string)hash[CommonFuncs.selectCharReceive];

            if (order == SkillOrder.NONE)
            {
                skill_done = true;
            }
        }
        else
        {
            switch (order)              //캐릭터 이동, 스킬 버튼 활성화
            {
                case SkillOrder.NONE:
                    skill_done = true;
                    pv.RPC(nameof(SycnReadyTile), RpcTarget.AllBufferedViaServer);
                    break;
                case SkillOrder.After:
                    pv.RPC(nameof(SycnReadyTile), RpcTarget.AllBufferedViaServer);
                    break;
                case SkillOrder.Both:
                case SkillOrder.Select:
                    move_btn.interactable = true;
                    skill_btn.interactable = true;
                    break;
                default:
                    break;
            }
        }

        StartCoroutine(IEDoneGreenCard());
    }

    IEnumerator IEDoneGreenCard()
    {
        //까만 배경 
        yield return StartCoroutine(backgroud_cg.IEAlpha(curve, false, 3f));

        //버튼 팝업 닫기
        yield return StartCoroutine(greenCard_tr.transform.IESetScale(curve, Vector3.zero, 2.5f));

        backgroud_cg.blocksRaycasts = false;         //선택 끝나면 true(다른 UI 클릭 가능하도록)

        GameManager.Instance.IsPlaying = true;
        GameManager.Instance.gameState = GameManager.GameState.CARDACTION;

        yield break;
    }
    #endregion

    #region 캐릭터 이동 & 스킬 사용
    /// <summary>
    /// 이동 가능한 타일 표시하기 (이동하기 버튼 누르거나, 스킬을 나중에 사용하는 캐릭터는 카드 선택시 호출)
    /// </summary>
    [PunRPC]
    private void SycnReadyTile()
    {
        if (selectChar_name == "Gull")
        {
            skill_done = true;
        }

        TileManager.Instance.ReadyCharacterTile(max_move);
    }

    /// <summary>
    /// 캐릭터 이동하기 버튼
    /// </summary>
    public void OnClickMove()
    {
        move_btn.interactable = false;
        skill_btn.interactable = false;

        pv.RPC(nameof(SycnReadyTile), RpcTarget.AllBufferedViaServer);
    }

    /// <summary>
    /// 스킬 사용하기 버튼, 이동완료 시 스킬 사용으로
    /// </summary>
    public void OnClickSkill()
    {
        move_btn.interactable = false;
        skill_btn.interactable = false;

        switch (selectChar_name)
        {
            case "Lestrade":                //경찰 저지선 이동
            case "Bert":                    //맨홀 타일 이동
            case "Smith":                   //가로등 타일 이동
            case "Goodley":                 //다른 캐릭터 자기쪽으로 이동시키기
                TileManager.Instance.SyncCharSkill();
                break;
            case "Gull":                   //다른 캐릭터와 위치 변경  (이동 안하게됨)
                move_done = true;
                TileManager.Instance.SyncCharSkill();
                break;
            case "Watson":                  //x  손전등 방향 가리키기                    
                TileManager.Instance.SyncCharSkill();
                break;
            case "Stealthy":                //x  스킬이 이동에 포함되어있음
                break;
            case "Holmes":                  //x  알리바이 카드 선택하기
                pv.RPC(nameof(SetRedCardRPC), RpcTarget.AllBufferedViaServer);
                break;
        }
    }

    /// <summary>
    /// 스킬, 이동 완료 되었는지에 따라서
    /// </summary>
    public void CheckCardActionState()
    {
        bool temp_turn = GameManager.Instance.MyTurn;
        if (move_done == false)
        {
            OnClickMove();          //이동하기
        }

        if (skill_done == false && temp_turn == true)
        {
            OnClickSkill();         //스킬 사용하기
        }

        //이동, 스킬 완료 상태
        if (move_done == true && skill_done == true)
        {
            GameManager.Instance.gameState = GameManager.GameState.CHECK;        //move_done, skill_done이 양쪽 플레이어가 같아야함
            GameManager.Instance.IsPlaying = true;
        }
    }
    #endregion

    /// <summary>
    /// 카드 선택 중 ui 누르면 내려가기
    /// </summary>
    /// param name="_ui" true : ui 버튼 클릭(ui 열림)
    public void UIPopupCheck(bool on)
    {
        StartCoroutine(backgroud_cg.IEAlpha(curve, on, 5f));

        Vector3 uiSize = on == false ? Vector3.zero : Vector3.one;
        if (redCard_open == true)
        {
            StartCoroutine(redCard_tr.transform.IESetScale(curve, uiSize, 5f));
        }
        else if (greenCard_open == true)
        {
            StartCoroutine(greenCard_tr.transform.IESetScale(curve, uiSize, 5f));
        }
    }

    /// <summary>
    /// 배열 랜덤으로 섞기
    /// 클라이언트에서만 실행 후 값 전달
    /// </summary>
    private void ShuffleArray(int[] _array)
    {
        var random = new System.Random();
        random.Shuffle(_array);
    }
}