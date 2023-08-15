using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleBackground : MonoBehaviour
{
    private Material mat;
    [SerializeField] private float speed;
    private float offSetY;

    private void Awake()
    {
        mat = GetComponent<Image>().material;

        Vector2 Offset = new Vector2(0, 0.2f);
        mat.SetTextureOffset("_MainTex", Offset);
    }

    void Update()
    {
        SetTextureOffset();
    }
   
    void SetTextureOffset()
    {
        offSetY -= speed * Time.deltaTime;
        if (offSetY < 0)
            offSetY = offSetY % 1.0f;

        Vector2 Offset = new Vector2(0, offSetY);

        mat.SetTextureOffset("_MainTex", Offset);
       
    }
}
