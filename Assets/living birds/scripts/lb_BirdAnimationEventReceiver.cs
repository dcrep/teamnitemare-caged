using UnityEngine;

public class lb_BirdAnimationEventReceiver : MonoBehaviour
{
    static readonly int HopHash = Animator.StringToHash("hop");
    static readonly int FlyingHash = Animator.StringToHash("flying");
    static readonly int LandingHash = Animator.StringToHash("landing");

    Animator targetAnimator;
    AudioSource targetAudioSource;

    bool playSongAudio;
    AudioClip song1;
    AudioClip song2;
    float songVolume = 1.0f;
    bool playSongAtWorldPosition = true;
    Vector3 songPositionOffset = Vector3.zero;
    const float PositionalMinDistance = 1.0f;
    const float PositionalMaxDistance = 200.0f;

    public void Configure(
        Animator animator,
        AudioSource audioSource,
        bool canPlaySongAudio,
        AudioClip configuredSong1,
        AudioClip configuredSong2,
        float configuredSongVolume,
        bool useWorldPosition,
        Vector3 configuredSongPositionOffset)
    {
        targetAnimator = animator;
        targetAudioSource = audioSource;
        playSongAudio = canPlaySongAudio;
        song1 = configuredSong1;
        song2 = configuredSong2;
        songVolume = Mathf.Max(0.0f, configuredSongVolume);
        playSongAtWorldPosition = useWorldPosition;
        songPositionOffset = configuredSongPositionOffset;
    }

    // Called by animation events on hop clips.
    public void ResetHopInt()
    {
        if (targetAnimator == null)
        {
            targetAnimator = GetComponent<Animator>();
        }

        if (targetAnimator != null)
        {
            targetAnimator.SetInteger(HopHash, 0);
        }
    }

    // Called by animation events on flying/landing clips.
    public void ResetFlyingLandingVariables()
    {
        if (targetAnimator == null)
        {
            targetAnimator = GetComponent<Animator>();
        }

        if (targetAnimator != null)
        {
            targetAnimator.SetBool(FlyingHash, false);
            targetAnimator.SetBool(LandingHash, false);
        }
    }

    // Called by animation events on sing clips.
    public void PlaySong()
    {
        if (!playSongAudio)
        {
            return;
        }

        AudioClip clip = GetRandomSong();
        if (clip == null)
        {
            return;
        }

        if (playSongAtWorldPosition)
        {
            PlayPositionalClip(clip, transform.position + songPositionOffset, songVolume);
            return;
        }

        if (targetAudioSource == null)
        {
            targetAudioSource = GetComponent<AudioSource>();
            if (targetAudioSource == null)
            {
                targetAudioSource = GetComponentInChildren<AudioSource>();
            }
        }

        if (targetAudioSource == null)
        {
            return;
        }

        targetAudioSource.PlayOneShot(clip, Mathf.Max(0.0f, songVolume));
    }

    void PlayPositionalClip(AudioClip clip, Vector3 worldPosition, float volume)
    {
        if (clip == null)
        {
            return;
        }

        GameObject tempAudioObject = new GameObject("lb_eventSongAudio");
        tempAudioObject.transform.position = worldPosition;

        AudioSource source = tempAudioObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1.0f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = PositionalMinDistance;
        source.maxDistance = PositionalMaxDistance;
        source.clip = clip;
        source.volume = Mathf.Max(0.0f, volume);
        source.Play();

        Destroy(tempAudioObject, clip.length + 0.1f);
    }

    AudioClip GetRandomSong()
    {
        if (song1 == null && song2 == null)
        {
            return null;
        }

        if (song1 == null)
        {
            return song2;
        }

        if (song2 == null)
        {
            return song1;
        }

        return Random.value < 0.5f ? song1 : song2;
    }
}
