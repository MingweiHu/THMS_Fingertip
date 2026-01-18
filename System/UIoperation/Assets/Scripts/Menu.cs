using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public AudioSource audioSource = new AudioSource();
    public AudioClip[] audios;

    public GameObject UI1;
    public GameObject UI2;
    public GameObject UI4;
    public GameObject UI5;
    public GameObject UI6;

    public Button musicmenubutton;
    public Button musicback;
    public Button previousbutton;
    public Button nextbutton;
    public Button playorpausebutton;
    public Button VolumeUp;
    public Button VolumeDown;
    public Slider soundLevelSlider;
    public Slider audioTimeSlider;

    public int i = 1;
    public int music = 0;
    public int currentMinute=0;
    public int currentSecond=0;
    public int clipMinute=0;
    public int clipSecond=0;
    public Text audioTimeText;
    public Text audioName;
    public bool isplay = true;
    public Sprite play;
    public Sprite pause;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = this.GetComponent<AudioSource>();
        soundLevelSlider.minValue = 0;
        soundLevelSlider.maxValue = 10;
        soundLevelSlider.value = 5;
        audioTimeSlider.minValue = 0;
        audioSource.Play();
        previousbutton.onClick.AddListener(Previous_song);
        nextbutton.onClick.AddListener(Next_song);
        playorpausebutton.onClick.AddListener(PlayorPause_song);
        musicmenubutton.onClick.AddListener(MusicMenu);
        musicback.onClick.AddListener(BacktoMainMenu);
        VolumeUp.onClick.AddListener(IncreaseVolume);
        VolumeDown.onClick.AddListener(ReduceVolume);
    }

    // Update is called once per frame
    void Update()
    {
        this.GetComponent<AudioSource>().volume = (soundLevelSlider.value) / 10;
        
        //Debug.Log(soundLevelSlider.value);
        if (this.GetComponent<AudioSource>().isPlaying)
        {
            ShowAudioTime();
        }
    }

    public void MusicMenu()
    {
        this.GetComponent<AudioSource>().clip = audios[music];
        this.GetComponent<AudioSource>().Play();
        UI1.SetActive(false);
        UI2.SetActive(true);
    }

    public void BacktoMainMenu()
    {
        this.GetComponent<AudioSource>().Pause();
        UI1.SetActive(true);
        UI2.SetActive(false);
    }

    public void PlayorPause_song()
    {
        if (isplay == true)
        {
            isplay = false;
            audioSource.Pause();
            playorpausebutton.GetComponent<Image>().sprite = play;
        }
        else
        {
            isplay = true;
            audioSource.Play();
            playorpausebutton.GetComponent<Image>().sprite = pause;
        }
    }

    public void Next_song()
    {
        music++;
        isplay = true;
        playorpausebutton.GetComponent<Image>().sprite = pause;

        if (music > audios.Length - 1)
        {
            music = 0;
        }
        this.GetComponent<AudioSource>().clip = audios[music];
        this.GetComponent<AudioSource>().Play();
    }

    public void Previous_song()
    {
        music--;
        isplay = true;
        playorpausebutton.GetComponent<Image>().sprite = pause;

        if (music < 0)
        {
            music = audios.Length - 1;
        }
        this.GetComponent<AudioSource>().clip = audios[music];
        this.GetComponent<AudioSource>().Play();
    }

    public void IncreaseVolume()
    {
        soundLevelSlider.value += 0.1f;
    }

    public void ReduceVolume()
    {
        soundLevelSlider.value -= 0.1f;
    }

    public void ShowAudioTime()
    //修改界面上的歌曲显示信息
    {
        clipMinute = (int)this.GetComponent<AudioSource>().clip.length / 60; //时间计算
        clipSecond = (int)this.GetComponent<AudioSource>().clip.length - clipMinute * 60;
        currentMinute = (int)this.GetComponent<AudioSource>().time / 60;
        currentSecond = (int)(this.GetComponent<AudioSource>().time - currentMinute * 60);
        audioTimeText.text = string.Format("{0:D2}:{1:D2} / {2:D2}:{3:D2}", currentMinute, currentSecond, clipMinute, clipSecond); //时间显示
        audioName.text = this.GetComponent<AudioSource>().clip.name;
        audioTimeSlider.maxValue = this.GetComponent<AudioSource>().clip.length; //时间slider长度最大值
        audioTimeSlider.value = this.GetComponent<AudioSource>().time;  //时间slider显示值
        //soundLevelSlider.value = this.GetComponent<AudioSource>().volume; //歌曲slider音量值
    }

    
}
