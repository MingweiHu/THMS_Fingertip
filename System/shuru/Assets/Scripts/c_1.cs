using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;

public class c_1 : MonoBehaviour
{
    public TcpServer serverData;
    public int block_num;
    public int rounds;
    public bool isrecorded = false;

    private bool flag_start = false;
    private int Rno = 0;
    private int rand_num;
    private int result;
    private Transform g;
    public GameObject panduan;
    private Dictionary<string, int> code_dic = new Dictionary<string, int>();

    private StreamWriter sw;

    private DateTime startTime, currentTime;
    private TimeSpan deltaTime;

    // Start is called before the first frame update
    void Start()
    {
        //panduan.transform.GetComponent<MeshRenderer>().material.color = Color.blue;
        for (int i = 1; i <= 26; i++)
        {
            //code_dic.Add(((char)(i + 96)).ToString(), i);
            code_dic.Add((i + 96).ToString(), i);
        }
        if (isrecorded)
        {
            sw = new StreamWriter(Application.dataPath + "/DataRecords/data " + transform.name + " " + string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".csv");
            sw.WriteLine("Rno,Expected_choice,User_choice,Spent_time,Result");//毫秒
        }
    }

    private void Update()
    {
        currentTime = DateTime.Now;
        if (Rno > rounds)
        {
            ProgramExit();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            flag_start = !flag_start;
            if (flag_start)
            {
                rand_num = 1;
                g = transform.Find("Block_" + rand_num);
                Rno = 1;
                g.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matHighlight");
                startTime = DateTime.Now;
            }
            else
            {
                ProgramExit();
            }
        }
        if (serverData.msg.Length > 0 && flag_start)
        {
            string[] msg2 = serverData.msg.Split(' ');
            string item = msg2[0];
            int item_a = int.Parse(item);
            //Debug.Log(item_a);
            if (item_a <= 20)
            {
                //item.ToLower();

                //处理 记录
                //result = (code_dic[item] == rand_num);

                //result = (item_a == rand_num);
                if (item_a == rand_num)
                {
                    //Debug.Log("102");
                    panduan.transform.GetComponent<MeshRenderer>().material.color = Color.green;
                    result = 1;


                }
                else
                {
                    //Debug.Log("201");
                    panduan.transform.GetComponent<MeshRenderer>().material.color = Color.red;
                    result = 0;
                }
                deltaTime = currentTime - startTime;
                if (isrecorded)
                {
                    //Debug.Log("Round: " + Rno);
                    //Debug.Log("input key: " + item);
                    //Debug.Log("delte time: " + deltaTime.ToString());

                    //sw.WriteLine(Rno + "," + rand_num + "," + code_dic[item] + "," + deltaTime.TotalMilliseconds + "," + result);
                    sw.WriteLine(Rno + "," + rand_num + "," + item_a + "," + deltaTime.TotalMilliseconds + "," + result);
                }
                //next
                if (Rno <= rounds)
                {
                    Rno++;
                    //panduan.transform.GetComponent<MeshRenderer>().material.color = Color.white;
                    g.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matNone");
                    rand_num = 1; 
                    g = transform.Find("Block_" + rand_num);
                    g.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matHighlight");
                    startTime = DateTime.Now;
                }
            }
            //switch (item.Length)
            //{
            //    case 1:
            //        //if (Regex.Match(item, @"^[A-Za-z]+$").Success)

            //        if (item_a<=20)
            //        {
            //            //item.ToLower();

            //            //处理 记录
            //            //result = (code_dic[item] == rand_num);
            //            result = (item_a == rand_num);
            //            deltaTime = currentTime - startTime;
            //            if (isrecorded)
            //            {
            //                //Debug.Log("Round: " + Rno);
            //                //Debug.Log("input key: " + item);
            //                //Debug.Log("delte time: " + deltaTime.ToString());

            //                //sw.WriteLine(Rno + "," + rand_num + "," + code_dic[item] + "," + deltaTime.TotalMilliseconds + "," + result);
            //                sw.WriteLine(Rno + "," + rand_num + "," + item_a + "," + deltaTime.TotalMilliseconds + "," + result);
            //            }
            //            //next
            //            if (Rno <= rounds)
            //            {
            //                Rno++;
            //                g.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matNone");
            //                for (int i = rand_num; i == rand_num;) { rand_num = UnityEngine.Random.Range(1, block_num + 1); }
            //                g = transform.Find("Block_" + rand_num);
            //                g.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matHighlight");
            //                startTime = DateTime.Now;
            //            }
            //        }
            //        else
            //        {
            //            Debug.Log("invalid value0: " + item);
            //        }
            //        break;
            //    default:
            //        Debug.Log("invalid value: " + item);
            //        break;
            //}
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
        if (isrecorded)
        {
            sw.Close();
        }
    }

    private void OnApplicationQuit()
    {
        if (isrecorded)
        {
            sw.Close();
        }
    }
}
