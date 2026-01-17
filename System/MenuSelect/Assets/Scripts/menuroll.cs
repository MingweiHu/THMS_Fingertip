using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class menuroll : MonoBehaviour
{
    public string s_menu="起；承；转；合";
    [Range(0.01f,5.0f)]
    public float roll_time = 1.0f;
    [Range(1,10)]
    public int visiable_range = 3;
    [Range(1,20)]
    public int flag_select=1;


    // Start is called before the first frame update
    void Start()
    {
        PrepareMenu();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {

        }
    }

    private void PrepareMenu()
    {
        if(s_menu != "")
        {
            //分割
            string[] s = s_menu.Split(';', '；');
            int size_menu = s.Length;

            //基本判断
            if (visiable_range % 2 == 0) visiable_range--;
            //if (size_menu < visiable_range)
            if (flag_select > size_menu)
            {
                ThrowEx("Error, Flag_select is larger than menu size");
            }
            flag_select--;

            //生成item
            for (int i = flag_select - 1; (i > (flag_select - visiable_range / 2 - 1)) && (i > -1); i--) 
            {
                Debug.Log(i);
            }
            Debug.Log(flag_select);
            for (int i = flag_select + 1; (i < (flag_select + visiable_range / 2 + 1)) && (i < size_menu); i++) 
            {
                Debug.Log(i);
            }
        }
        else
        {
            ThrowEx("Error, S_menu is null! ");
        }
    }

    private void RollMenu()
    {

    }

    private void ThrowEx(string s)
    {
        Debug.Log(s);
        ProgramExit();
    }

    private void ProgramExit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
