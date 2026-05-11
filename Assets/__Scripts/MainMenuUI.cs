using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    private Button[] buttons;
    //public InputField playerNameInput;

    public Slider mainVolumeSlider;
    public Slider mouseSensitivitySlider;
    public TMP_Text mouseSensitivityValueText;
    public Toggle muteToggle;

    bool bSettingVolumeSliderManually = false;
    float lastVolumeButtonSoundTime = 0f;
    float volumeButtonSoundCooldown = 0.1f;

    public void Awake()
    {
        /*
        foreach (Button b in buttons)
        {
            b.onClick.AddListener(ButtonSound);
        }
        playerNameInput.onEndEdit.AddListener(SetPlayerName);
        mainVolumeSlider.onValueChanged.AddListener(delegate { VolumeChange(); });
        muteToggle.onValueChanged.AddListener(delegate { MuteToggle(muteToggle.isOn); });
        */
        buttons = GameObject.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            b.onClick.AddListener(ButtonSound);
        }
    }

    void Start()
    {
        // playerNameInput.text = PlayerPreferences.Instance.playerName;
        bSettingVolumeSliderManually = true;
        mainVolumeSlider.value = PlayerPreferences.Instance.mainVolume;
        mouseSensitivitySlider.value = PlayerPreferences.Instance.mouseSensitivity;
        muteToggle.isOn = PlayerPreferences.Instance.mainMuted;
        mouseSensitivityValueText.text = PlayerPreferences.Instance.mouseSensitivity.ToString("0.00");
    }

    public void ButtonSound()
    {
        AudioManager.PlaySoundAt(AudioManager.uiAudioSourcesSO.UIMenuClick, 1f);
    }

    public void CancelButtonSound()
    {
        AudioManager.PlaySoundAt(AudioManager.uiAudioSourcesSO.UIMenuCancel, 1f);
    }

    public void MuteToggle(bool isMuted)
    {
        isMuted = muteToggle.isOn;
        //Debug.Log("Mute called, bool = " + isMuted);
        if (isMuted)
        {
            ButtonSound();
            AudioManager.Mute();
            AudioListener.volume = 0f;
            PlayerPreferences.Instance.mainMuted = true;
            PlayerPreferences.Instance.SavePreferences();
        }
        else
        {
            AudioManager.UnMute();
            bSettingVolumeSliderManually = true;
            AudioListener.volume = PlayerPreferences.Instance.mainVolume;
            PlayerPreferences.Instance.mainMuted = false;
            PlayerPreferences.Instance.SavePreferences();
        }
    }

    public void VolumeChange()
    {
        PlayerPreferences.Instance.SetMainVolume(mainVolumeSlider.value);
        if (bSettingVolumeSliderManually)
        {
            bSettingVolumeSliderManually = false;
        }
        else
        {
            // cooldown
            if (Time.time - lastVolumeButtonSoundTime < volumeButtonSoundCooldown)
            {
                return;
            }
            ButtonSound();
            lastVolumeButtonSoundTime = Time.time;
        }
        
        //Debug.Log("Volume changed to " + mainVolumeSlider.value);
    }

    public void MouseSensitivityChange()
    {
        float mouseSensitivity = mouseSensitivitySlider.value;
        PlayerPreferences.Instance.mouseSensitivity = mouseSensitivity;
        mouseSensitivityValueText.text = mouseSensitivity.ToString("0.00");

        PlayerPreferences.Instance.SavePreferences();
    }

    public void PlayerBackButton()
    {
        CancelButtonSound();
    }

    public void SetPlayerName(string name)
    {
        //InputField playerNameInput = GameObject.Find("PlayerNameInput").GetComponent<InputField>();
        GameManager.Instance.playerState.playerName = name;
        PlayerPreferences.Instance.SetPlayerName(name);
        //Debug.Log("Player name set to: " + name);
    }

    public void StartGameButton()
    {
        GameManager.Instance.gameState.ResetGameState();
        GameManager.Instance.LoadScene(Scenes.Game);
    }

    public void OptionsButton()
    {
        //GameManager.Instance.LoadScene(Scenes.Options);
    }

    public void ContinueButton()
    {
        //GameManager.Instance.LoadScene(Scenes.Game);
    }

    public void QuitButton()
    {
        GameManager.Quit();
    }
}
