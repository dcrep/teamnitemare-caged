using UnityEngine;

[DisallowMultipleComponent]
public class GrapplePointGlow : MonoBehaviour
{
    [SerializeField] private Light glowLight;
    [SerializeField] private bool autoFindLightInChildren = true;
    [SerializeField] private float highlightedIntensity = 2f;
    [SerializeField] private Color highlightedColor = Color.white;

    private bool baseLightEnabled;
    private float baseLightIntensity;
    private Color baseLightColor;
    private bool hasCachedBaseState;

    void Awake()
    {
        CacheLightState();
    }

    public void SetHighlighted(bool highlighted)
    {
        CacheLightState();

        if (glowLight == null)
        {
            return;
        }

        if (highlighted)
        {
            glowLight.enabled = true;
            glowLight.intensity = highlightedIntensity;
            glowLight.color = highlightedColor;
            return;
        }

        glowLight.enabled = baseLightEnabled;
        glowLight.intensity = baseLightIntensity;
        glowLight.color = baseLightColor;
    }

    public void ForceLightOff()
    {
        CacheLightState();

        if (glowLight != null)
        {
            glowLight.enabled = false;
        }
    }

    public Light GetLight()
    {
        CacheLightState();
        return glowLight;
    }

    private void CacheLightState()
    {
        if (glowLight == null && autoFindLightInChildren)
        {
            glowLight = GetComponentInChildren<Light>(true);
        }

        if (glowLight == null || hasCachedBaseState)
        {
            return;
        }

        baseLightEnabled = glowLight.enabled;
        baseLightIntensity = glowLight.intensity;
        baseLightColor = glowLight.color;
        hasCachedBaseState = true;
    }
}