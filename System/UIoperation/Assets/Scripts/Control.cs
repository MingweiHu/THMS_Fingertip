using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text.RegularExpressions;


public class Control : MonoBehaviour
{
    // Start is called before the first frame update

    public TcpServer serverData;
    public GameObject UI1;
    public GameObject UI2;
    public GameObject UI4;
    public GameObject UI5;
    public GameObject UI6;
    public Menu mScript;
    public Call cScript;
    public Keyboard kScript;
    public Text m_Text;
    public Text p_Text;
    public Image i_p;
    public int m_rounds;
    public int p_rounds;
    public int x_rounds;
    //public GameObject panduan;

    //private int num_z = 0;//组数
    //private int num_n = 0;//数据总量
    //private int[] num_a = new int[60]; //存放随机数据
    //private int numu_n = 0;//用户数据总量
    //private int[] numu_a = new int[60]; //存放用户数据
    private StreamWriter sw_m;
    private StreamWriter sw_p;
    private StreamWriter sw_x;
    private bool flag_start = false;
    private int rand_num;
    private int result;
    private int Rno_m = 0;
    private int prand_num;
    private int result_p;
    private int Rno_p = 0;
    //private int xrand_num;
    private int result_x;
    private int Rno_x = 0;

    private DateTime startTime;
    private DateTime nowtime;
    private bool isStart = false;

    void Start()
    {
        /*
        //m_Text = GetComponent<Text>();
        sw_m = new StreamWriter(Application.dataPath + "/DataRecords/m_data " + transform.name + " " + string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".csv");
        sw_m.WriteLine("Rno,Expected_choice,User_choice,Spent_time,Result");//毫秒
        //Debug.Log("5000");
        sw_p = new StreamWriter(Application.dataPath + "/DataRecords/p_data " + transform.name + " " + string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".csv");
        sw_p.WriteLine("Rno,Expected_choice,User_choice,Spent_time,Result");//毫秒
        sw_x = new StreamWriter(Application.dataPath + "/DataRecords/x_data " + transform.name + " " + string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".csv");
        sw_x.WriteLine("Rno,Expected_choice,User_choice,Spent_time,Result");//毫秒
        */
        //rand_num = UnityEngine.Random.Range(1, 5);
        ////Debug.Log("5000");
        //mrand_text(rand_num);
        //Rno_m += 1;
        ////m_Text.text = "increase volume";
        //prand_num = UnityEngine.Random.Range(1, 3);
        ////Debug.Log("5000");
        //prand_text(prand_num);
        //Rno_p += 1;
        i_p.color = Color.blue;

        //Rno_x += 1;

    }

    // Update is called once per frame
    void Update()
    {
        nowtime = DateTime.Now;
        if (!isStart && Input.GetKeyDown(KeyCode.Space))
        {
            isStart = true;
            Debug.Log("The experiment begins");
            //m_Text = GetComponent<Text>();
            sw_m = new StreamWriter(Application.dataPath + "/DataRecords/m_data " + transform.name + " " + string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".csv");
            sw_m.WriteLine("Rno,Expected_choice,User_choice,Spent_time,Result");//毫秒
                                                                                //Debug.Log("5000");
            sw_p = new StreamWriter(Application.dataPath + "/DataRecords/p_data " + transform.name + " " + string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".csv");
            sw_p.WriteLine("Rno,Expected_choice,User_choice,Spent_time,Result");//毫秒
            sw_x = new StreamWriter(Application.dataPath + "/DataRecords/x_data " + transform.name + " " + string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".csv");
            sw_x.WriteLine("Rno,Expected_choice,User_choice,Spent_time,Result");//毫秒
            startTime = DateTime.Now;
            rand_num = UnityEngine.Random.Range(1, 5);
            //Debug.Log("5000");
            mrand_text(rand_num);
            Rno_m += 1;
            //m_Text.text = "increase volume";
            prand_num = UnityEngine.Random.Range(1, 3);
            //Debug.Log("5000");
            prand_text(prand_num);
            Rno_p += 1;
            //i_p.color = Color.blue;

            Rno_x += 1;
        }
        else
        {
            if (serverData.msg.Length > 0)
            {
                string[] msg2 = serverData.msg.Split(' ');
                string item = msg2[0];
                int item_a = int.Parse(item);
                if (Rno_x > x_rounds)
                {
                    
                    sw_x.WriteLine(0 + "," + 0 + "," + 0 + "," + (nowtime - startTime).TotalMilliseconds + "," + 0);
                    ProgramExit();
                }
                if (!UI1.activeSelf && !UI2.activeSelf && !UI4.activeSelf && !UI5.activeSelf && !UI6.activeSelf && (item_a == 1))
                {
                    result_x = 1;
                    sw_x.WriteLine(Rno_x + "," + "1" + "," + item_a + "," + "none" + "," + result_x);
                    UI1.SetActive(true);
                    Rno_x += 1;
                }
                else if (UI1.activeSelf && (item_a == 1))
                {
                    result_x = 1;
                    sw_x.WriteLine(Rno_x + "," + "1" + "," + item_a + "," + "none" + "," + result_x);
                    UI1.SetActive(false);
                    Rno_x += 1;
                }
                else
                {
                    result_x = 0;
                    sw_x.WriteLine(Rno_x + "," + "1" + "," + item_a + "," + "none" + "," + result_x);
                    Rno_x += 1;
                }
                if (UI4.activeSelf)
                {
                    if (Rno_p > p_rounds)
                    {
                        sw_p.WriteLine(0 + "," + 0 + "," + 0 + "," + (nowtime - startTime).TotalMilliseconds + "," + 0);
                        ProgramExit();
                    }

                    if (item_a == 1 && item_a == prand_num)
                    {
                        result_p = 1;
                        i_p.color = Color.green;
                        sw_p.WriteLine(Rno_p + "," + prand_num + "," + item_a + "," + "none" + "," + result_p);
                        prand_num = UnityEngine.Random.Range(1, 3);
                        prand_text(prand_num);
                        Rno_p += 1;
                        //cScript.Up();
                    }
                    else if (item_a == 2 && item_a == prand_num)
                    {
                        result_p = 1;
                        i_p.color = Color.green;
                        sw_p.WriteLine(Rno_p + "," + prand_num + "," + item_a + "," + "none" + "," + result_p);
                        prand_num = UnityEngine.Random.Range(1, 3);
                        prand_text(prand_num);
                        Rno_p += 1;
                        //cScript.Down1();
                    }
                    else
                    {
                        result_p = 0;
                        i_p.color = Color.red;
                        sw_p.WriteLine(Rno_p + "," + prand_num + "," + item_a + "," + "none" + "," + result_p);
                        prand_num = UnityEngine.Random.Range(1, 3);
                        prand_text(prand_num);
                        Rno_p += 1;
                    }
                }
                if (UI5.activeSelf)
                {
                    if (item_a == 2)
                    {
                        cScript.Down2();
                    }
                }
                if (UI2.activeSelf)
                {
                    if (Rno_m > m_rounds)
                    {
                        sw_m.WriteLine(0 + "," + 0 + "," + 0 + "," + (nowtime - startTime).TotalMilliseconds + "," + 0);
                        ProgramExit();
                    }

                    if (item_a == 1 && item_a == rand_num)
                    {
                        mScript.IncreaseVolume();
                        result = 1;
                        sw_m.WriteLine(Rno_m + "," + rand_num + "," + item_a + "," + "none" + "," + result);
                        rand_num = UnityEngine.Random.Range(1, 5);
                        mrand_text(rand_num);
                        Rno_m += 1;
                    }
                    else if (item_a == 2 && item_a == rand_num)
                    {
                        mScript.ReduceVolume();
                        result = 1;
                        sw_m.WriteLine(Rno_m + "," + rand_num + "," + item_a + "," + "none" + "," + result);
                        rand_num = UnityEngine.Random.Range(1, 5);
                        mrand_text(rand_num);
                        Rno_m += 1;
                    }
                    else if (item_a == 3 && item_a == rand_num)
                    {
                        mScript.Previous_song();
                        result = 1;
                        sw_m.WriteLine(Rno_m + "," + rand_num + "," + item_a + "," + "none" + "," + result);
                        rand_num = UnityEngine.Random.Range(1, 5);
                        mrand_text(rand_num);
                        Rno_m += 1;
                    }
                    else if (item_a == 4 && item_a == rand_num)
                    {
                        mScript.Next_song();
                        result = 1;
                        sw_m.WriteLine(Rno_m + "," + rand_num + "," + item_a + "," + "none" + "," + result);
                        rand_num = UnityEngine.Random.Range(1, 5);
                        mrand_text(rand_num);
                        Rno_m += 1;
                    }
                    else if (item_a == 5)
                    {
                        mScript.PlayorPause_song();
                        result = 0;
                        sw_m.WriteLine(Rno_m + "," + rand_num + "," + item_a + "," + "none" + "," + result);
                        rand_num = UnityEngine.Random.Range(1, 5);
                        mrand_text(rand_num);
                        Rno_m += 1;
                    }
                    else if (item_a == 0)
                    {
                        ;
                    }
                    else
                    {
                        mScript.PlayorPause_song();
                        result = 0;
                        sw_m.WriteLine(Rno_m + "," + rand_num + "," + item_a + "," + "none" + "," + result);
                        rand_num = UnityEngine.Random.Range(1, 5);
                        mrand_text(rand_num);
                        Rno_m += 1;
                    }

                }
                if (UI6.activeSelf)
                {
                    if (item_a == 1)
                    {
                        kScript.pushKey1();
                    }
                    else if (item_a == 2)
                    {
                        kScript.pushKey2();
                    }
                    else if (item_a == 3)
                    {
                        kScript.pushKey3();
                    }
                    else if (item_a == 4)
                    {
                        kScript.pushKey4();
                    }
                    else if (item_a == 5)
                    {
                        kScript.pushKey5();
                    }
                    else if (item_a == 6)
                    {
                        kScript.pushKey6();
                    }
                    else if (item_a == 7)
                    {
                        kScript.pushKey7();
                    }
                    else if (item_a == 8)
                    {
                        kScript.pushKey8();
                    }
                    else if (item_a == 9)
                    {
                        kScript.pushKey9();
                    }
                    else
                    {
                        kScript.pushKey10();
                    }
                }
            }
        }
        
        serverData.msg = "";
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
        sw_p.Close();
        sw_x.Close();

    }

    private void OnApplicationQuit()
    {
        sw_m.Close();
        sw_p.Close();
        sw_x.Close();
    }

    private void mrand_text(int num)
    {
        //Debug.Log("5000");
        switch (num)
        {
            case 1:
                m_Text.text = "increase volume";
                break;
            case 2:
                m_Text.text = "decrease volume";
                break;
            case 3:
                m_Text.text = "Previous song";
                break;
            case 4:
                m_Text.text = "Next song";
                break;
            default:
                m_Text.text = "error";
                break;
        }
    }

    private void prand_text(int num)
    {
        //Debug.Log("5000");
        switch (num)
        {
            case 1:
                p_Text.text = "reject";
                break;
            case 2:
                p_Text.text = "answer";
                break;
            //case 3:
            //    m_Text.text = "Previous song";
            //    break;
            //case 4:
            //    m_Text.text = "Next song";
            //    break;
            default:
                p_Text.text = "error";
                break;
        }
    }
}
