using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TilemapType
{
    LOAD,
    LIGHT,
    MANHOLE,
    OBSTACLE,
    POLICELINE
}

public class TileInfo : MonoBehaviour
{
    public TilemapType type;

    public int moveIndex; //캐릭터를 기준으로 몇번째 칸인지

    public Vector3Int tile_pos { get; set; }

    //캐릭터 유무, 정보
    public bool character_isOn { get; set; }
    public string character_name { get; set; }

    // 가로등, 맨홀 활성화 유무
    public bool isActive { get; set; }

    //라운드에 따라 비활성화될 가로등 순서
    //deleteLight_list를 여기에 넣고 tile_pos에 따라서 넘버링?
    public int light_num { get; set; }

    //이동 가능한 상태인지
    public bool canTouch { get; set; }

    //클릭된 상태인지
    public bool isSelected { get; set; }

    public Tile[] normalTiles { get; set; }
    public Tile normalTile
    {
        get
        {
            if(isActive == true)
            {
                return normalTiles[0];
            }
            else
            {
                return normalTiles[1];
            }
        }
    }

    public Tile[] selectTiles { get; set; } //가로등, 맨홀 상태에 따른 타일 리스트
    public Tile selectTile
    {
        //탈출구면 비활성화 상태일 때 탈출하기 버튼? 문구 띄우기
        get
        {
            if (isActive == true)
            {
                return selectTiles[0];
            }
            else
            {
                return selectTiles[1];
            }
        }
    }

    private Vector3Int[] neighborhoods_tile;
    public Vector3Int[] Neighborhoods
    {
        get => neighborhoods_tile;
    }

    public TileInfo()
    {
        neighborhoods_tile = new Vector3Int[6];
    }

    public void SetNeighborgoods()
    {
        if (tile_pos.y % 2 == 0)    //y 짝수
        {
            neighborhoods_tile[0] = new Vector3Int(tile_pos.x + 1, tile_pos.y);
            neighborhoods_tile[1] = new Vector3Int(tile_pos.x, tile_pos.y + 1);
            neighborhoods_tile[2] = new Vector3Int(tile_pos.x - 1, tile_pos.y + 1);
            neighborhoods_tile[3] = new Vector3Int(tile_pos.x - 1, tile_pos.y);
            neighborhoods_tile[4] = new Vector3Int(tile_pos.x - 1, tile_pos.y - 1);
            neighborhoods_tile[5] = new Vector3Int(tile_pos.x, tile_pos.y - 1);
        }
        else                        //y 홀수 
        {
            neighborhoods_tile[0] = new Vector3Int(tile_pos.x + 1, tile_pos.y);
            neighborhoods_tile[1] = new Vector3Int(tile_pos.x + 1, tile_pos.y + 1);
            neighborhoods_tile[2] = new Vector3Int(tile_pos.x, tile_pos.y + 1);
            neighborhoods_tile[3] = new Vector3Int(tile_pos.x - 1, tile_pos.y);
            neighborhoods_tile[4] = new Vector3Int(tile_pos.x, tile_pos.y - 1);
            neighborhoods_tile[5] = new Vector3Int(tile_pos.x + 1, tile_pos.y - 1);
        }
    }
}