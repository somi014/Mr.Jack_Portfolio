using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character Info", menuName = "Scriptable Object/Character Info", order = 1)]
public class CharacterInfo : ScriptableObject
{
    [SerializeField]
    private int characterIndex;                 //캐릭터 인덱스
    public int CharacterIndex { get => characterIndex; }

    [SerializeField]
    private string character_name;             //이름
    public string CharacterName { get => character_name; }

    [SerializeField]
    private SkillOrder order;                   //능력 사용 순서 0: 이동전 1: 이동후 -1: 상관없음
    public SkillOrder Order { get => order; }

    [SerializeField]
    private int maxMove = 3;                    //최대 이동 가능한 칸 수
    public int MaxMove { get => maxMove; }
}
