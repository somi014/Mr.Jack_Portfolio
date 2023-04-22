using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CommonFunctions;
using TMPro;

public class UIGroup : MonoBehaviour
{
    public static UIGroup Instance = null;

    [SerializeField] Transform turn_popup;
    [SerializeField] Transform redCard_popup;
    [SerializeField] Transform greenCard_popup;
    Transform popup_cur;

    Button round_btn;
    TextMeshProUGUI round_txt;

    Image jack_exposed_img;

    Button redCard_btn;
    Button greenCard_btn;

    [SerializeField] Image jack_img;
    [SerializeField] GameObject redCard_jack_text;

    [SerializeField] Image[] redCard;
    int redCard_index;
    [SerializeField] Image[] greenCard;

    bool canClick = true;         
    public bool CanClickUI
    {
        get => canClick;
        set => canClick = value;
    }
    bool popup_open = false;

    [SerializeField] AnimationCurve curve;
    [SerializeField] Sprite[] innocentCard_img;

    IEnumerator ieScale;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        turn_popup.localScale = Vector3.zero;
        redCard_popup.localScale = Vector3.zero;
        greenCard_popup.localScale = Vector3.zero;

        round_btn = transform.FindGameObject<Button>("TurnGroup");
        round_txt = transform.FindGameObject<TextMeshProUGUI>("Round_text");

        jack_exposed_img = transform.FindGameObject<Image>("Jack_exposed");

        redCard_btn = transform.FindGameObject<Button>("RedCardGroup");
        greenCard_btn = transform.FindGameObject<Button>("GreenCardGroup");

        redCard_jack_text.SetActive(false);
        redCard_index = 0;
    }

    /// <summary>
    /// 우측 상단 UI. 현재 라운드 표시 
    /// </summary>
    /// <param name="_round"></param>
    public void SetRoundText(int _round)
    {
        round_txt.text = _round.ToString();
    }

    public void SetJackExposed(bool _exposed)
    {
        jack_exposed_img.enabled = _exposed;
    }

    public void SetJackImage(int _sprite)
    {
        jack_img.sprite = innocentCard_img[_sprite];
        redCard_jack_text.SetActive(true);
    }

    public void SetRedCardPopup(int _sprite)
    {
        redCard[redCard_index].gameObject.SetActive(true);
        redCard[redCard_index].sprite = innocentCard_img[_sprite];
        redCard_index++;
    }

    public void SetGreenCardPopup(int _sprite, int _index)
    {
        greenCard[_index].sprite = innocentCard_img[_sprite];
    }

    public void OnClickUI(int _index)
    {
        if (canClick == false)
        {
            return;
        }

        //알리바이 카드 & 움직일 캐릭터 카드 선택 중일때 UI 클릭하면 카드 내려가기
        PlayUIGroup.Instance.UIPopupCheck(popup_open);

        switch (_index)
        {
            case 0:
                //round_btn.enabled = false;
                //StartCoroutine(IEPopupScale(turn_popup));
                break;
            case 1:
                redCard_btn.enabled = false;
                StartCoroutine(IEPopupScale(redCard_popup));
                break;
            case 2:
                greenCard_btn.enabled = false;
                StartCoroutine(IEPopupScale(greenCard_popup));
                break;
            default:
                break;
        }
        popup_open = !popup_open;
    }

    IEnumerator IEPopupScale(Transform _popup)
    {
        if (popup_cur == null)
        {
            popup_cur = _popup;
        }
        else
        {
            if (_popup != popup_cur)
            {
                //이전거 닫기
                yield return StartCoroutine(popup_cur.IESetScale(curve, Vector3.zero, 7f));
            }
        }

        if (ieScale != null)
        {
            StopCoroutine(ieScale);
        }

        popup_cur = _popup;

        if (_popup.localScale.x < 1f)
        {
            ieScale = _popup.IESetScale(curve, Vector3.one, 7f);
        }
        else if (_popup.localScale.x > 0f)
        {
            ieScale = _popup.IESetScale(curve, Vector3.zero, 7f);
        }

        yield return StartCoroutine(ieScale);

        yield return null;
        round_btn.enabled = true;
        redCard_btn.enabled = true;
        greenCard_btn.enabled = true;

        canClick = true;
    }
}
