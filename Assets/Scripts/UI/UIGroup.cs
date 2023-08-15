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
   private Transform popup_cur;

    private Image[] turn_img;

    private Button round_btn;
    private TextMeshProUGUI round_txt;

    private Image jack_exposed_img;

    private Button redCard_btn;
    private Button greenCard_btn;

    [SerializeField] private Image jack_img;
    [SerializeField] private GameObject redCard_jack_text;

    [SerializeField] private Image[] redCard;
    private int redCard_index;
    [SerializeField] private Image[] greenCard;

    private bool canClick = true;
    public bool CanClickUI
    {
        get => canClick;
        set => canClick = value;
    }
    private bool popup_open = false;

    [SerializeField] private AnimationCurve curve;
    [SerializeField] private Sprite[] innocentCard_img;

    private IEnumerator ieScale;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        turn_popup.localScale = Vector3.zero;
        redCard_popup.localScale = Vector3.zero;
        greenCard_popup.localScale = Vector3.zero;

        turn_img = new Image[4];
        for (int i = 0; i < turn_img.Length; i++)
        {
            turn_img[i] = transform.FindGameObject<Image>("TurnOrder_" + i).transform.GetChild(0).GetComponent<Image>();
        }

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
    public void SetRoundText(int round)
    {
        round_txt.text = round.ToString();

        bool round_boolen = round % 2 == 1;        //홀수 라운드에서는 이미지 활성화
        for (int i = 0; i < turn_img.Length; i++)
        {
            turn_img[i].enabled = round_boolen;
        }
    }

    public void SetJackExposed(bool exposed)
    {
        jack_exposed_img.enabled = exposed;
    }

    public void SetJackImage(int sprite)
    {
        jack_img.sprite = innocentCard_img[sprite];
        redCard_jack_text.SetActive(true);
    }

    public void SetRedCardPopup(int sprite)
    {
        redCard[redCard_index].gameObject.SetActive(true);
        redCard[redCard_index].sprite = innocentCard_img[sprite];
        redCard_index++;
    }

    public void SetGreenCardPopup(int sprite, int index)
    {
        greenCard[index].sprite = innocentCard_img[sprite];
    }

    public void OnClickUI(int index)
    {
        if (canClick == false)
        {
            return;
        }

        //알리바이 카드 & 움직일 캐릭터 카드 선택 중일때 UI 클릭하면 카드 내려가기
        PlayUIGroup.Instance.UIPopupCheck(popup_open);

        switch (index)
        {
            case 0:
                round_btn.enabled = false;
                StartCoroutine(IEPopupScale(turn_popup));
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

    IEnumerator IEPopupScale(Transform popup)
    {
        if (popup_cur == null)
        {
            popup_cur = popup;
        }
        else
        {
            if (popup != popup_cur)
            {
                //이전거 닫기
                yield return StartCoroutine(popup_cur.IESetScale(curve, Vector3.zero, 7f));
            }
        }

        if (ieScale != null)
        {
            StopCoroutine(ieScale);
        }

        popup_cur = popup;

        if (popup.localScale.x < 1f)
        {
            ieScale = popup.IESetScale(curve, Vector3.one, 7f);
        }
        else if (popup.localScale.x > 0f)
        {
            ieScale = popup.IESetScale(curve, Vector3.zero, 7f);
        }

        yield return StartCoroutine(ieScale);

        yield return null;
        round_btn.enabled = true;
        redCard_btn.enabled = true;
        greenCard_btn.enabled = true;

        canClick = true;
    }
}
