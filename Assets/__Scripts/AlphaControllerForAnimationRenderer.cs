using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class AlphaControllerForAnimationRenderer : MonoBehaviour
{
    [Range(0f, 1f)]
    public float alpha = 1f;

    [Range(0f, 1f)]
    public float emissionIntensity = 1f;

    private Renderer rend;
    private Material mat;
    //MaterialPropertyBlock block;

    [Range(0f, 1f)]
    public float startAlpha = 1f;          // Initial alpha
    public float fadeDuration = 2f;        // Time to fade
    public Color targetEmission = Color.black; // Target emission color when fading out
    public float targetEmissionIntensity = 1f; // Intensity multiplier for target emission

    private Color originalBaseColor;
    private Color originalEmissionColor;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mat = rend.sharedMaterial;

        // Store original colors
        originalBaseColor = mat.color;
        originalEmissionColor = mat.GetColor("_EmissionColor");

        // Ensure transparency works
        SetMaterialToFadeMode(mat);

        // Ensure emission keyword is enabled
        if (!mat.IsKeywordEnabled("_EMISSION"))
            mat.EnableKeyword("_EMISSION");
    }
        

    void OnEnable()
    {
        Apply();
    }

    // editor-only: update in real-time when values change
    void OnValidate()
    {
        if (!rend)
        {
            rend = GetComponent<Renderer>();
            mat = rend.sharedMaterial;
        } 

        Apply();
    }

    public void Apply()
    {
        if (!rend) return;

        //Debug.Log("SharedMaterial color: " + rend.sharedMaterial.color);
        //Debug.Log("Material color: " + rend.material.color);
        //Debug.Log("Emission color: " + rend.material.GetColor("_EmissionColor"));

        //rend.material.color = new Color(rend.material.color.r, rend.material.color.g, rend.material.color.b, alpha);
        //!! This line causes Editor & compilation issues when using rend.material,
        // however the material becoes darker. But also the name of the material keeps changing with
        // each change, which is a sign of instancing. So we have to use sharedMaterial,
        // but that causes the alpha to not update in the editor. Issue is linked to OnValidate() (editor-only)
        var mat = rend.sharedMaterial;
        Color c = mat.color;
        c.a = alpha;
        mat.color = c;
        Color e = mat.GetColor("_EmissionColor");

        SetEmissionIntensity(emissionIntensity);
    }


    public void SetEmissionIntensity(float intensity)
    {
        Color currentEmission = mat.GetColor("_EmissionColor");
        SetEmission(currentEmission, intensity);
    }
    // Set emission to a specific color and intensity
    public void SetEmission(Color color, float intensity = 1f)
    {
        Color finalColor = color * Mathf.LinearToGammaSpace(intensity);
        mat.SetColor("_EmissionColor", finalColor);
        mat.EnableKeyword("_EMISSION");
    }

    // Restore original emission
    public void RestoreEmission()
    {
        mat.SetColor("_EmissionColor", originalEmissionColor);
        mat.EnableKeyword("_EMISSION");
    }

    public void SetToAlpha(float newAlpha)
    {
        alpha = newAlpha;
        Apply();
    }
    public void SetFullyOpaque()
    {
        SetToAlpha(1f);
    }
    public void SetFullyTransparent()
    {
        SetToAlpha(0f);
    }

    public void FadeOutToTransparent(float duration = 0.5f)
    {
        FadeOut(duration, Color.white, 0f);
    }
    public void FadeOut(float duration, Color targetEmissionColor, float targetEmissionIntensity)
    {
        fadeDuration = duration;
        this.targetEmission = targetEmissionColor;
        this.targetEmissionIntensity = targetEmissionIntensity;
        FadeOut();
    }

    public void FadeOut()
    {
        // stop any ongoing fade (this script only) to prevent conflicts
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(originalBaseColor.a, 0f, originalEmissionColor, targetEmission * Mathf.LinearToGammaSpace(targetEmissionIntensity)));
    }

    public void FadeIn()
    {
        // stop any ongoing fade (this script only) to prevent conflicts
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(mat.color.a, startAlpha, mat.GetColor("_EmissionColor"), originalEmissionColor));
    }

    private IEnumerator FadeRoutine(float alphaFrom, float alphaTo, Color emissionFrom, Color emissionTo)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // Fade alpha
            Color newBase = originalBaseColor;
            newBase.a = Mathf.Lerp(alphaFrom, alphaTo, t);
            mat.color = newBase;

            // Fade emission
            Color newEmission = Color.Lerp(emissionFrom, emissionTo, t);
            mat.SetColor("_EmissionColor", newEmission);

            yield return null;
        }

        // If fully faded out, disable emission for performance
        if (alphaTo <= 0f && emissionTo == Color.black)
            mat.DisableKeyword("_EMISSION");
        else
            mat.EnableKeyword("_EMISSION");
    }

    // Helper: Set Standard Shader to Fade mode
    private void SetMaterialToFadeMode(Material m)
    {
        m.SetFloat("_Mode", 2); // Fade
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
    }

}

