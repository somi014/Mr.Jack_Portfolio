using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;
using System.Linq;
using CommonFunctions;

public class TileManager : MonoBehaviourPun
{
    public static TileManager Instance;

    Camera mainCamera;
    PhotonView pv;
    AStarFindPath findPath;

    public Dictionary<Vector3Int, TileInfo> dataOnTiles;        //전체 타일 
    public Dictionary<string, CharacterTile> characterTile;     //캐릭터 얼굴 UI
    List<Vector3Int> temp_neighborhoods;                        //이동가능한 위치 표시용

    Node[,] NodeArray;
    [SerializeField] List<Node> findList;                //Goodley로 가는 길
    Vector3Int findVec;

    [SerializeField] Tilemap tilemap;
    [SerializeField] Tilemap obstacle_tilemap;

    public GameObject text;             //테스트 후 지우기
    public Transform parent_test;       //테스트 후 지우기

    [SerializeField] Tile[] active_tile;
    [SerializeField] Tile[] unactive_tile;
    [SerializeField] Tile road_select_tile;
    [SerializeField] Tile[] light_select_tile;
    [SerializeField] Tile[] manhole_select_tile;
    [SerializeField] Tile[] policeLine_select_tile;

    [SerializeField] Transform characterTile_parent;
    [SerializeField] Transform exitText_tr;

    [SerializeField] Transform[] lightNumber_tr;

    private Vector3Int previousPos;     //캐릭터가 이동한 후 이전 위치의 정보 수정하기 위해

    private bool myTurn;
    private bool moveTouch;              //캐릭터 이동 타일 선택 가능한지
    private bool skillTouch;            //스킬 사용시 타일 터치 가능한지
    private bool goodleyMove;

    private int clickCount = 0;         //타일 바꾸기에서 선택한 타일 수
    private int maxMoveCount = 0;       //캐릭터가 최대 이동할 수 있는 칸 수
    private int leftMoveCount = 0;
    private int manholeClickCount = 0;

    private List<string> goodleySkill_name;

    #region 타일 상태 리스트
    Dictionary<string, Vector3Int> charPos_list = new Dictionary<string, Vector3Int>() {
        { "Bert", new Vector3Int(5, 8) }, { "Goodley", new Vector3Int(5, 12) }, {"Gull", new Vector3Int(8, 4) },{"Holmes", new Vector3Int(3, 6) },
        {"Lestrade", new Vector3Int(4, 4) }, {"Smith",new Vector3Int(6, 6) }, {"Stealthy", new Vector3Int(1, 8) }, {"Watson", new Vector3Int(4, 0) } };

    Dictionary<Vector3Int, bool> lightPos_list = new Dictionary<Vector3Int, bool>() {
        {new Vector3Int(2, 1), true }, { new Vector3Int(7, 2), true },  { new Vector3Int(7, 5), false }, {new Vector3Int(3, 5), true },
        {new Vector3Int(5, 7), true }, { new Vector3Int(1, 7), false }, { new Vector3Int(2, 10), true }, { new Vector3Int(6, 11), true } };


    //라운드 진행에 따라 비활성화할 가로등 리트스 총 4개
    List<Vector3Int> deleteLight_list = new List<Vector3Int>()
                                    { new Vector3Int(2, 1), new Vector3Int(6, 11), new Vector3Int(7, 2),new Vector3Int(2, 10) };

    List<(bool, Vector3Int)> manholePos_list = new List<(bool, Vector3Int)>() {
        (true, new Vector3Int(6, 0)), (false, new Vector3Int(2, 2)), (true, new Vector3Int(8, 5)), (true, new Vector3Int(5, 5)),
        (true, new Vector3Int(3, 7)), (true, new Vector3Int(0, 7)), (false, new Vector3Int(7, 11)), (true, new Vector3Int(4, 12))};

    Dictionary<Vector3Int, bool> policeLinePos_list = new Dictionary<Vector3Int, bool>()    {
        {new Vector3Int(0, 1), true}, {new Vector3Int(8, 1), false}, {new Vector3Int(0, 11), false}, {new Vector3Int(8, 11), true} };
    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        mainCamera = Camera.main;
        pv = GetComponent<PhotonView>();
        findPath = GetComponent<AStarFindPath>();

        dataOnTiles = new Dictionary<Vector3Int, TileInfo>();
        characterTile = new Dictionary<string, CharacterTile>();
        temp_neighborhoods = new List<Vector3Int>();

        string temp_name;
        for (int i = 0; i < characterTile_parent.childCount; i++)
        {
            temp_name = characterTile_parent.GetChild(i).name;
            string name = temp_name.Substring(0, temp_name.IndexOf('_'));
            characterTile[name] = characterTile_parent.GetChild(i).GetComponent<CharacterTile>();
            characterTile[name].Char_Name = name;
        }

        moveTouch = false;
        skillTouch = false;

        goodleySkill_name = new List<string>();

        exitText_tr.gameObject.SetActive(false);
    }

    private void Start()
    {
        SetTiles();
    }

    /// <summary>
    /// 타일맵의 각 타일의 정보 설정
    /// </summary>
    /// <param name="_tilemap"></param>
    void SetTiles()
    {
        NodeArray = new Node[10, 13];

        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos) == false)
            {
                continue;
            }
            var tile = tilemap.GetTile<TileBase>(pos);
            dataOnTiles[pos] = new TileInfo()
            {
                type = TilemapType.LOAD,
                tile_pos = pos,
                character_isOn = false,
                canTouch = false,
                isSelected = false,
                normalTiles = new Tile[2] { active_tile[0], unactive_tile[0] },
                selectTiles = new Tile[2] { road_select_tile, road_select_tile }        //로드 타일 다 넣어둠
            };
            dataOnTiles[pos].SetNeighborgoods();

            foreach (var value in charPos_list)
            {
                if (value.Value == pos)
                {
                    dataOnTiles[pos].character_isOn = true;
                    dataOnTiles[pos].character_name = value.Key;

                    Vector3 cellToWorld = tilemap.CellToWorld(pos);
                    var targetPos = mainCamera.WorldToScreenPoint(cellToWorld);
                    characterTile[value.Key].SetPos(targetPos);
                }
            }

            foreach (var value in lightPos_list)
            {
                if (value.Key == pos)
                {
                    dataOnTiles[pos].type = TilemapType.LIGHT;
                    dataOnTiles[pos].normalTiles[0] = active_tile[(int)(TilemapType.LIGHT)];
                    dataOnTiles[pos].normalTiles[1] = unactive_tile[(int)(TilemapType.LIGHT)];
                    dataOnTiles[pos].selectTiles[0] = light_select_tile[0];
                    dataOnTiles[pos].selectTiles[1] = light_select_tile[1];

                    dataOnTiles[pos].isActive = value.Value;
                    dataOnTiles[pos].light_num = -1;

                    tilemap.SetTile(pos, dataOnTiles[pos].normalTile);
                }
            }

            foreach (var value in manholePos_list)
            {
                if (value.Item2 == pos)
                {
                    dataOnTiles[pos].type = TilemapType.MANHOLE;
                    dataOnTiles[pos].normalTiles[0] = active_tile[(int)(TilemapType.MANHOLE)];
                    dataOnTiles[pos].normalTiles[1] = unactive_tile[(int)(TilemapType.MANHOLE)];
                    dataOnTiles[pos].selectTiles[0] = manhole_select_tile[0];
                    dataOnTiles[pos].selectTiles[1] = manhole_select_tile[1];

                    dataOnTiles[pos].isActive = value.Item1;

                    tilemap.SetTile(pos, dataOnTiles[pos].normalTile);
                }
            }

            foreach (var obstacle in obstacle_tilemap.cellBounds.allPositionsWithin)
            {
                if (obstacle_tilemap.HasTile(obstacle) == false)
                {
                    continue;
                }
                if (pos == obstacle)
                {
                    dataOnTiles[pos].type = TilemapType.OBSTACLE;
                }
            }

            foreach (var value in policeLinePos_list)
            {
                if (value.Key == pos)
                {
                    dataOnTiles[pos].type = TilemapType.POLICELINE;

                    dataOnTiles[pos].normalTiles[0] = active_tile[(int)(TilemapType.POLICELINE)];
                    dataOnTiles[pos].normalTiles[1] = unactive_tile[(int)(TilemapType.POLICELINE)];
                    dataOnTiles[pos].selectTiles[0] = policeLine_select_tile[0];
                    dataOnTiles[pos].selectTiles[1] = policeLine_select_tile[1];

                    dataOnTiles[pos].isActive = value.Value;

                    tilemap.SetTile(pos, dataOnTiles[pos].normalTile);
                }
            }

            bool temp_wall = dataOnTiles[pos].type == TilemapType.LIGHT || dataOnTiles[pos].type == TilemapType.OBSTACLE;
            if (pos.x >= 0 && pos.y >= 0)
            {
                NodeArray[pos.x, pos.y] = new Node(temp_wall, pos.x, pos.y);
            }

            ////테스트용 
            //GameObject clone = Instantiate(text);
            //clone.transform.parent = parent_test;
            //clone.transform.localScale = Vector3.one;

            //Vector3 worldPosition = tilemap.CellToWorld(pos);
            //Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            //clone.transform.position = screenPosition;
            //clone.GetComponent<TextMeshProUGUI>().text = "x : " + pos.x + "\ny : " + pos.y;
        }

        for (int i = 0; i < deleteLight_list.Count; i++)
        {
            Vector3 worldPosition = tilemap.CellToWorld(deleteLight_list[i]);
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            dataOnTiles[deleteLight_list[i]].light_num = i;
            lightNumber_tr[i].position = new Vector3(screenPosition.x + 50f, screenPosition.y, screenPosition.z);
        }

        findPath.SetNode(NodeArray);
    }

    private void Update()
    {
        myTurn = GameManager.Instance.MyTurn;
        if (myTurn == false)
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector3 pos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cell = tilemap.WorldToCell(new Vector3(pos.x, pos.y));

            Vector3 worldPosition = tilemap.CellToWorld(cell);
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            bool checkTile = CheckClickTile(cell);
            if (moveTouch == true)
            {
                if (checkTile == true)
                {
                    moveTouch = false;
                    //활성화된 맨홀 타일이면 나오는 타일도 선택해야함
                    if (dataOnTiles[cell].type == TilemapType.MANHOLE && dataOnTiles[cell].isActive == true)
                        pv.RPC(nameof(PassManholeTile), RpcTarget.AllBufferedViaServer, pos);
                    else
                        pv.RPC(nameof(MoveCharTile), RpcTarget.AllBufferedViaServer, pos);
                }
                else
                    moveTouch = true;
            }
            //스킬 사용인지 
            else if (skillTouch == true)
            {
                if (checkTile == true)
                {
                    skillTouch = false;
                    Debug.Log("skill touch");

                    pv.RPC(nameof(ChangeSkillTile), RpcTarget.AllBufferedViaServer, pos);
                }
                else
                {
                    skillTouch = true;
                }
            }
        }
    }

    /// <summary>
    /// 클릭한 타일이 이동가능한 타일인지 체크
    /// </summary>
    /// <param name="_cell"></param>
    /// <returns></returns>
    private bool CheckClickTile(Vector3Int _cell)
    {
        if (moveTouch == false && skillTouch == false)
        {
            return false;
        }

        if (dataOnTiles.ContainsKey(_cell) == true)
        {
            return dataOnTiles[_cell].canTouch;
        }
        else
        {
            return false;
        }
    }

    #region 기본 이동 관련
    [PunRPC]
    /// <summary>
    /// 마우스 위치의 타일로 이동
    /// </summary>
    /// <param name="_cellpos">클릭한 타일의 좌표(해상도에 따라 실제 위치가 다르기 때문에 타일 좌표로 맞게 위치를 다시 구해야함)</param>
    public void MoveCharTile(Vector3 _pos)
    {
        Vector3Int cell = tilemap.WorldToCell(new Vector3(_pos.x, _pos.y));

        Vector3 worldPosition = tilemap.CellToWorld(cell);
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        charPos_list[PlayUIGroup.Instance.SelectChar_Name] = cell;
        characterTile[PlayUIGroup.Instance.SelectChar_Name].SetPos(screenPosition);                     //선택한 타일로 캐릭터 이동

        dataOnTiles[previousPos].character_name = string.Empty;
        dataOnTiles[previousPos].character_isOn = false;

        ResetNeighborhoods();

        //탈출 했는지 체크
        bool exit_check = dataOnTiles[cell].type == TilemapType.POLICELINE;
        if (exit_check == true)
        {
            //탈출
            GameManager.Instance.GameOver(true);
        }
        else if (dataOnTiles[cell].character_isOn == true)       //검거하기
        {
            Debug.Log(GameManager.Instance.Jack_Name + " == " + dataOnTiles[cell].character_name);
            if (GameManager.Instance.Jack_Name == dataOnTiles[cell].character_name)
            {
                GameManager.Instance.GameOver(false);
            }
            else
            {
                GameManager.Instance.GameOver(true); //검거 실패
            }
        }
        else
        {
            dataOnTiles[cell].character_name = PlayUIGroup.Instance.SelectChar_Name;
            dataOnTiles[cell].character_isOn = true;

            PlayUIGroup.Instance.Move_Done = true;
            PlayUIGroup.Instance.CheckCardActionState();
        }
    }

    [PunRPC]
    public void PassManholeTile(Vector3 _pos)
    {
        //캐릭터 위치 변경
        Vector3Int click_cellPos = tilemap.WorldToCell(new Vector3(_pos.x, _pos.y));

        Vector3 worldPosition = tilemap.CellToWorld(click_cellPos);
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        charPos_list[PlayUIGroup.Instance.SelectChar_Name] = click_cellPos;
        characterTile[PlayUIGroup.Instance.SelectChar_Name].SetPos(screenPosition);

        dataOnTiles[previousPos].character_name = string.Empty;
        dataOnTiles[previousPos].character_isOn = false;

        dataOnTiles[click_cellPos].character_name = PlayUIGroup.Instance.SelectChar_Name;
        dataOnTiles[click_cellPos].character_isOn = true;

        //첫번째 맨홀 클릭
        if (manholeClickCount == 0)
        {
            int temp_index = dataOnTiles[click_cellPos].moveIndex;
            leftMoveCount = temp_index + 1;
            ResetNeighborhoods();

            foreach (var item in dataOnTiles)
            {
                if (item.Value.type == TilemapType.MANHOLE)
                {
                    if (item.Value.isActive == true)
                    {
                        tilemap.SetTile(item.Key, item.Value.selectTile);
                        item.Value.canTouch = true;
                    }
                }
            }
            moveTouch = true;
        }
        //두번째 맨홀 클릭(나오는 곳)
        else if (manholeClickCount == 1)
        {
            foreach (var item in dataOnTiles)
            {
                if (item.Value.type == TilemapType.MANHOLE)
                {
                    if (click_cellPos != item.Key)    //클릭한 맨홀을 제외한 활성화된 맨홀 터치 가능하게
                    {
                        tilemap.SetTile(item.Key, item.Value.normalTile);
                        item.Value.canTouch = false;
                    }
                }
            }

            if (previousPos == click_cellPos || leftMoveCount < 0)
            {
                //해당 맨홀에서 완료
                ResetNeighborhoods();
                tilemap.SetTile(click_cellPos, dataOnTiles[click_cellPos].normalTile);
                dataOnTiles[click_cellPos].canTouch = false;

                PlayUIGroup.Instance.Move_Done = true;
                PlayUIGroup.Instance.CheckCardActionState();
            }
            else
            {
                //남은 칸 만큼 맨홀에서 이동 가능한 칸 표시
                temp_neighborhoods.Clear();

                TileRecursive(click_cellPos, click_cellPos, leftMoveCount, maxMoveCount);

                temp_neighborhoods.Add(click_cellPos);      //나왔던 맨홀도 이웃타일에 넣어서 같이 초기화 시킬수 있게
                moveTouch = true;
            }
        }
        else
        {
            ResetNeighborhoods();

            PlayUIGroup.Instance.Move_Done = true;
            PlayUIGroup.Instance.CheckCardActionState();
        }

        previousPos = click_cellPos;
        manholeClickCount++;
    }

    private void ResetNeighborhoods()
    {
        for (int i = 0; i < temp_neighborhoods.Count; i++)
        {
            tilemap.SetTile(temp_neighborhoods[i], dataOnTiles[temp_neighborhoods[i]].normalTile);

            dataOnTiles[temp_neighborhoods[i]].canTouch = false;
            dataOnTiles[temp_neighborhoods[i]].moveIndex = 0;
        }

        foreach (var item in characterTile)
        {
            characterTile[item.Key].CharTextOff();
        }
        exitText_tr.gameObject.SetActive(false);
    }

    /// <summary>
    /// 이동할 캐릭터의 이웃 타일 활성화
    /// </summary>
    /// <param name="_name"></param>
    public void ReadyCharacterTile(int _max)
    {
        //캐릭터가 있는 타일 위치값 가져오기
        string str = PlayUIGroup.Instance.SelectChar_Name;
        Debug.Log("ready char " + str);
        temp_neighborhoods.Clear();

        manholeClickCount = 0;
        leftMoveCount = 0;
        maxMoveCount = _max;

        foreach (var item in dataOnTiles)
        {
            if (item.Value.character_isOn == true)
            {
                if (item.Value.character_name.Contains(str))
                {
                    previousPos = item.Key;
                }
            }
        }

        TileRecursive(previousPos, previousPos, 1, _max);

        moveTouch = true;
    }

    /// <summary>
    /// 이웃한 타일 모두 찾기
    /// </summary>
    /// <param name="_start">이동할 캐릭터의 위치</param>
    /// <param name="_vec">이웃을 찾을 타일의 좌표</param>
    /// <param name="_num">이동한 칸 수</param>
    /// <param name="_max">최대 이동 가능한 칸 수</param>
    public void TileRecursive(Vector3Int _start, Vector3Int _vec, int _num, int _max)
    {
        if (_num > _max)
            return;

        bool jackMoveCheck = false;
        if (myTurn == true && GameManager.Instance.PlayType == PlayTypes.JACK)      //내 차례이고, 내가 jack일 때
            jackMoveCheck = true;
        else if (myTurn == false && GameManager.Instance.PlayType != PlayTypes.JACK)     //내 차례가 아니고, 내가 jack이 아닐때
            jackMoveCheck = true;

        Vector3Int target_vec;
        int num = _num;
        string temp_name = GameManager.Instance.Jack_Name;

        List<Vector3Int> temp_list = new List<Vector3Int>();
        for (int i = 0; i < dataOnTiles[_vec].Neighborhoods.Length; i++)
        {
            bool neighborhoodExist = dataOnTiles.ContainsKey(dataOnTiles[_vec].Neighborhoods[i]);
            if (neighborhoodExist == true)
            {
                target_vec = dataOnTiles[_vec].Neighborhoods[i];
                bool keyExists = dataOnTiles.ContainsKey(target_vec);

                if (keyExists == true && _start != target_vec)
                {
                    if (dataOnTiles[target_vec].type == TilemapType.LOAD ||
                        dataOnTiles[target_vec].type == TilemapType.MANHOLE)
                    {
                        if (dataOnTiles[target_vec].character_isOn == true && jackMoveCheck == false)
                        {
                            characterTile[dataOnTiles[target_vec].character_name].SetText();
                        }

                        temp_neighborhoods.Add(target_vec);

                        temp_list.Add(target_vec);

                        //이웃한 타일 선택 이미지로 교체
                        tilemap.SetTile(target_vec, dataOnTiles[target_vec].selectTile);

                        bool arrived = dataOnTiles[target_vec].canTouch;

                        //해당 타일 터치 가능하게
                        dataOnTiles[target_vec].canTouch = true;

                        if (dataOnTiles[target_vec].moveIndex < num && arrived == true)
                        {
                            continue;
                        }
                        else
                        {
                            dataOnTiles[target_vec].moveIndex = num;
                        }
                    }
                    else if (dataOnTiles[target_vec].type == TilemapType.POLICELINE && jackMoveCheck == true)    //범인은 탈출 가능
                    {
                        //경찰 저지선 없음 + 목격된 상태 아닐때만 + 범인 캐릭터 일때
                        if (dataOnTiles[target_vec].isActive == false && GameManager.Instance.Jack_Exposed == false &&
                            temp_name == dataOnTiles[previousPos].character_name)
                        {
                            //탈출하기 텍스트 띄우기
                            Vector3 worldPosition = tilemap.CellToWorld(target_vec);
                            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
                            exitText_tr.position = screenPosition;
                            exitText_tr.gameObject.SetActive(true);

                            temp_neighborhoods.Add(target_vec);

                            temp_list.Add(target_vec);

                            //이웃한 타일 선택 이미지로 교체
                            tilemap.SetTile(target_vec, dataOnTiles[target_vec].selectTile);

                            bool arrived = dataOnTiles[target_vec].canTouch;

                            //해당 타일 터치 가능하게
                            dataOnTiles[target_vec].canTouch = true;

                            if (dataOnTiles[target_vec].moveIndex < num && arrived == true)
                                continue;
                            else
                                dataOnTiles[target_vec].moveIndex = num;
                        }
                    }

                    //Stealthy는 건물도 넘어갈 수 있음
                    if (_max == 4)
                    {
                        if (dataOnTiles[target_vec].type == TilemapType.LIGHT ||
                       dataOnTiles[target_vec].type == TilemapType.OBSTACLE)
                        {
                            temp_neighborhoods.Add(target_vec);

                            temp_list.Add(target_vec);

                            dataOnTiles[target_vec].moveIndex = num;
                        }
                    }
                }
            }
        }

        int temp_num = num + 1;
        for (int i = 0; i < temp_list.Count; i++)
        {
            TileRecursive(_start, temp_list[i], temp_num, _max);
        }
    }
    #endregion

    #region 스킬 관련
    public void SyncCharSkill()
    {
        pv.RPC(nameof(SkillActive), RpcTarget.AllBufferedViaServer);
    }

    [PunRPC]
    /// <summary>
    /// 캐릭터별 스킬 구분
    /// </summary>
    public void SkillActive()
    {
        clickCount = 0;
        moveTouch = false;

        string temp_name = PlayUIGroup.Instance.SelectChar_Name;
        bool temp_boolen = false;

        foreach (var item in dataOnTiles)
        {
            temp_boolen = false;

            switch (temp_name)
            {
                case "Lestrade":    //경찰 저지선 이동
                    if (item.Value.type == TilemapType.POLICELINE && item.Value.isActive == true)
                    {
                        temp_boolen = true;
                    }
                    break;
                case "Bert":        //맨홀 타일 이동
                    if (item.Value.type == TilemapType.MANHOLE && item.Value.isActive == true)
                    {
                        temp_boolen = true;
                    }
                    break;
                case "Smith":       //가로등 타일 이동

                    if (item.Value.type == TilemapType.LIGHT && item.Value.isActive == true)
                    {
                        temp_boolen = true;
                    }
                    break;
                case "Gull":        //gull 제외한 모든 캐릭터 타일 활성화
                    if (item.Value.character_isOn == true)
                    {
                        if (item.Value.character_name.Contains("Gull") == false)
                        {
                            temp_boolen = true;
                        }
                    }
                    break;
                case "Watson":
                    if (item.Value.character_isOn == true)
                    {
                        if (item.Value.character_name.Contains("Watson") == true)
                        {
                            //Watson 이웃한 모든 타일 활성화
                            for (int i = 0; i < item.Value.Neighborhoods.Length; i++)
                            {
                                bool neighborhoodExist = dataOnTiles.ContainsKey(item.Value.Neighborhoods[i]);
                                if (neighborhoodExist == true)
                                {
                                    Vector3Int target_vec = item.Value.Neighborhoods[i];

                                    temp_neighborhoods.Add(target_vec);

                                    //이웃한 타일 선택 이미지로 교체
                                    tilemap.SetTile(target_vec, dataOnTiles[target_vec].selectTile);

                                    //해당 타일 터치 가능하게
                                    dataOnTiles[target_vec].canTouch = true;
                                }
                            }
                        }
                    }
                    break;
            }

            if (temp_boolen == true)
            {
                tilemap.SetTile(item.Key, dataOnTiles[item.Key].selectTile);
                dataOnTiles[item.Key].canTouch = true;
            }
        }

        if (temp_name == "Goodley")
        {
            leftMoveCount = 3;
            goodleyMove = false;
            goodleySkill_name.Clear();

            GoodleySkillSet();
        }

        skillTouch = myTurn;
    }

    /// <summary>
    /// 여러 캐릭터 이동시 Goodley 스킬 활성화
    /// </summary>
    private void GoodleySkillSet()
    {
        foreach (var item in charPos_list)
        {
            //다른 캐릭터 자기쪽으로 이동시키기
            //바로 옆에 붙은 캐릭터는 제외
            if (item.Key.Contains("Goodley") == false)
            {
                if (goodleySkill_name.Count > 0)
                {
                    for (int i = 0; i < goodleySkill_name.Count; i++)
                    {
                        if (goodleySkill_name[i] != item.Key)
                        {
                            tilemap.SetTile(item.Value, dataOnTiles[item.Value].selectTile);
                            dataOnTiles[item.Value].canTouch = true;
                        }
                    }
                }
                else
                {
                    tilemap.SetTile(item.Value, dataOnTiles[item.Value].selectTile);
                    dataOnTiles[item.Value].canTouch = true;
                }
            }
        }
    }

    [PunRPC]
    /// <summary>
    /// 스킬 - 타일 교체
    /// </summary>
    public void ChangeSkillTile(Vector3 _pos)
    {
        string str = PlayUIGroup.Instance.SelectChar_Name;

        //클릭한 타일의 좌표
        Vector3Int cell = tilemap.WorldToCell(new Vector3(_pos.x, _pos.y));
        Vector3 worldPosition = tilemap.CellToWorld(cell);
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        TilemapType temp_type = dataOnTiles[cell].type;

        if (str.Contains("Bert") || str.Contains("Smith") || str.Contains("Lestrade"))
        {
            if (clickCount == 0)        //활성화된 가로등과 교체할 비활성화된 가로등 타일 선택 가능하게
            {
                foreach (var item in dataOnTiles)
                {
                    if (item.Value.type == temp_type)
                    {
                        if (item.Value.isActive == false)
                        {
                            tilemap.SetTile(item.Key, dataOnTiles[item.Key].selectTile);
                            dataOnTiles[item.Key].canTouch = true;
                        }
                        else
                        {
                            dataOnTiles[item.Key].canTouch = false;

                            if (item.Key != cell)     //현재 선택한 가로등을 제외한
                            {
                                tilemap.SetTile(item.Key, dataOnTiles[item.Key].normalTile);
                            }
                            else
                            {
                                dataOnTiles[item.Key].isSelected = true;       //현재 선택한 가로등
                                previousPos = item.Key;
                            }
                        }
                    }
                }

                skillTouch = myTurn;
            }
            else if (clickCount == 1)
            {
                dataOnTiles[cell].isActive = true;

                dataOnTiles[previousPos].isSelected = false;
                dataOnTiles[previousPos].isActive = false;

                if (temp_type == TilemapType.LIGHT)
                {
                    UpdateLightNumber(cell.x, cell.y);

                    int temp_light = dataOnTiles[previousPos].light_num;        //가로등 번호 바꾸기
                    dataOnTiles[previousPos].light_num = dataOnTiles[cell].light_num;
                    dataOnTiles[previousPos].light_num = temp_light;
                }

                foreach (var item in dataOnTiles)
                {
                    if (item.Value.type == temp_type)
                    {
                        tilemap.SetTile(item.Key, dataOnTiles[item.Key].normalTile);
                        dataOnTiles[item.Key].canTouch = false;
                    }
                }

                //스킬 사용 완료
                PlayUIGroup.Instance.Skill_Done = true;
                PlayUIGroup.Instance.CheckCardActionState();
            }
            clickCount++;
        }
        else if (str.Contains("Gull"))
        {
            //Gull 캐릭터의 좌표
            Vector3Int gull_cell = charPos_list[str];
            Vector3 gull_worldPosition = tilemap.CellToWorld(gull_cell);
            Vector3 gull_screenPosition = mainCamera.WorldToScreenPoint(gull_worldPosition);

            //클릭한 캐릭터의 위치도 바꿔야함
            string cell_name = dataOnTiles[cell].character_name;    //gull과 자리 바꿀 캐릭터
            charPos_list[cell_name] = gull_cell;
            charPos_list[str] = cell;

            characterTile[cell_name].SetPos(gull_screenPosition);
            characterTile[str].SetPos(screenPosition);

            string temp_name = dataOnTiles[gull_cell].character_name;
            dataOnTiles[gull_cell].character_name = dataOnTiles[cell].character_name;
            dataOnTiles[cell].character_name = temp_name;

            foreach (var item in dataOnTiles)
            {
                if (item.Value.character_isOn == true)
                {
                    item.Value.canTouch = false;
                    tilemap.SetTile(item.Key, item.Value.normalTile);
                }
            }

            PlayUIGroup.Instance.Skill_Done = true;
            PlayUIGroup.Instance.Move_Done = true;      //해당 캐릭터는 스킬 사용시 이동불가하기 때문
            PlayUIGroup.Instance.CheckCardActionState();
        }
        else if (str.Contains("Watson"))
        {
            //선택한 타일이 몇번째 인덱스 이웃인지 따라서 모든 이웃 찾기
            int temp_index = 0;
            for (int i = 0; i < dataOnTiles[charPos_list[str]].Neighborhoods.Length; i++)
            {
                if (cell == dataOnTiles[charPos_list[str]].Neighborhoods[i])
                {
                    temp_index = i;
                    break;
                }
            }

            ResetNeighborhoods();

            //선택한 방향으로 손전등 가리키기
            characterTile[str].HandLightPos(temp_index);

            PlayUIGroup.Instance.Skill_Done = true;
            PlayUIGroup.Instance.CheckCardActionState();
        }
        else if (str.Contains("Goodley"))
        {
            if (goodleyMove == false)       //이동할 캐릭터의 길찾기
            {
                string cell_name = dataOnTiles[cell].character_name;
                previousPos = charPos_list[cell_name];                        //이동할 캐릭터의 위치를 저장
                goodleySkill_name.Add(cell_name);

                findPath.PathFinding(cell, charPos_list[str]);

                findList = findPath.GetNodeList;
                for (int i = 1; i <= leftMoveCount; i++)
                {
                    findVec = new Vector3Int(findList[i].x, findList[i].y);
                    if (dataOnTiles[findVec].character_isOn == true)
                    {
                        if (dataOnTiles[findVec].character_name.Contains("Goodley"))        //Goodley와 겹치지 않도록
                        {
                            break;
                        }
                        else
                        {
                            dataOnTiles[findVec].canTouch = true;
                            dataOnTiles[findVec].moveIndex = i;
                            tilemap.SetTile(findVec, dataOnTiles[findVec].selectTile);
                        }
                    }
                    else
                    {
                        dataOnTiles[findVec].canTouch = true;
                        dataOnTiles[findVec].moveIndex = i;
                        tilemap.SetTile(findVec, dataOnTiles[findVec].selectTile);
                    }
                }

                //캐릭터 타일 초기화
                foreach (var item in dataOnTiles)
                {
                    if (item.Value.character_isOn == true)
                    {
                        item.Value.canTouch = false;
                        tilemap.SetTile(item.Key, dataOnTiles[item.Key].normalTile);
                    }
                }
            }
            else   //이동가능한 타일 선택하기 & 이동하기
            {
                string cell_name = dataOnTiles[previousPos].character_name;     //이동할 캐릭터의 이름
                charPos_list[cell_name] = cell;
                characterTile[cell_name].SetPos(screenPosition);

                dataOnTiles[previousPos].character_name = string.Empty;
                dataOnTiles[previousPos].character_isOn = false;

                dataOnTiles[cell].character_name = cell_name;
                dataOnTiles[cell].character_isOn = true;

                int temp_moveCount = dataOnTiles[cell].moveIndex;

                //초기화
                for (int i = 1; i < findList.Count; i++)
                {
                    findVec = new Vector3Int(findList[i].x, findList[i].y);
                    dataOnTiles[findVec].canTouch = false;
                    dataOnTiles[findVec].moveIndex = 0;
                    tilemap.SetTile(findVec, dataOnTiles[findVec].normalTile);
                }

                //선택한 타일만큼 leftMoveCount 
                leftMoveCount -= temp_moveCount;

                findList.Clear();

                if (leftMoveCount == 0)
                {
                    PlayUIGroup.Instance.Skill_Done = true;
                    PlayUIGroup.Instance.CheckCardActionState();
                }
                else
                {
                    GoodleySkillSet();
                }
            }
            goodleyMove = !goodleyMove;
            skillTouch = myTurn;
        }
    }

    void UpdateLightNumber(int _x, int _y)
    {
        Vector3Int vec = new Vector3Int(_x, _y, 0);
        Vector3 worldPosition = tilemap.CellToWorld(vec);
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        Vector3 worldPosition_pre = tilemap.CellToWorld(previousPos);
        Vector3 screenPosition_pre = mainCamera.WorldToScreenPoint(worldPosition_pre);

        int cur_num = dataOnTiles[vec].light_num;
        int pre_num = dataOnTiles[previousPos].light_num;

        if (cur_num >= 0)
            lightNumber_tr[cur_num].position = new Vector3(screenPosition_pre.x + 50f, screenPosition_pre.y, screenPosition_pre.z);
        if (pre_num >= 0)
            lightNumber_tr[pre_num].position = new Vector3(screenPosition.x + 50f, screenPosition.y, screenPosition.z);
    }
    #endregion

    #region 캐릭터 목격되었는지 체크
    public void CheckExposed()
    {
        Vector3Int target_pos;

        //캐릭터 상태 업데이트
        foreach (var item in characterTile)
        {
            target_pos = charPos_list[item.Key];
            CheckNeighborhood(target_pos.x, target_pos.y, item.Value.Char_Name);

            item.Value.SetImage();
        }

        //손전등이 가리키는 방향의 타일 조사
        int light_dir = characterTile["Watson"].HandLight_Direct;
        Vector3Int start_pos = charPos_list["Watson"];

        bool loop = true;
        do
        {
            target_pos = dataOnTiles[start_pos].Neighborhoods[light_dir];
            if (dataOnTiles.ContainsKey(target_pos) == true)
            {
                if (dataOnTiles[target_pos].type == TilemapType.LOAD || dataOnTiles[target_pos].type == TilemapType.MANHOLE)
                {
                    if (dataOnTiles[target_pos].character_isOn == true)
                    {
                        string str = dataOnTiles[target_pos].character_name;
                        characterTile[str].IsExposed = true;
                        characterTile[str].SetImage();
                        loop = false;
                    }
                }
                else
                    loop = false;
            }
            else
                loop = false;

            start_pos = target_pos;
        } while (loop == true);

        GameManager.Instance.Jack_Exposed = characterTile[GameManager.Instance.Jack_Name].IsExposed;

        //내가 끝났을 때 상대방도 끝났으면 DONE
        GameManager.Instance.gameState = GameManager.GameState.DONE;
        GameManager.Instance.IsPlaying = true;
    }

    /// <summary>
    /// 캐릭터 주변에 다른 캐릭터 또는 활성화된 가로등이 있는지
    /// </summary>
    /// <param name="_x"></param>
    /// <param name="_y"></param>
    private void CheckNeighborhood(int _x, int _y, string _name)
    {
        string str = string.Empty;
        Vector3Int vec = new Vector3Int(_x, _y, 0);
        characterTile[_name].IsExposed = false;

        for (int i = 0; i < dataOnTiles[vec].Neighborhoods.Length; i++)
        {
            Vector3Int target_vec = dataOnTiles[vec].Neighborhoods[i];
            if (dataOnTiles.ContainsKey(target_vec) == true)
            {
                if (dataOnTiles[target_vec].character_isOn == true)
                {
                    str = dataOnTiles[target_vec].character_name;
                    characterTile[str].IsExposed = true;
                    characterTile[_name].IsExposed = true;
                }
                else if (dataOnTiles[target_vec].type == TilemapType.LIGHT && dataOnTiles[target_vec].isActive == true)
                {
                    characterTile[_name].IsExposed = true;      //해당 캐릭터의 주변에 활성화된 가로등이 있음
                }
            }
        }
    }
    #endregion

    public void DeleteLight()
    {
        int temp_round = GameManager.Instance.Round_Cur - 1;        //0부터 시작
        foreach (var item in dataOnTiles)
        {
            if (item.Value.type == TilemapType.LIGHT && dataOnTiles[item.Key].light_num == temp_round)
            {
                dataOnTiles[item.Key].light_num = -1;
                lightNumber_tr[temp_round].gameObject.SetActive(false);

                dataOnTiles[item.Key].isActive = false;
                tilemap.SetTile(item.Key, dataOnTiles[item.Key].normalTile);
                break;
            }
        }
    }
}