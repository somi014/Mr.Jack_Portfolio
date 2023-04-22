using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillOrder
{
    NONE,
    After,
    Both,
    Select
}

/// <summary>
/// 초록(이동), 빨간 카드(알리바이) 정보 
/// </summary>
public class CharaterCard : MonoBehaviour
{
    [SerializeField] CharacterInfo charInfo;
    public CharacterInfo CharInfo
    {
        get => charInfo;
        set => charInfo = value;
    }

    private bool isSelected = false;
    public bool IsSelected
    {
        get => isSelected;
        set => isSelected = value;
    }

}
