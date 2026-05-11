using UnityEngine;

public class ChirpAttract : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] AudioClip attractionSound;

    AudioSource cachedAudioSource;

    void Awake()
    {
        EnsureAudioSource();
    }

    public void PlaySound()
    {
        if (attractionSound != null)
        {
            EnsureAudioSource();

            if (cachedAudioSource == null)
            {
                Debug.LogWarning($"{nameof(ChirpAttract)} on {name} could not get an AudioSource, so no sound was played.");
                return;
            }

            cachedAudioSource.transform.position = transform.position;
            cachedAudioSource.PlayOneShot(attractionSound);
        }
    }

    void EnsureAudioSource()
    {
        if (cachedAudioSource == null)
        {
            cachedAudioSource = GetComponent<AudioSource>();
        }

        if (cachedAudioSource == null)
        {
            cachedAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (cachedAudioSource != null)
        {
            cachedAudioSource.playOnAwake = false;
            cachedAudioSource.loop = false;
            cachedAudioSource.spatialBlend = 1f;    // 3D sound
            cachedAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }
}
