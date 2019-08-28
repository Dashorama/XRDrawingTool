using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InvertColorsOnSelect : MonoBehaviour
{
    public Image[] buttonImages;

    private void Start()
    {
        Select(1);
    }

    public void Select(int index)
    {
        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (i == index)
            {
                buttonImages[i].color = Color.black;
                buttonImages[i].transform.GetChild(0).GetComponent<Image>().color = Color.white;
            }
            else
            {
                buttonImages[i].color = Color.white;
                buttonImages[i].transform.GetChild(0).GetComponent<Image>().color = Color.black;
            }
        }
    }

}
