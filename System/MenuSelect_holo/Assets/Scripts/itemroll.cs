using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;

public class itemroll : MonoBehaviour
{
    private float speed = 30.0f;
    public TextMeshPro m_Text;
    private bool isStart = false;
    private StreamWriter sw_m;
    private int Rno_m = 0;
    private int rand_num;
    private int result;
    private int num_item;
    private DateTime startTime;
    private DateTime nowtime;
    public int m_rounds;
    //public TcpServer serverData;

    private void mrand_text(int num)
    {
        //Debug.Log("5000");
        switch (num)
        {
            case 1:
                m_Text.text = "item 1";
                break;
            case 2:
                m_Text.text = "item 2";
                break;
            case 3:
                m_Text.text = "item 3";
                break;
            case 4:
                m_Text.text = "item 4";
                break;
            case 5:
                m_Text.text = "item 5";
                break;
            case 6:
                m_Text.text = "item 6";
                break;
            case 7:
                m_Text.text = "item 7";
                break;
            case 8:
                m_Text.text = "item 8";
                break;
            default:
                m_Text.text = "error";
                break;
        }
    }
    public void DisableChildObject(string childName)
    {
        Transform childTransform = transform.Find(childName);
        if (childTransform != null)
        {
            childTransform.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Child object not found: " + childName);
        }
    }

    public void AbleChildObject(string childName)
    {
        Transform childTransform = transform.Find(childName);
        if (childTransform != null)
        {
            childTransform.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Child object not found: " + childName);
        }
    }

    public void SelectChildObject(string childName)
    {
        Transform childTransform = transform.Find(childName);
        if (childTransform != null)
        {
            childTransform.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matSelected");
        }
        else
        {
            Debug.LogWarning("Child object not found: " + childName);
        }
    }

    public void NoneChildObject(string childName)
    {
        Transform childTransform = transform.Find(childName);
        if (childTransform != null)
        {
            childTransform.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matNone");
        }
        else
        {
            Debug.LogWarning("Child object not found: " + childName);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject obj_111 = GameObject.Find("Diagnostics");
        obj_111.SetActive(false);
        num_item = 2;

}

// Update is called once per frame
void Update()
    {
        if (!isStart && Input.GetKeyDown(KeyCode.Space))
        {
            isStart = true;

            Debug.Log("The experiment begins");
            sw_m = new StreamWriter(Application.dataPath + "/DataRecords/m_data " + transform.name + " " + string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".csv");
            sw_m.WriteLine("Rno,Expected_choice,User_choice,Spent_time,Result");//毫秒
            startTime = DateTime.Now;
            rand_num = UnityEngine.Random.Range(1, 8);
            mrand_text(rand_num);
            Rno_m += 1;
        }
        else
        {
            if (Rno_m > m_rounds)
            {
                sw_m.WriteLine(0 + "," + 0 + "," + 0 + "," + (nowtime - startTime).TotalMilliseconds + "," + 0);
                ProgramExit();
            }
            //if (serverData.msg.Length > 0)
            //{
            //    string[] msg2 = serverData.msg.Split(' ');
            //    string item = msg2[0];
            //    int item_a = int.Parse(item);
            //    if (Rno_m > m_rounds)
            //    {
            //        sw_m.WriteLine(0 + "," + 0 + "," + 0 + "," + (nowtime - startTime).TotalMilliseconds + "," + 0);
            //        ProgramExit();
            //    }
            //    //if (Rno_m > m_rounds)
            //    //{
            //    //    sw_m.WriteLine(0 + "," + 0 + "," + 0 + "," + (nowtime - startTime).TotalMilliseconds + "," + 0);
            //    //    ProgramExit();
            //    //}
            //    if (item_a == 1)
            //    {
            //        if (this.transform.position.x > -12)
            //        {
            //            LeftRoll();

            //        }
            //    }
            //    else if (item_a == 2)
            //    {
            //        if (this.transform.position.x < 2)
            //        {
            //            RightRoll();

            //        }
            //    }
            //    if (item_a == 3)
            //    {
            //if (num_item == rand_num)
            //{
            //    nowtime = DateTime.Now;
            //    result = 1;
            //    sw_m.WriteLine(Rno_m + "," + rand_num + "," + item_a + "," + (nowtime - startTime).TotalMilliseconds + "," + result);
            //    while (num_item == rand_num)
            //    {
            //        rand_num = UnityEngine.Random.Range(1, 8);
            //    }
            //    mrand_text(rand_num);
            //    Rno_m += 1;
            //    startTime = DateTime.Now;
            //}
            //    }

            //}



            if (Input.GetKeyUp(KeyCode.LeftArrow))
            {

                if (num_item < 8 )
                {
                    LeftRoll();

                }
            }

            if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                if (num_item > 1)
                {
                    RightRoll();

                }
            }

            switch (num_item)
            {

                case 1:
                    AbleChildObject("menu_item1");
                    AbleChildObject("menu_item2");
                    DisableChildObject("menu_item3");
                    DisableChildObject("menu_item4");
                    DisableChildObject("menu_item5");
                    DisableChildObject("menu_item6");
                    DisableChildObject("menu_item7");
                    DisableChildObject("menu_item8");
                    SelectChildObject("menu_item1");
                    NoneChildObject("menu_item2");
                    
                    break;
                case 2:
                    AbleChildObject("menu_item1");
                    AbleChildObject("menu_item2");
                    AbleChildObject("menu_item3");
                    DisableChildObject("menu_item4");
                    DisableChildObject("menu_item5");
                    DisableChildObject("menu_item6");
                    DisableChildObject("menu_item7");
                    DisableChildObject("menu_item8");
                    SelectChildObject("menu_item2");
                    NoneChildObject("menu_item1");
                    NoneChildObject("menu_item3");
                    
                    break;
                case 3:
                    DisableChildObject("menu_item1");
                    AbleChildObject("menu_item2");
                    AbleChildObject("menu_item3");
                    AbleChildObject("menu_item4");
                    DisableChildObject("menu_item5");
                    DisableChildObject("menu_item6");
                    DisableChildObject("menu_item7");
                    DisableChildObject("menu_item8");
                    SelectChildObject("menu_item3");
                    NoneChildObject("menu_item2");
                    NoneChildObject("menu_item4");
                    
                    break;
                case 4:
                    DisableChildObject("menu_item1");
                    DisableChildObject("menu_item2");
                    AbleChildObject("menu_item3");
                    AbleChildObject("menu_item4");
                    AbleChildObject("menu_item5");
                    DisableChildObject("menu_item6");
                    DisableChildObject("menu_item7");
                    DisableChildObject("menu_item8");
                    SelectChildObject("menu_item4");
                    NoneChildObject("menu_item3");
                    NoneChildObject("menu_item5");
                    
                    break;
                case 5:
                    DisableChildObject("menu_item1");
                    DisableChildObject("menu_item2");
                    DisableChildObject("menu_item3");
                    AbleChildObject("menu_item4");
                    AbleChildObject("menu_item5");
                    AbleChildObject("menu_item6");
                    DisableChildObject("menu_item7");
                    DisableChildObject("menu_item8");
                    SelectChildObject("menu_item5");
                    NoneChildObject("menu_item4");
                    NoneChildObject("menu_item6");
                    
                    break;
                case 6:
                    DisableChildObject("menu_item1");
                    DisableChildObject("menu_item2");
                    DisableChildObject("menu_item3");
                    DisableChildObject("menu_item4");
                    AbleChildObject("menu_item5");
                    AbleChildObject("menu_item6");
                    AbleChildObject("menu_item7");
                    DisableChildObject("menu_item8");
                    SelectChildObject("menu_item6");
                    NoneChildObject("menu_item5");
                    NoneChildObject("menu_item7");
                    
                    break;
                case 7:
                    DisableChildObject("menu_item1");
                    DisableChildObject("menu_item2");
                    DisableChildObject("menu_item3");
                    DisableChildObject("menu_item4");
                    DisableChildObject("menu_item5");
                    AbleChildObject("menu_item6");
                    AbleChildObject("menu_item7");
                    AbleChildObject("menu_item8");
                    SelectChildObject("menu_item7");
                    NoneChildObject("menu_item6");
                    NoneChildObject("menu_item8");
                    
                    break;
                case 8:
                    DisableChildObject("menu_item1");
                    DisableChildObject("menu_item2");
                    DisableChildObject("menu_item3");
                    DisableChildObject("menu_item4");
                    DisableChildObject("menu_item5");
                    DisableChildObject("menu_item6");
                    AbleChildObject("menu_item7");
                    AbleChildObject("menu_item8");
                    SelectChildObject("menu_item8");
                    NoneChildObject("menu_item7");
                    
                    break;
            }
        }

        
            


        //if (this.transform.position.x == -2)
        //{
        //    DisableChildObject("menu_item1");
        //    AbleChildObject("menu_item3");
        //}

        //if (this.transform.position.x == 0)
        //{
        //    AbleChildObject("menu_item3");
        //    AbleChildObject("menu_item1");
        //}

        //if (this.transform.position.x == 2)
        //{
        //    DisableChildObject("menu_item3");
        //    AbleChildObject("menu_item1");
        //}
    }

    public void select_button()
    {
        if (num_item == rand_num)
        {
            nowtime = DateTime.Now;
            result = 1;
            sw_m.WriteLine(Rno_m + "," + rand_num + "," + num_item + "," + (nowtime - startTime).TotalMilliseconds + "," + result);
            while (num_item == rand_num)
            {
                rand_num = UnityEngine.Random.Range(1, 8);
            }
            mrand_text(rand_num);
            Rno_m += 1;
            startTime = DateTime.Now;
        }
    }

    public void LeftRoll()
    {
        //this.transform.position = Vector3.MoveTowards(this.transform.position,new Vector3(this.transform.position.x-10, this.transform.position.y, this.transform.position.z), speed * Time.deltaTime);
        this.transform.Translate(-2f,0,0);
        num_item = num_item + 1;
    }

    public void RightRoll()
    {
        this.transform.Translate(2f, 0, 0);
        num_item = num_item - 1;
    }

    private void ProgramExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
    }

    private void OnDestroy()
    {

        sw_m.Close();
        

    }

    private void OnApplicationQuit()
    {
        sw_m.Close();
        
    }
}
