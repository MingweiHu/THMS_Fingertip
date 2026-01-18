using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Call : MonoBehaviour
{
    public GameObject UI1;
    public GameObject UI2;
    public GameObject UI4;
    public GameObject UI5;
    public GameObject UI6;
    public GameObject music1;

    public AudioSource audioSource = new AudioSource();
    public AudioClip[] audios;

    public Button call;
    public Button up;
    public Button down1;
    public Button down2;

    public Text phonetime;
    public int currentMinute;
    public int currentSecond;
    public int clipMinute;
    public int clipSecond;
    public int currentMinute1;
    public int currentSecond1;
    public int clipMinute1;
    public int clipSecond1;
    // Start is called before the first frame update
    void Start()
    {
        call.onClick.AddListener(PhoneMenu);
        up.onClick.AddListener(Up);
        down1.onClick.AddListener(Down1);
        down2.onClick.AddListener(Down2);

    }

    // Update is called once per frame
    void Update()
    {
        if (this.GetComponent<AudioSource>().clip == audios[1])
        {
            showphonetime();
        }
    }

    public void PhoneMenu()
    {
        this.GetComponent<AudioSource>().clip = audios[0];
        this.GetComponent<AudioSource>().Play();
        UI1.SetActive(false);
        UI4.SetActive(true);
        UI6.SetActive(false);
    }

    public void Up()
    {
        this.GetComponent<AudioSource>().clip = audios[1];
        this.GetComponent<AudioSource>().Play();
        UI1.SetActive(false);
        UI2.SetActive(false);
        UI4.SetActive(false);
        UI5.SetActive(true);
        UI6.SetActive(false);
    }

    public void Down1()
    {
        this.GetComponent<AudioSource>().Stop();
        UI1.SetActive(true);
        UI2.SetActive(false);
        UI4.SetActive(false);
        UI5.SetActive(false);
        UI6.SetActive(false);
    }

    public void Down2()
    {
        this.GetComponent<AudioSource>().Stop();
        UI1.SetActive(true);
        UI2.SetActive(false);
        UI4.SetActive(false);
        UI5.SetActive(false);
        UI6.SetActive(false);
    }

    void showphonetime()
    {
        clipMinute1 = (int)this.GetComponent<AudioSource>().clip.length / 60; //时间计算
        clipSecond1 = (int)this.GetComponent<AudioSource>().clip.length - clipMinute1 * 60;

        currentMinute1 = (int)this.GetComponent<AudioSource>().time / 60;
        currentSecond1 = (int)(this.GetComponent<AudioSource>().time - currentMinute1 * 60);
        phonetime.text = string.Format("{0:D2} : {1:D2}", currentMinute1, currentSecond1);

        if (currentSecond1 == clipSecond1)  //当通话结束后自动返回主菜单界面
        {
            //Debug.Log("有没有");
            UI1.SetActive(false);
            UI2.SetActive(true);
            this.GetComponent<AudioSource>().Stop();
            music1.GetComponent<AudioSource>().Play();
            UI4.SetActive(false);
            UI5.SetActive(false);
            UI6.SetActive(false);
        }
    }
}
