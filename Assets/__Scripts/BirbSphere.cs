using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BirbSphere : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("References")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Light pointLight;

    [Header("Emission")]
    [SerializeField] private Color emissionColor = Color.white;
    [SerializeField] private float minEmissionIntensity = 0f;
    [SerializeField] private float maxEmissionIntensity = 5f;
    [SerializeField] private float currentEmissionIntensity = 1f;
    [SerializeField] private float pointLightIntensityMultiplier = 1f;

    [Header("Events")]
    [SerializeField] private UnityEvent onDissolveCompleted;

    private Material runtimeMaterial;
    private bool isDuringDissolve = false;
    private Coroutine pulseRoutine;
    private Coroutine dissolveRoutine;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogWarning($"{nameof(BirbSphere)} on {name} has no Renderer assigned.");
            return;
        }

        runtimeMaterial = targetRenderer.material;

        if (pointLight == null)
            pointLight = GetComponentInChildren<Light>();

        if (runtimeMaterial.HasProperty(EmissionColorId))
            runtimeMaterial.EnableKeyword("_EMISSION");

        // Debug.Log($"[BirbSphere] Awake on {name}: Material={runtimeMaterial.name}, Shader={runtimeMaterial.shader.name}");
        //LogMaterialProperties();

        SetEmissionIntensity(currentEmissionIntensity);
    }

    public void SetEmissionIntensity(float intensity)
    {
        currentEmissionIntensity = Mathf.Clamp(intensity, minEmissionIntensity, maxEmissionIntensity);
        //Debug.Log($"[BirbSphere] SetEmissionIntensity called on {name}: intensity={currentEmissionIntensity:F2}");

        if (runtimeMaterial == null)
        {
            Debug.LogWarning($"[BirbSphere] on {name}: runtimeMaterial is null");
            return;
        }

        if (!runtimeMaterial.HasProperty(EmissionColorId))
        {
            Debug.LogWarning($"{nameof(BirbSphere)} on {name}: material '{runtimeMaterial.name}' does not have _EmissionColor property.");
            return;
        }

        Color finalEmission = emissionColor * Mathf.LinearToGammaSpace(currentEmissionIntensity);
        //Debug.Log($"[BirbSphere] Setting emission color to {finalEmission}");
        runtimeMaterial.SetColor(EmissionColorId, finalEmission);

        if (!isDuringDissolve && pointLight != null)
        {
            float newIntensity = currentEmissionIntensity * pointLightIntensityMultiplier;
            //Debug.Log($"[BirbSphere] Setting point light intensity to {newIntensity:F2} (emission={currentEmissionIntensity:F2}, multiplier={pointLightIntensityMultiplier:F2})");
            pointLight.intensity = newIntensity;
        }
        else if (pointLight == null)
        {
            Debug.LogWarning($"[BirbSphere] on {name}: pointLight is null, not updating light intensity");
        }
    }

    public void IncreaseEmission(float amount = 0.5f)
    {
        SetEmissionIntensity(currentEmissionIntensity + Mathf.Abs(amount));
    }

    public void DecreaseEmission(float amount = 0.5f)
    {
        SetEmissionIntensity(currentEmissionIntensity - Mathf.Abs(amount));
    }

    public void SetFullBrightness()
    {
        SetEmissionIntensity(maxEmissionIntensity);
    }

    public void SetZeroBrightness()
    {
        SetEmissionIntensity(minEmissionIntensity);
    }

    public void PulseEmission(float pulseDuration = 1f)
    {
        PulseEmission(minEmissionIntensity, maxEmissionIntensity, pulseDuration);
    }

    public void PulseEmission(float lowIntensity, float highIntensity, float pulseDuration = 1f)
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(PulseEmissionRoutine(lowIntensity, highIntensity, pulseDuration));
    }

    public void DissolveAndDestroy(float duration = 1.5f)
    {
        if (dissolveRoutine != null)
            return;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        isDuringDissolve = true;
        dissolveRoutine = StartCoroutine(DissolveAndDestroyRoutine(duration));
    }

    protected virtual void OnDissolveComplete()
    {
        // Intentionally empty so this method can be filled in later.
    }

    private void LogMaterialProperties()
    {
        if (runtimeMaterial == null)
            return;

        Debug.Log($"[BirbSphere] Material properties on {name}:");
        Debug.Log($"  _EmissionColor exists: {runtimeMaterial.HasProperty(EmissionColorId)}");
        if (runtimeMaterial.HasProperty(EmissionColorId))
            Debug.Log($"  _EmissionColor value: {runtimeMaterial.GetColor(EmissionColorId)}");

        Debug.Log($"  Material.color (base color): {runtimeMaterial.color}");
        Debug.Log($"  Shader keywords enabled: {string.Join(", ", runtimeMaterial.shaderKeywords)}");
    }

    private IEnumerator PulseEmissionRoutine(float lowIntensity, float highIntensity, float pulseDuration)
    {
        float safeDuration = Mathf.Max(0.01f, pulseDuration);
        float low = Mathf.Clamp(lowIntensity, minEmissionIntensity, maxEmissionIntensity);
        float high = Mathf.Clamp(highIntensity, minEmissionIntensity, maxEmissionIntensity);

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            float pulseT = t <= 0.5f
                ? Mathf.Lerp(low, high, t * 2f)
                : Mathf.Lerp(high, low, (t - 0.5f) * 2f);

            SetEmissionIntensity(pulseT);
            yield return null;
        }

        SetEmissionIntensity(low);
        pulseRoutine = null;
    }

    private IEnumerator DissolveAndDestroyRoutine(float duration)
    {
        if (runtimeMaterial == null)
        {
            OnDissolveComplete();
            onDissolveCompleted?.Invoke();
            if (targetRenderer != null)
                targetRenderer.enabled = false;
            yield break;
        }

        float safeDuration = Mathf.Max(0.01f, duration);
        EnsureMaterialSupportsAlphaFade();
        Color startBaseColor = GetBaseColor(runtimeMaterial);
        float startAlpha = GetMaterialAlpha(runtimeMaterial);
        float startEmissionIntensity = currentEmissionIntensity;

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            SetMaterialAlpha(runtimeMaterial, startBaseColor, Mathf.Lerp(startAlpha, 0f, t));

            SetEmissionIntensity(Mathf.Lerp(startEmissionIntensity, 0f, t));

            yield return null;
        }

        SetMaterialAlpha(runtimeMaterial, startBaseColor, 0f);
        SetEmissionIntensity(0f);

        if (targetRenderer != null)
            targetRenderer.enabled = false;
        currentEmissionIntensity = 0f;

        OnDissolveComplete();
        onDissolveCompleted?.Invoke();
    }

    private void EnsureMaterialSupportsAlphaFade()
    {
        if (runtimeMaterial == null)
            return;

        if (runtimeMaterial.HasProperty("_Surface"))
        {
            runtimeMaterial.SetFloat("_Surface", 1f);
            if (runtimeMaterial.HasProperty("_Blend"))
                runtimeMaterial.SetFloat("_Blend", 0f);
            if (runtimeMaterial.HasProperty("_ZWrite"))
                runtimeMaterial.SetInt("_ZWrite", 0);
            runtimeMaterial.DisableKeyword("_ALPHATEST_ON");
            runtimeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            runtimeMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return;
        }

        if (runtimeMaterial.HasProperty("_Mode"))
        {
            runtimeMaterial.SetFloat("_Mode", 2f);
            if (runtimeMaterial.HasProperty("_SrcBlend"))
                runtimeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (runtimeMaterial.HasProperty("_DstBlend"))
                runtimeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (runtimeMaterial.HasProperty("_ZWrite"))
                runtimeMaterial.SetInt("_ZWrite", 0);
            runtimeMaterial.DisableKeyword("_ALPHATEST_ON");
            runtimeMaterial.EnableKeyword("_ALPHABLEND_ON");
            runtimeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            runtimeMaterial.renderQueue = 3000;
        }
    }

    private static Color GetBaseColor(Material material)
    {
        if (material.HasProperty(BaseColorId))
            return material.GetColor(BaseColorId);

        if (material.HasProperty(ColorId))
            return material.GetColor(ColorId);

        return material.color;
    }

    private static void SetBaseColor(Material material, Color color)
    {
        if (material.HasProperty(BaseColorId))
        {
            material.SetColor(BaseColorId, color);
            return;
        }

        if (material.HasProperty(ColorId))
        {
            material.SetColor(ColorId, color);
            return;
        }

        material.color = color;
    }

    private static float GetMaterialAlpha(Material material)
    {
        return GetBaseColor(material).a;
    }

    private static void SetMaterialAlpha(Material material, Color baseColor, float alpha)
    {
        Color fadedColor = baseColor;
        fadedColor.a = alpha;

        if (material.HasProperty(BaseColorId))
            material.SetColor(BaseColorId, fadedColor);

        if (material.HasProperty(ColorId))
            material.SetColor(ColorId, fadedColor);

        material.color = fadedColor;
    }
}
