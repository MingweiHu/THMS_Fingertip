using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text.RegularExpressions;

public class Keyboard : MonoBehaviour
{
    public GameObject UI1;
    public GameObject UI2;
    public GameObject UI4;
    public GameObject UI5;
    public GameObject UI6;

    public GameObject tabCardPrefab;
    public GameObject refPanel;
    public GameObject inputPanel;
    public Button k1;
    public Button k2;
    public Button k3;
    public Button k4;
    public Button k5;
    public Button k6;
    public Button k7;
    public Button k8;
    public Button k9;
    public Button onKey;

    private float lastPos1 = 0.0f;
    private float lastPos2=0.0f;
    private int num_z = 0;//组数
    private int num_n = 0;//数据总量
    private int[] num_a = new int[60]; //存放随机数据
    private int numu_n = 0;//用户数据总量
    private int[] numu_a = new int[60]; //存放用户数据
    private StreamWriter sw_9;
    int result;

    bool isStart = false;
    private DateTime startTime;
    private DateTime nowtime;


    private List<int> inputList;
    private List<int> refList;
    private int maxLength =10;

    // Start is called before the first frame update
    void Start()
    {
        k1.onClick.AddListener(pushKey1);
        k2.onClick.AddListener(pushKey2);
        k3.onClick.AddListener(pushKey3);
        k4.onClick.AddListener(pushKey4);
        k5.onClick.AddListener(pushKey5);
        k6.onClick.AddListener(pushKey6);
        k7.onClick.AddListener(pushKey7);
        k8.onClick.AddListener(pushKey8);
        k9.onClick.AddListener(pushKey9);
        onKey.onClick.AddListener(openKeyborad);
        //sw_9 = new StreamWriter(Application.dataPath + "/DataRecords/data " + transform.name + " " + string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".csv");
        //sw_9.WriteLine("Rno,Expected_choice,User_choice,Spent_time,Result");//毫秒

        //lastPos1 = refPanel.transform.localPosition.x;
        //lastPos2 = inputPanel.transform.localPosition.x;
        lastPos1 = 85.0f;
        lastPos2 = 85.0f;
        Debug.Log(lastPos1);
        Debug.Log(lastPos2);
        refList = new List<int>();
        inputList = new List<int>();

        generateTabs();
    }

    // Update is called once per frame
    void Update()
    {
        nowtime = DateTime.Now;
        if (!isStart && Input.GetKeyDown(KeyCode.Space))
        {
            isStart = true;
            Debug.Log("The experiment begins");
            sw_9 = new StreamWriter(Application.dataPath + "/DataRecords/data " + transform.name + " " + string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".csv");
            sw_9.WriteLine("Rno,Expected_choice,User_choice,Spent_time,Result");//毫秒
            startTime = DateTime.Now;
        }
        else
        {
            if (inputPanel.transform.childCount == 10)
            {
                for (int i = 0; i < inputPanel.transform.childCount; i++)
                {
                    Destroy(inputPanel.transform.GetChild(i).gameObject);
                }
                inputList.Clear();
                lastPos2 = 85.0f;
                generateTabs();
                num_z = num_z + 1;
            }
            if (num_z == 5)
            {
                string s = string.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss"));
                for (int i = 0; i < numu_n; i++)
                {
                    if (num_a[i] == numu_a[i])
                    {
                        result = 1;
                    }
                    else
                        result = 0;
                    sw_9.WriteLine(i + "," + num_a[i] + "," + numu_a[i] + "," + "none" + "," + result);
                }
                //sw_9.Close();
                sw_9.WriteLine(0 + "," + 0 + "," + 0 + "," + (nowtime - startTime).TotalMilliseconds + "," + 0);
                ProgramExit();
            }
        }
        
    }

    public void openKeyborad()
    {
        UI1.SetActive(false);
        UI6.SetActive(true);
    }

    public void generateTabs()
    {
        for(int i = 0; i < refPanel.transform.childCount; i++)
        {
            Destroy(refPanel.transform.GetChild(i).gameObject);
        }
        refList.Clear();

        lastPos1 = 85.0f;
        for (int i = 0; i < maxLength; i++)
        {
            int randnum = UnityEngine.Random.Range(1, 10);
            GameObject newTab = Instantiate(tabCardPrefab, refPanel.transform);
            //Image img = newTab.GetComponent<Image>();

            //img.sprite = Resources.Load<Sprite>("icons/icon" + randnum);
            newTab.GetComponent<Image>().sprite = Resources.Load<Sprite>("icons/icon" + randnum);
            //img.sprite = Resources.Load<Sprite>("icons/icon1");
            float hPos = lastPos1;
            float vPos = refPanel.transform.localPosition.y - 140.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos1 += 100.0f;
            num_a[num_n] = randnum;
            num_n += 1;
            refList.Add(randnum);
            
            
        }
    }

    public void pushKey1()
    {
        if (inputList.Count < maxLength)
        {
            //Debug.Log(inputList.Count);
            GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
            Image img = newTab.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("icons/icon1");
            float hPos = lastPos2;
            float vPos = inputPanel.transform.localPosition.y - 35.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos2 += 100.0f;
            numu_a[numu_n] = 1;
            numu_n += 1;
            inputList.Add(1);
        }


    }

    public void pushKey2()
    {
        if (inputList.Count < maxLength)
        {
            //Debug.Log(inputList.Count);
            GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
            Image img = newTab.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("icons/icon2");
            float hPos = lastPos2;
            float vPos = inputPanel.transform.localPosition.y - 35.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos2 += 100.0f;
            numu_a[numu_n] = 2;
            numu_n += 1;
            inputList.Add(2);
        }
    }

    public void pushKey3()
    {
        if (inputList.Count < maxLength)
        {
            //Debug.Log(inputList.Count);
            GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
            Image img = newTab.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("icons/icon3");
            float hPos = lastPos2;
            float vPos = inputPanel.transform.localPosition.y - 35.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos2 += 100.0f;
            numu_a[numu_n] = 3;
            numu_n += 1;
            inputList.Add(3);
        }
    }
    public void pushKey4()
    {
        if (inputList.Count < maxLength)
        {
            //Debug.Log(inputList.Count);
            GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
            Image img = newTab.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("icons/icon4");
            float hPos = lastPos2;
            float vPos = inputPanel.transform.localPosition.y - 35.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos2 += 100.0f;
            numu_a[numu_n] = 4;
            numu_n += 1;
            inputList.Add(4);
        }
    }
    public void pushKey5()
    {
        if (inputList.Count < maxLength)
        {
            //Debug.Log(inputList.Count);
            GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
            Image img = newTab.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("icons/icon5");
            float hPos = lastPos2;
            float vPos = inputPanel.transform.localPosition.y - 35.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos2 += 100.0f;
            numu_a[numu_n] = 5;
            numu_n += 1;
            inputList.Add(5);
        }
    }
    public void pushKey6()
    {
        if (inputList.Count < maxLength)
        {
            //Debug.Log(inputList.Count);
            GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
            Image img = newTab.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("icons/icon6");
            float hPos = lastPos2;
            float vPos = inputPanel.transform.localPosition.y - 35.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos2 += 100.0f;
            numu_a[numu_n] = 6;
            numu_n += 1;
            inputList.Add(6);
        }
    }
    public void pushKey7()
    {
        if (inputList.Count < maxLength)
        {
            //Debug.Log(inputList.Count);
            GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
            Image img = newTab.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("icons/icon7");
            float hPos = lastPos2;
            float vPos = inputPanel.transform.localPosition.y - 35.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos2 += 100.0f;
            numu_a[numu_n] = 7;
            numu_n += 1;
            inputList.Add(7);
        }
    }
    public void pushKey8()
    {
        if (inputList.Count < maxLength)
        {
            //Debug.Log(inputList.Count);
            GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
            Image img = newTab.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("icons/icon8");
            float hPos = lastPos2;
            float vPos = inputPanel.transform.localPosition.y - 35.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos2 += 100.0f;
            numu_a[numu_n] = 8;
            numu_n += 1;
            inputList.Add(8);
        }
    }
    public void pushKey9()
    {
        if (inputList.Count < maxLength)
        {
            //Debug.Log(inputList.Count);
            GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
            Image img = newTab.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("icons/icon9");
            float hPos = lastPos2;
            float vPos = inputPanel.transform.localPosition.y - 35.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos2 += 100.0f;
            numu_a[numu_n] = 9;
            numu_n += 1;
            inputList.Add(9);
        }
    }

    public void pushKey10()
    {
        if (inputList.Count < maxLength)
        {
            //Debug.Log(inputList.Count);
            GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
            Image img = newTab.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("icons/icond");
            float hPos = lastPos2;
            float vPos = inputPanel.transform.localPosition.y - 35.0f;
            newTab.transform.localPosition = new Vector2(hPos, vPos);
            lastPos2 += 100.0f;
            numu_a[numu_n] = 10;
            numu_n += 1;
            inputList.Add(9);
        }
    }

    //public void input_image(int num_choice)
    //{
    //    if (inputList.Count < maxLength)
    //    {
    //        //Debug.Log(inputList.Count);
    //        GameObject newTab = Instantiate(tabCardPrefab, inputPanel.transform);
    //        Image img = newTab.GetComponent<Image>();
    //        img.sprite = Resources.Load<Sprite>("icons/icon"+num_choice);
    //        float hPos = lastPos2;
    //        float vPos = inputPanel.transform.localPosition.y - 35.0f;
    //        newTab.transform.localPosition = new Vector2(hPos, vPos);
    //        lastPos2 += 100.0f;
    //        numu_a[numu_n] = num_choice;
    //        numu_n += 1;
    //        inputList.Add(num_choice);
    //    }
    //}

    public void closePage()
    {
        UI1.SetActive(true);
        UI2.SetActive(false);
        UI4.SetActive(false);
        UI5.SetActive(false);
        UI6.SetActive(false);
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
        
         sw_9.Close();
        
    }

    private void OnApplicationQuit()
    {
        sw_9.Close();
    }
}
