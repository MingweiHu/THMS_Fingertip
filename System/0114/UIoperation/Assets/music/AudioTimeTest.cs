using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AudioTimeTest : MonoBehaviour
{
    public AudioSource audioSource;
    private bool isPlay = true;
    private int audioClipIndex = 0;

    public AudioClip[] audioClips;
    public Button NextButton;
    public Button PreviousButton;
    public Button PlayOrPauseButton;

    public Slider audioTimeSlider;
    public Slider soundLevelSlider;

    public Text audioTimeText;
    public Text audioName;

    public GameObject btnObj;
    public Sprite Play;
    public Sprite Pause;
    Button btn;//声明按钮

    private int currentMinute;
    private int currentSecond;
    private int clipMinute;
    private int clipSecond;

    // Use this for initialization
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        //audioTimeSlider.onValueChanged.AddListener(SetAudioTimeValueChange());

        //audioTimeSlider.onValueChanged.AddListener(delegate { SetAudioTimeValueChange(); });
        soundLevelSlider.minValue = 0;
        soundLevelSlider.maxValue = 1;
        PreviousButton.onClick.AddListener(PreviousAudio);
        NextButton.onClick.AddListener(NextAudio);
        //PlayOrPauseButton.onClick.AddListener(PlayOrPauseAudio);
        audioSource.clip = audioClips[audioClipIndex];
        audioTimeSlider.minValue = 0;
        audioTimeSlider.maxValue = audioClips[audioClipIndex].length;

        audioSource.Play();

        btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(delegate ()
        {
            if (isPlay)
            {
                isPlay = false;
                audioSource.Pause();
                btn.GetComponent<Image>().sprite = Play;
            }
            else
            {
                isPlay = true;
                audioSource.Play();
                btn.GetComponent<Image>().sprite = Pause;
            }
        });
    }

    private void NextAudio()
    {
        audioClipIndex++;

        if (audioClipIndex > audioClips.Length - 1)
        {
            audioClipIndex = 0;
        }
        if (audioSource.isPlaying == true)
        {
            audioSource.Pause();
        }
        audioSource.clip = audioClips[audioClipIndex];
        audioSource.Play();
    }

    private void PreviousAudio()
    {
        audioClipIndex--;

        if (audioClipIndex < 0)
        {
            audioClipIndex = audioClips.Length - 1;
        }
        if (audioSource.isPlaying == true)
        {
            audioSource.Pause();
        }
        audioSource.clip = audioClips[audioClipIndex];
        audioSource.Play();
    }
    
    private void SetAudioTimeValueChange()
    {

        audioTimeSlider.value = audioSource.time;
        if (audioSource.isPlaying != true)
        { audioSource.Play(); }
    }

    private void ShowAudioTime()
    {
        clipMinute = (int)audioClips[audioClipIndex].length / 60;
        clipSecond = (int)(audioClips[audioClipIndex].length - clipMinute * 60);

        currentMinute = (int)audioSource.time / 60;
        currentSecond = (int)(audioSource.time - currentMinute * 60);

        audioTimeText.text = string.Format("{0:D2}:{1:D2}/{2:D2}:{3:D2}", currentMinute, currentSecond, clipMinute, clipSecond);
        audioName.text = audioClips[audioClipIndex].name;
    }
    // Update is called once per frame
    void Update()
    {
        audioSource.volume = soundLevelSlider.value;
        ShowAudioTime();
        //Debug.Log(audioClips[audioClipIndex].length);
        SetAudioTimeValueChange();
    }

    
}
