using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CommonFunctions;
using TMPro;

/// <summary>
/// 타일 위에 위치할 캐릭터 얼굴
/// </summary>
public class CharacterTile : MonoBehaviour
{
    Image exposed_img;          //가로등, 손전등, 캐릭터에 노출된 이미지
    Image unexposed_img;

    Image active_img;           //선택 가능 활성화

    GameObject des_go;
    TextMeshProUGUI des_txt;    

    RectTransform handLight_rt;
    private int handLight_direct = 0;
    public int HandLight_Direct { get => handLight_direct; }

    private string char_name;
    public string Char_Name
    {
        get => char_name;
        set => char_name = value;
    }

    private bool isExposed = false;
    public bool IsExposed
    {
        get => isExposed;
        set => isExposed = value;
    }

    private void Awake()
    {
        exposed_img = transform.FindGameObject<Image>("Exposed_img");
        exposed_img.enabled = true;
        unexposed_img = transform.FindGameObject<Image>("Unexposed_img");
        unexposed_img.enabled = false;

        active_img = transform.FindGameObject<Image>("Active_img");
        active_img.enabled = false;

        handLight_rt = transform.FindGameObject<RectTransform>("Hand_Light");

        des_go = transform.FindGameObject("TextBackground");
        des_txt = des_go.GetComponentInChildren<TextMeshProUGUI>();
        des_go.SetActive(false);
    }

    public void CharTextOff()
    {
        des_go.SetActive(false);
    }

    /// <summary>
    /// 캐릭터의 목격자에게 노출되었는지 이미지 교체
    /// </summary>
    /// <param name="_exposed"></param>
    public void SetImage()
    {
        exposed_img.enabled = isExposed;
        unexposed_img.enabled = !isExposed;
    }

    public void SetPos(Vector3 _pos)
    {
        transform.position = _pos;
    }

    /// <summary>
    /// Watson 손전등 방향 바꾸기
    /// </summary>
    /// <param name="_index"></param>
    public void HandLightPos(int _index)
    {
        switch (_index)
        {
            case 0:
                handLight_rt.rotation = Quaternion.Euler(0, 0, 90f);
                break;
            case 1:
                handLight_rt.rotation = Quaternion.Euler(0, 0, 20f);
                break;
            case 2:
                handLight_rt.rotation = Quaternion.Euler(0, 0, -20f);
                break;
            case 3:
                handLight_rt.rotation = Quaternion.Euler(0, 0, -90f);
                break;
            case 4:
                handLight_rt.rotation = Quaternion.Euler(0, 0, -160f);
                break;
            case 5:
                handLight_rt.rotation = Quaternion.Euler(0, 0, -210);
                break;
        }
        handLight_direct = _index;
    }

    public void SetText()
    {
        des_txt.text = "검거하기";

        des_go.SetActive(true);
    }
}
