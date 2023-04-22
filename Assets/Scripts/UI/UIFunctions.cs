using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CommonFunctions;

public class UIFunctions : MonoBehaviour
{
    List<Button> ui_btn;


    private void Awake()
    {

        ui_btn = new List<Button>(4);
        for (int i = 0; i < ui_btn.Count; i++)
        {
            ui_btn.Add(transform.GetChild(i).GetComponent<Button>());
            ui_btn[i].interactable = false;                          //ui 버튼 비활성화
        }
    }

   
}
