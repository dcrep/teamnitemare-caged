using System.Security.Cryptography;
using UnityEngine;

public class PlayerPreferences : MonoBehaviour
{
    const string PLAYER_NAME_KEY = "PlayerName";
    const string MAIN_VOLUME_KEY = "MainVolume";
    const string MAIN_MUTED_KEY = "MainMuted";
    const string MUSIC_VOLUME_KEY = "MusicVolume";
    const string SFX_VOLUME_KEY = "SfxVolume";
    const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity";

    public string playerName = "Player";
    public float mainVolume = 1.0f;
    public bool mainMuted = false;
    public float musicVolume = 1.0f;
    public float sfxVolume = 1.0f;
    public float mouseSensitivity = 1.0f;

    bool isDirty = false;

    public static PlayerPreferences Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPreferences();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadPreferences();
        SetState();
    }

    void OnApplicationFocus(bool focus)
    {
        // This is annoying and causes too many writes
        //if (!focus)
        //    SavePreferences();
    }

    void OnApplicationQuit()
    {
        if (isDirty)
        {
            SavePreferences();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start() {}

    // Update is called once per frame
    //void Update() {}

    public void SavePreferences()
    {        
        PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
        PlayerPrefs.SetFloat(MAIN_VOLUME_KEY, mainVolume);
        PlayerPrefs.SetInt(MAIN_MUTED_KEY, mainMuted ? 1 : 0);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_KEY, mouseSensitivity);
        PlayerPrefs.Save();
        Debug.Log("Preferences saved");
        isDirty = false;
    }

    public void LoadPreferences()
    {
        playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player");
        mainVolume = PlayerPrefs.GetFloat(MAIN_VOLUME_KEY, 1.0f);
        mainMuted = PlayerPrefs.GetInt(MAIN_MUTED_KEY, 0) == 1;
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1.0f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1.0f);
        mouseSensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, 1.0f);
        Debug.Log("Preferences loaded");
        isDirty = false;
    }

    public void SetMainVolume(float volume)
    {
        mainVolume = Mathf.Clamp01(volume); // Clamps value between 0 and 1
        AudioListener.volume = mainVolume;
        isDirty = true;
        //Debug.Log($"Main volume set to {mainVolume}");
    }

    private void SetState()
    {
        if (mainMuted)
        {
            AudioManager.Mute();
            AudioListener.volume = 0f;
        }
        else
        {
            AudioManager.UnMute();
            AudioListener.volume = mainVolume;
        }
    }

    public void RefreshAudioSettings()
    {
        SetState();
    }

    public void SetPlayerName(string name)
    {
        playerName = name;
        isDirty = true;
        Debug.Log($"Player name set to {playerName}");
    }
}
