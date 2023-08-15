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

    private Camera mainCamera;
    private PhotonView pv;
    private AStarFindPath findPath;

    public Dictionary<Vector3Int, TileInfo> dataOnTiles;        //전체 타일 
    public Dictionary<string, CharacterTile> characterTile;     //캐릭터 얼굴 UI
    private List<Vector3Int> temp_neighborhoods;                        //이동가능한 위치 표시용

    private Node[,] NodeArray;
    [SerializeField] List<Node> findList;                //Goodley로 가는 길
    private Vector3Int findVec;

    [SerializeField] Tilemap tilemap;
    [SerializeField] Tilemap obstacle_tilemap;

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

    Dictionary<Vector3Int, bool> manholePos_list = new Dictionary<Vector3Int, bool>()
    {
        { new Vector3Int(6, 0), true }, { new Vector3Int(2, 2), false }, { new Vector3Int(8, 5), true }, { new Vector3Int(5, 5), true },
        { new Vector3Int(3, 7), true }, { new Vector3Int(0, 7), true }, { new Vector3Int(7, 11), false }, { new Vector3Int(4, 12), true }
    };

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

    #region 타일 정보 세팅
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

            SetCharacterTile(pos);
            SetLightTile(pos);
            SetMaholeTile(pos);
            SetPoliceLineTile(pos);
            SetObstacleTile(pos);

            bool temp_wall = dataOnTiles[pos].type == TilemapType.LIGHT || dataOnTiles[pos].type == TilemapType.OBSTACLE;
            if (pos.x >= 0 && pos.y >= 0)
            {
                NodeArray[pos.x, pos.y] = new Node(temp_wall, pos.x, pos.y);
            }
        }

        for (int i = 0; i < deleteLight_list.Count; i++)
        {
            Vector3 screenPosition = GetScreenPosition(deleteLight_list[i]);

            dataOnTiles[deleteLight_list[i]].light_num = i;
            lightNumber_tr[i].position = new Vector3(screenPosition.x + 50f, screenPosition.y, screenPosition.z);
        }

        findPath.SetNode(NodeArray);
    }

    private void SetCharacterTile(Vector3Int pos)
    {
        foreach (var tile in charPos_list)
        {
            if (tile.Value == pos)
            {
                dataOnTiles[pos].character_isOn = true;
                dataOnTiles[pos].character_name = tile.Key;

                Vector3 cellToWorld = tilemap.CellToWorld(pos);
                var targetPos = mainCamera.WorldToScreenPoint(cellToWorld);
                characterTile[tile.Key].SetPos(targetPos);
            }
        }
    }

    private void SetLightTile(Vector3Int pos)
    {
        foreach (var tile in lightPos_list)
        {
            if (tile.Key == pos)
            {
                dataOnTiles[pos].type = TilemapType.LIGHT;
                dataOnTiles[pos].normalTiles[0] = active_tile[(int)(TilemapType.LIGHT)];
                dataOnTiles[pos].normalTiles[1] = unactive_tile[(int)(TilemapType.LIGHT)];
                dataOnTiles[pos].selectTiles[0] = light_select_tile[0];
                dataOnTiles[pos].selectTiles[1] = light_select_tile[1];

                dataOnTiles[pos].isActive = tile.Value;
                dataOnTiles[pos].light_num = -1;

                tilemap.SetTile(pos, dataOnTiles[pos].normalTile);
            }
        }
    }

    private void SetMaholeTile(Vector3Int pos)
    {
        foreach (var tile in manholePos_list)
        {
            if (tile.Key == pos)
            {
                dataOnTiles[pos].type = TilemapType.MANHOLE;
                dataOnTiles[pos].normalTiles[0] = active_tile[(int)(TilemapType.MANHOLE)];
                dataOnTiles[pos].normalTiles[1] = unactive_tile[(int)(TilemapType.MANHOLE)];
                dataOnTiles[pos].selectTiles[0] = manhole_select_tile[0];
                dataOnTiles[pos].selectTiles[1] = manhole_select_tile[1];

                dataOnTiles[pos].isActive = tile.Value;

                tilemap.SetTile(pos, dataOnTiles[pos].normalTile);
            }
        }
    }

    private void SetPoliceLineTile(Vector3Int pos)
    {
        foreach (var tile in policeLinePos_list)
        {
            if (tile.Key == pos)
            {
                dataOnTiles[pos].type = TilemapType.POLICELINE;

                dataOnTiles[pos].normalTiles[0] = active_tile[(int)(TilemapType.POLICELINE)];
                dataOnTiles[pos].normalTiles[1] = unactive_tile[(int)(TilemapType.POLICELINE)];
                dataOnTiles[pos].selectTiles[0] = policeLine_select_tile[0];
                dataOnTiles[pos].selectTiles[1] = policeLine_select_tile[1];

                dataOnTiles[pos].isActive = tile.Value;

                tilemap.SetTile(pos, dataOnTiles[pos].normalTile);
            }
        }
    }

    private void SetObstacleTile(Vector3Int pos)
    {
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
    }
    #endregion

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

            bool checkTile = CheckClickTile(cell);
            if (moveTouch == true)
            {
                TouchToMove(pos, cell, checkTile);
            }
            //스킬 사용인지 
            else if (skillTouch == true)
            {
                TouchToSkill(pos, checkTile);
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

    private void TouchToMove(Vector3 pos, Vector3Int cell, bool checkTile)
    {
        moveTouch = !checkTile;
        if (checkTile == true)
        {
            //활성화된 맨홀 타일이면 나오는 타일도 선택해야함
            if (dataOnTiles[cell].type == TilemapType.MANHOLE && dataOnTiles[cell].isActive == true)
            {
                pv.RPC(nameof(PassManholeTile), RpcTarget.AllBufferedViaServer, pos);
            }
            else
            {
                pv.RPC(nameof(MoveCharTile), RpcTarget.AllBufferedViaServer, pos);
            }
        }
    }

    private void TouchToSkill(Vector3 pos, bool checkTile)
    {
        skillTouch = !checkTile;

        if (checkTile == true)
        {
            pv.RPC(nameof(ChangeSkillTile), RpcTarget.AllBufferedViaServer, pos);
        }
    }


    private void SetCharTileName(Vector3Int cell, string charName, bool on)
    {
        dataOnTiles[cell].character_name = charName;
        dataOnTiles[cell].character_isOn = on;
    }


    private Vector3 GetScreenPosition(Vector3Int cell)
    {
        Vector3 worldPosition = tilemap.CellToWorld(cell);
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        return screenPosition;
    }


    #region 기본 이동 관련
    [PunRPC]
    /// <summary>
    /// 마우스 위치의 타일로 이동
    /// </summary>
    /// <param name="_cellpos">클릭한 타일의 좌표(해상도에 따라 실제 위치가 다르기 때문에 타일 좌표로 맞게 위치를 다시 구해야함)</param>
    public void MoveCharTile(Vector3 pos)
    {
        Vector3Int cell = tilemap.WorldToCell(new Vector3(pos.x, pos.y));
        Vector3 screenPosition = GetScreenPosition(cell);

        charPos_list[PlayUIGroup.Instance.SelectChar_Name] = cell;
        characterTile[PlayUIGroup.Instance.SelectChar_Name].SetPos(screenPosition);                     //선택한 타일로 캐릭터 이동

        dataOnTiles[previousPos].character_name = string.Empty;
        dataOnTiles[previousPos].character_isOn = false;

        ResetNeighborhoods();

        //탈출 했는지 체크
        bool exit_check = dataOnTiles[cell].type == TilemapType.POLICELINE;
        if (exit_check == true)
        {
            GameManager.Instance.GameOver(true);
            return;
        }

        if (dataOnTiles[cell].character_isOn == true)       //검거하기
        {
            bool jackWin = GameManager.Instance.Jack_Name == dataOnTiles[cell].character_name;
            GameManager.Instance.GameOver(!jackWin);
        }
        else
        {
            SetCharTileName(cell, PlayUIGroup.Instance.SelectChar_Name, true);

            PlayUIGroup.Instance.Move_Done = true;
            PlayUIGroup.Instance.CheckCardActionState();
        }
    }

    [PunRPC]
    public void PassManholeTile(Vector3 pos)
    {
        //캐릭터 위치 변경
        Vector3Int click_cellPos = tilemap.WorldToCell(new Vector3(pos.x, pos.y));
        Vector3 screenPosition = GetScreenPosition(click_cellPos);

        charPos_list[PlayUIGroup.Instance.SelectChar_Name] = click_cellPos;
        characterTile[PlayUIGroup.Instance.SelectChar_Name].SetPos(screenPosition);

        SetCharTileName(previousPos, string.Empty, false);

        SetCharTileName(click_cellPos, PlayUIGroup.Instance.SelectChar_Name, true);

        //첫번째 맨홀 클릭
        switch (manholeClickCount)
        {
            case 0:
                int temp_index = dataOnTiles[click_cellPos].moveIndex;
                leftMoveCount = temp_index + 1;
                ResetNeighborhoods();

                foreach (var item in dataOnTiles)
                {
                    if (item.Value.type == TilemapType.MANHOLE && item.Value.isActive == true)
                    {
                        tilemap.SetTile(item.Key, item.Value.selectTile);
                        item.Value.canTouch = true;
                    }
                }
                moveTouch = true;
                break;
            case 1:         //두번째 맨홀 클릭(나오는 곳)
                foreach (var item in dataOnTiles)
                {
                    if (item.Value.type == TilemapType.MANHOLE && click_cellPos != item.Key)        //클릭한 맨홀을 제외한 활성화된 맨홀 터치 가능하게
                    {
                        tilemap.SetTile(item.Key, item.Value.normalTile);
                        item.Value.canTouch = false;
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
                break;
            default:
                ResetNeighborhoods();

                PlayUIGroup.Instance.Move_Done = true;
                PlayUIGroup.Instance.CheckCardActionState();
                break;
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
    public void ReadyCharacterTile(int max)
    {
        //캐릭터가 있는 타일 위치값 가져오기
        string str = PlayUIGroup.Instance.SelectChar_Name;
        temp_neighborhoods.Clear();

        manholeClickCount = 0;
        leftMoveCount = 0;
        maxMoveCount = max;

        foreach (var item in dataOnTiles)
        {
            if (item.Value.character_isOn == true && item.Value.character_name.Contains(str))
            {
                previousPos = item.Key;
            }
        }

        TileRecursive(previousPos, previousPos, 1, max);

        moveTouch = true;
    }


    /// <summary>
    /// 이웃한 타일 모두 찾기
    /// </summary>
    /// <param name="_start">이동할 캐릭터의 위치</param>
    /// <param name="_vec">이웃을 찾을 타일의 좌표</param>
    /// <param name="_num">이동한 칸 수</param>
    /// <param name="_max">최대 이동 가능한 칸 수</param>
    public void TileRecursive(Vector3Int start, Vector3Int vec, int next, int max)
    {
        if (next > max)
            return;

        bool jackMoveCheck = false;
        if (myTurn == true && GameManager.Instance.PlayType == PlayTypes.JACK)          //내 차례이고, 내가 jack일 때
        {
            jackMoveCheck = true;
        }
        else if (myTurn == false && GameManager.Instance.PlayType != PlayTypes.JACK)     //내 차례가 아니고, 내가 jack이 아닐때
        {
            jackMoveCheck = true;
        }

        Vector3Int target_vec;
        int num = next;
        string temp_name = GameManager.Instance.Jack_Name;

        List<Vector3Int> temp_list = new List<Vector3Int>();
        for (int i = 0; i < dataOnTiles[vec].Neighborhoods.Length; i++)
        {
            bool neighborhoodExist = dataOnTiles.ContainsKey(dataOnTiles[vec].Neighborhoods[i]);
            if (neighborhoodExist == false)
            {
                continue;
            }

            target_vec = dataOnTiles[vec].Neighborhoods[i];

            bool keyExists = dataOnTiles.ContainsKey(target_vec);
            if (keyExists == false || start == target_vec)
            {
                continue;
            }

            if (dataOnTiles[target_vec].type == TilemapType.LOAD ||
                dataOnTiles[target_vec].type == TilemapType.MANHOLE)
            {
                if (dataOnTiles[target_vec].character_isOn == true && jackMoveCheck == false)
                {
                    characterTile[dataOnTiles[target_vec].character_name].SetText();
                }

                temp_list.Add(target_vec);
                SetNeihborhoodsTile(target_vec, num, max);
            }
            else if (dataOnTiles[target_vec].type == TilemapType.POLICELINE && jackMoveCheck == true)    //범인은 탈출 가능
            {
                //경찰 저지선 없음 + 목격된 상태 아닐때만 + 범인 캐릭터 일때
                if (dataOnTiles[target_vec].isActive == false && GameManager.Instance.Jack_Exposed == false &&
                    temp_name == dataOnTiles[previousPos].character_name)
                {
                    ExitTextOn(target_vec);                                                             //탈출하기 텍스트 띄우기

                    temp_list.Add(target_vec);
                    SetNeihborhoodsTile(target_vec, num, max);
                }
            }

            if (max == 4)
            {
                if (dataOnTiles[target_vec].type == TilemapType.LIGHT || dataOnTiles[target_vec].type == TilemapType.OBSTACLE)      //Stealthy는 건물도 넘어갈 수 있음
                {
                    temp_neighborhoods.Add(target_vec);

                    temp_list.Add(target_vec);

                    dataOnTiles[target_vec].moveIndex = num;
                }
            }
        }

        int temp_num = num + 1;
        for (int i = 0; i < temp_list.Count; i++)
        {
            TileRecursive(start, temp_list[i], temp_num, max);
        }
    }

    private void SetNeihborhoodsTile(Vector3Int target_vec, int num, int max)
    {
        temp_neighborhoods.Add(target_vec);

        tilemap.SetTile(target_vec, dataOnTiles[target_vec].selectTile);                    //이웃한 타일 선택 이미지로 교체

        bool arrived = dataOnTiles[target_vec].canTouch;

        dataOnTiles[target_vec].canTouch = true;
        //if (dataOnTiles[target_vec].moveIndex < num && arrived == true)
        //{

        //}
        //else
        if (dataOnTiles[target_vec].moveIndex < num || arrived == true)
        {
            dataOnTiles[target_vec].moveIndex = num;
        }

    }

    private void ExitTextOn(Vector3Int target_vec)
    {
        Vector3 screenPosition = GetScreenPosition(target_vec);
        exitText_tr.position = screenPosition;
        exitText_tr.gameObject.SetActive(true);
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
            }

            if (temp_boolen == true)
            {
                tilemap.SetTile(item.Key, dataOnTiles[item.Key].selectTile);
                dataOnTiles[item.Key].canTouch = true;
            }
        }

        if (temp_name == "Watson")
        {
            TileInfo watsonTile = dataOnTiles[charPos_list[temp_name]];
            for (int i = 0; i < watsonTile.Neighborhoods.Length; i++)
            {
                bool neighborhoodExist = dataOnTiles.ContainsKey(watsonTile.Neighborhoods[i]);
                if (neighborhoodExist == true)
                {
                    Vector3Int target_vec = watsonTile.Neighborhoods[i];

                    temp_neighborhoods.Add(target_vec);

                    //이웃한 타일 선택 이미지로 교체
                    tilemap.SetTile(target_vec, dataOnTiles[target_vec].selectTile);

                    //해당 타일 터치 가능하게
                    dataOnTiles[target_vec].canTouch = true;
                }
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
            if (item.Key.Contains("Goodley") == true)
            {
                continue;
            }

            //캐릭터 타일 활성화
            if (CheckGoodleyName(item) == false)
            {
                tilemap.SetTile(item.Value, dataOnTiles[item.Value].selectTile);
                dataOnTiles[item.Value].canTouch = true;
            }
        }
    }

    /// <summary>
    /// 구들리 스킬로 이동했던 캐릭터 인지(이동한 캐릭터가 없으면 false)
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private bool CheckGoodleyName(KeyValuePair<string, Vector3Int> item)
    {
        for (int i = 0; i < goodleySkill_name.Count; i++)
        {
            if (goodleySkill_name[i] == item.Key)           //이동한 캐릭터 제외
            {
                return true;
            }
        }
        return false;
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
        Vector3 screenPosition = GetScreenPosition(cell);

        TilemapType temp_type = dataOnTiles[cell].type;

        switch (str)
        {
            case "Bert":
            case "Smith":
            case "Lestrade":
                if (clickCount == 0)        //활성화된 가로등과 교체할 비활성화된 가로등 타일 선택 가능하게
                {
                    SkillChangeTile(cell, temp_type);

                    skillTouch = myTurn;
                }
                else if (clickCount == 1)
                {
                    SkillChangeTileDone(cell, temp_type);                   //스킬 사용 완료

                    PlayUIGroup.Instance.Skill_Done = true;
                    PlayUIGroup.Instance.CheckCardActionState();
                }
                clickCount++;
                break;
            case "Gull":
                GullChangeTile(str, cell, screenPosition);

                ResetCharacterTile();

                PlayUIGroup.Instance.Skill_Done = true;
                PlayUIGroup.Instance.Move_Done = true;      //해당 캐릭터는 스킬 사용시 이동불가하기 때문
                PlayUIGroup.Instance.CheckCardActionState();
                break;
            case "Watson":
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

                characterTile[str].HandLightPos(temp_index);        //선택한 방향으로 손전등 가리키기

                PlayUIGroup.Instance.Skill_Done = true;
                PlayUIGroup.Instance.CheckCardActionState();
                break;
            case "Goodley":
                GoodleyChangeTile(str, cell, screenPosition);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Bert, Smith, Lestrade 스킬 사용
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="temp_type"></param>
    private void SkillChangeTile(Vector3Int cell, TilemapType temp_type)
    {
        foreach (var item in dataOnTiles)
        {
            if (item.Value.type != temp_type)
            {
                continue;
            }

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

    /// <summary>
    /// Bert, Smith, Lestrade 스킬 사용 완료
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="temp_type"></param>
    private void SkillChangeTileDone(Vector3Int cell, TilemapType temp_type)
    {
        dataOnTiles[cell].isActive = true;

        dataOnTiles[previousPos].isSelected = false;
        dataOnTiles[previousPos].isActive = false;

        if (temp_type == TilemapType.LIGHT)
        {
            UpdateLightNumber(cell.x, cell.y);
        }

        foreach (var item in dataOnTiles)
        {
            if (item.Value.type == temp_type)
            {
                tilemap.SetTile(item.Key, dataOnTiles[item.Key].normalTile);
                dataOnTiles[item.Key].canTouch = false;
            }
        }
    }

    private void GullChangeTile(string str, Vector3Int cell, Vector3 screenPosition)
    {
        Vector3Int gull_cell = charPos_list[str];               //Gull 캐릭터의 좌표
        Vector3 gull_screenPosition = GetScreenPosition(gull_cell);

        //클릭한 캐릭터의 위치도 바꿔야함
        string cell_name = dataOnTiles[cell].character_name;    //Gull과 자리 바꿀 캐릭터
        charPos_list[cell_name] = gull_cell;
        charPos_list[str] = cell;

        characterTile[cell_name].SetPos(gull_screenPosition);
        characterTile[str].SetPos(screenPosition);

        string temp_name = dataOnTiles[gull_cell].character_name;
        SetCharTileName(gull_cell, dataOnTiles[cell].character_name, true);
        SetCharTileName(cell, temp_name, true);
    }


    #region 구들리
    private void GoodleyChangeTile(string str, Vector3Int cell, Vector3 screenPosition)
    {
        if (goodleyMove == false)       //이동할 캐릭터의 길찾기
        {
            FindPathToGoodley(str, cell);

            ResetCharacterTile();
        }
        else   //이동가능한 타일 선택하기 & 이동하기
        {
            string cell_name = dataOnTiles[previousPos].character_name;     //이동할 캐릭터의 이름
            charPos_list[cell_name] = cell;
            characterTile[cell_name].SetPos(screenPosition);

            SetCharTileName(previousPos, string.Empty, false);
            SetCharTileName(cell, cell_name, true);

            int temp_moveCount = dataOnTiles[cell].moveIndex;

            ResetFoundPath(temp_moveCount);                                 //초기화

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

    private void FindPathToGoodley(string str, Vector3Int cell)
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
    }

    /// <summary>
    /// 구들리 스킬로 찾은 타일 초기화
    /// </summary>
    /// <param name="temp_moveCount"></param>
    private void ResetFoundPath(int temp_moveCount)
    {
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
    }
    #endregion

    private void ResetCharacterTile()
    {
        foreach (var item in dataOnTiles)
        {
            if (item.Value.character_isOn == true)
            {
                item.Value.canTouch = false;
                tilemap.SetTile(item.Key, item.Value.normalTile);
            }
        }
    }

    void UpdateLightNumber(int x, int y)
    {
        Vector3Int vec = new Vector3Int(x, y, 0);

        Vector3 screenPosition_pre = GetScreenPosition(previousPos);
        int cur_num = dataOnTiles[vec].light_num;
        if (cur_num >= 0)
        {
            lightNumber_tr[cur_num].position = new Vector3(screenPosition_pre.x + 50f, screenPosition_pre.y, screenPosition_pre.z);
        }

        Vector3 screenPosition = GetScreenPosition(vec);
        int pre_num = dataOnTiles[previousPos].light_num;
        if (pre_num >= 0)
        {
            lightNumber_tr[pre_num].position = new Vector3(screenPosition.x + 50f, screenPosition.y, screenPosition.z);
        }

        int temp_light = dataOnTiles[previousPos].light_num;        //가로등 번호 바꾸기
        dataOnTiles[previousPos].light_num = dataOnTiles[vec].light_num;
        dataOnTiles[vec].light_num = temp_light;
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

        CheckHandLightTile();

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
    private void CheckNeighborhood(int x, int y, string charName)
    {
        string str = string.Empty;
        Vector3Int vec = new Vector3Int(x, y, 0);
        characterTile[charName].IsExposed = false;

        for (int i = 0; i < dataOnTiles[vec].Neighborhoods.Length; i++)
        {
            Vector3Int target_vec = dataOnTiles[vec].Neighborhoods[i];
            if (dataOnTiles.ContainsKey(target_vec) == false)
            {
                continue;
            }

            if (dataOnTiles[target_vec].character_isOn == true)
            {
                str = dataOnTiles[target_vec].character_name;
                characterTile[str].IsExposed = true;
                characterTile[charName].IsExposed = true;
            }
            else if (dataOnTiles[target_vec].type == TilemapType.LIGHT && dataOnTiles[target_vec].isActive == true)  //해당 캐릭터의 주변에 활성화된 가로등이 있음
            {
                characterTile[charName].IsExposed = true;
            }
        }
    }

    /// <summary>
    ///  손전등이 가리키는 방향의 타일 조사 
    /// </summary>
    private void CheckHandLightTile()
    {
        Vector3Int target_pos;
        int light_dir = characterTile["Watson"].HandLight_Direct;
        Vector3Int start_pos = charPos_list["Watson"];

        bool loop = true;
        do
        {
            target_pos = dataOnTiles[start_pos].Neighborhoods[light_dir];
            if (dataOnTiles.ContainsKey(target_pos) == false)
            {
                loop = false;
                continue;
            }

            if (dataOnTiles[target_pos].type == TilemapType.POLICELINE || dataOnTiles[target_pos].type == TilemapType.LIGHT)
            {
                loop = false;
                continue;
            }

            if (dataOnTiles[target_pos].character_isOn == true)
            {
                string str = dataOnTiles[target_pos].character_name;
                characterTile[str].IsExposed = true;
                characterTile[str].SetImage();
                loop = false;
            }

            start_pos = target_pos;
        } while (loop == true);
    }
    #endregion

    /// <summary>
    /// 가로등 순서대로 지우기
    /// </summary>
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