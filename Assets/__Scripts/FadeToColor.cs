using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Create a Panel UI element with anchors at full-stretch (default), and
// set the Image component to a solid color with alpha 0 for fade-in or alpha 255/1f for fade-out.
// This script can be attached to that panel or the panel component must be assigned in the Editor
public class FadeToColor : MonoBehaviour
{
    public Color fadeFromColor = Color.black;
    public Color fadeToColor = Color.black;
    [SerializeField] Image fadeImage;
    [SerializeField] float fadeDuration = 1f;

    void Awake()
    {
        if (fadeImage == null)
        {
            // Try to find an Image component on the same GameObject
            fadeImage = GetComponent<Image>();
            if (fadeImage == null)            {
                Debug.LogError("ScreenFade: No Image component assigned for fade effect. (drag panel into the Image field)");
                this.enabled = false;
                return;
            }
        }

        // Initialize the fade image to be fully transparent
        Color initialColor = fadeFromColor;
        initialColor.a = 0f;
        fadeImage.color = initialColor;
    }

    // this can be used for fade-in effects (fade from color to transparent)
    public void SetFadeFromColor(Color newColor)
    {
        fadeFromColor = newColor;
    }
    public void SetFadeToColor(Color newColor)
    {
        fadeToColor = newColor;
    }
    public void SetFadeDuration(float newDuration)
    {
        fadeDuration = newDuration;
    }

    public void FadeToBlack()
    {
        fadeFromColor = Color.black;
        fadeFromColor.a = 0f; // start fully transparent
        fadeToColor = Color.black;
        StartFadeOut(false);
    }

    // fade from color to transparent
    public void StartFadeIn(bool useCurrentColor = true)
    {
        Color source = fadeImage.color;
        Color transparent = fadeToColor;
        if (!useCurrentColor)
        {
            source = fadeFromColor;
        }
        transparent.a = 0f; // target fully transparent
        StartCoroutine(FadeRoutine(source, transparent));
    }

    // fade from transparent to color
    public void StartFadeOut(bool useCurrentColor = true)
    {
        Color source = fadeImage.color;
        Color targetColor = fadeToColor;
        if (!useCurrentColor)
        {
            source = fadeFromColor;
        }
        targetColor.a = 1f; // target fully opaque
        StartCoroutine(FadeRoutine(source, targetColor));
    }
    public void StartCrossFade()
    {
        StartCoroutine(FadeRoutine(fadeFromColor, fadeToColor));
    }

    public void SetColorInstantly(Color newColor)
    {
        fadeImage.color = newColor;
    }

    IEnumerator FadeRoutine(Color startColor, Color endColor)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, endColor, t / fadeDuration);
            yield return null;
        }

        fadeImage.color = endColor;
    }
}
