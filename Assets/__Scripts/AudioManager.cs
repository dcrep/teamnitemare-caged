using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.PlayerLoop;

//! TODO: Audio Mixer? Look into this. Audio Listener - added 1 function, is there more?

// See "Unity AUDIO Volume Settings Menu Tutorial" @ https://youtu.be/G-JUp8AMEx0
// Mixer Group volume needs to be exposed, then renamed in Exposed Parameters
// before changing
// I have AudioMixer named "MainMix" in Resources/Audio
// It has a Master group, with Music, SFX, and Voice subgroups. Defaults set to 0. No FX

public class AudioManager : MonoBehaviour
{
    //[SerializeField] private AudioClip[] soundClips;

    private const string MasterVolumeParameter = "Master";
    private const string MusicVolumeParameter = "Music";
    private const string SfxVolumeParameter = "SFX";
    private const string VoiceVolumeParameter = "Voice";

    private static bool audioMuted = false;
    private static AudioManager Instance;
    public static AudioSource audioSourceMusic, audioSourceSFX, audioSourceVoice;

    public static AudioMixer audioMixer;
    public static AudioMixerGroup masterMixerGroup, musicMixerGroup, sfxMixerGroup, voiceMixerGroup;

    private static float oneShotVolume = 1f;

    public static UIAudioSourcesSO uiAudioSourcesSO;
    public static AudioClip musicPlaceholder;
    void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("AudioManager->Awake()");
            Instance = this;
            audioSourceMusic = gameObject.AddComponent<AudioSource>();
            audioSourceMusic.playOnAwake = false;

            audioSourceSFX = gameObject.AddComponent<AudioSource>();
            audioSourceSFX.playOnAwake = false;

            audioSourceVoice = gameObject.AddComponent<AudioSource>();
            audioSourceVoice.playOnAwake = false;

            audioMixer = Resources.Load<AudioMixer>("Audio/MainMix");
            if (audioMixer == null)
            {
                Debug.LogError("AudioManager: Failed to load MainMix.");
            }
            else
            {
                masterMixerGroup = audioMixer.FindMatchingGroups("Master")[0];
                musicMixerGroup = audioMixer.FindMatchingGroups("Music")[0];
                sfxMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];
                voiceMixerGroup = audioMixer.FindMatchingGroups("Voice")[0];
                audioSourceMusic.outputAudioMixerGroup = musicMixerGroup;
                audioSourceSFX.outputAudioMixerGroup = sfxMixerGroup;
                audioSourceVoice.outputAudioMixerGroup = voiceMixerGroup;
            }

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void Init(UIAudioSourcesSO uiAudioSourcesSO)
    {
        AudioManager.uiAudioSourcesSO = uiAudioSourcesSO;
    }

    public static void Mute()
    {
        audioMuted = true;
        audioSourceMusic.mute = true;
        //Debug.Log("Audio muted");
    }
    public static void UnMute()
    {
        audioMuted = false;
        audioSourceMusic.mute = false;
    }
    public static void  MuteToggle()
    {
        if (audioMuted)
        {
            UnMute();
        }
        else
        {
            Mute();
        }
    }

    public static void SetMainVolumeAL(float volume)
    {
        AudioListener.volume = volume;
    }

    public static void SetMainVolumeMixer(float volume)
    {
        // volume can't be set to 0, so 0.00001 is used as the minimum instead
        // 0 maps to -80, 1 maps to 0, >1 (max 10) maps to +0.x to +20
        volume = Mathf.Clamp(volume, 0.00001f, 10f);
        // Convert linear 0-1 volume to logarithmic -80 to 0 dB for the mixer
        audioMixer.SetFloat(MasterVolumeParameter, Mathf.Log10(volume) * 20);
    }

    public static void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.00001f, 10f);
        audioMixer.SetFloat(MusicVolumeParameter, Mathf.Log10(volume) * 20);
    }
    public static void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.00001f, 10f);
        audioMixer.SetFloat(SfxVolumeParameter, Mathf.Log10(volume) * 20);
    }
    public static void SetVoiceVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.00001f, 10f);
        audioMixer.SetFloat(VoiceVolumeParameter, Mathf.Log10(volume) * 20);
    }

    // Using audioSourceMusic to play 1 (and only 1) sound at a time
    // Useful for music or other long sounds
    // This will stop any currently playing sound before playing the new one
    public static void Play(AudioClip clip, float volume = 1, bool use3D = false)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play a null clip.");
            return;
        }
        audioSourceMusic.clip = clip;
        audioSourceMusic.volume = volume;
        // use 3d
        if (use3D)
        {
            audioSourceMusic.spatialBlend = 1f;
        }        else
        {
            audioSourceMusic.spatialBlend = 0f;
        }
        audioSourceMusic.Play();
    }
    public static void Stop() => audioSourceMusic.Stop();
    public static void Pause() => audioSourceMusic.Pause();
    public static void UnPause() => audioSourceMusic.UnPause();
    public static void PauseToggle()
    {
        if (IsPlaying())
        {
            Pause();
        }
        else
        {
            UnPause();
        }
    }
    public static void SetVolume(float volume) => audioSourceMusic.volume = volume;
    public static bool IsPlaying() => audioSourceMusic.isPlaying;
    public static bool IsPaused() => audioSourceMusic.isPlaying == false && audioSourceMusic.time > 0;
    public static bool IsLooping() => audioSourceMusic.loop;
    public static void Loop(bool loop = true) => audioSourceMusic.loop = loop;
    public static float GetClipLength() => audioSourceMusic.clip.length;
    public static float GetClipTime() => audioSourceMusic.time;
    public static float GetPitch() => audioSourceMusic.pitch;
    public static void SetPitch(float pitch) => audioSourceMusic.pitch = pitch;

    // Using audioSourceSFX to play additional sounds without interrupting the current sound

    public static void PlayVolumeForOneShot(float volume) => oneShotVolume = volume;
    public static float GetVolumeForOneShot() => oneShotVolume;

    public static void PlayOneShot(AudioClip clip)
    {
        PlayOneShot(clip, oneShotVolume);
    }
    public static void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play a null clip.");
            return;
        }
        if (audioMuted)
        {
            return;
        }
        audioSourceSFX.PlayOneShot(clip, volume);
    }
    public static void PlayOneShotFromArray(AudioClip[] clips)
    {
        PlayOneShotFromArray(clips, oneShotVolume);
    }
    public static void PlayOneShotFromArray(AudioClip[] clips, float volume = 1)
    {
        if (audioMuted)
        {
            return;
        }
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("AudioManager: Attempted to play a null or empty clip array.");
            return;
        }

        int randomIndex = Random.Range(0, clips.Length);
        PlayOneShot(clips[randomIndex], volume);
    }

    // Play sound at a specific position in the world, on a newly created (and disposed-of) AudioSource
    // !!Don't use if want volume or mixer-control!!
    public static void PlaySoundAt2DAS(AudioClip clip, float volume = 1, Vector2 position = default(Vector2))
    {
        Vector3 position3D = new Vector3(position.x, position.y, Camera.main.transform.position.z);
        //Debug.Log("Playing sound " + clip.name + " at position " + position3D + " with volume " + volume);
        
        PlaySoundAt3DAS(clip, volume, position3D);
    }
    public static void PlaySoundAt3DAS(AudioClip clip, float volume = 1, Vector3 position = default(Vector3))
    {
        if (audioMuted)
        {
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play a null clip.");
            return;
        }
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    public static void PlaySoundAt2DPoint(AudioClip clip, float volume = 1, Vector2 position = default(Vector2))
    {
        if (audioMuted)
        {
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play a null clip.");
            return;
        }
        Vector3 position3D = new Vector3(position.x, position.y, Camera.main.transform.position.z);
        //Debug.Log("Playing sound " + clip.name + " at position " + position3D + " with volume " + volume);
        
        // Play the sound at the specified position
        PlaySoundAt3DPoint(clip, volume, position3D);
    }

    public static void PlaySoundAt3DPoint(AudioClip clip, float volume = 1, Vector3 position = default(Vector3))
    {
        // Create a temporary GameObject at the position
        GameObject tempGO = new GameObject("LocalizedAudioClip");
        tempGO.transform.position = position;

        // Add AudioSource and configure it
        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.volume = Mathf.Clamp01(volume);
        aSource.outputAudioMixerGroup = sfxMixerGroup; // Route to mixer group
        aSource.rolloffMode = AudioRolloffMode.Logarithmic;
        // aSource.minDistance = Mathf.Max(0.01f, positionalAudioMinDistance);
        // aSource.maxDistance = Mathf.Max(aSource.minDistance + 0.01f, positionalAudioMaxDistance);
        aSource.spatialBlend = 1f; // 3D sound
        aSource.Play();

        // Destroy after clip finishes
        Destroy(tempGO, clip.length / aSource.pitch + 0.1f);
    }

    // Helper function for PlaySoundAt to play a random sound from an array of clips
    public static void PlaySoundAt2DPointFromArray(AudioClip[] clips, float volume = 1, Vector2 position = default(Vector2))
    {
        Vector3 position3D = new Vector3(position.x, position.y, Camera.main.transform.position.z);
        PlaySoundAt3DPointFromArray(clips, volume, position3D);
    }
    public static void PlaySoundAt3DPointFromArray(AudioClip[] clips, float volume = 1, Vector3 position = default(Vector3))
    {
        if (audioMuted)
        {
            return;
        }
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("AudioManager: Attempted to play a null or empty clip array.");
            return;
        }

        int randomIndex = Random.Range(0, clips.Length);
        PlaySoundAt3DPoint(clips[randomIndex], volume, position);
    }


    public static void PlayDialogueButtonPressAudioClip()
    {
        PlayOneShot(uiAudioSourcesSO.UIMenuClick);
    }
    public static void PlayDialogueButtonCancelAudioClip()
    {
        PlayOneShot(uiAudioSourcesSO.UIMenuCancel);
    }
}