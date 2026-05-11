using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightShadowAtlastWarningFix : MonoBehaviour
{
    public UniversalRenderPipelineAsset urpAsset;
    public int mainLightAtlasSize = 8192;
    public int additionalLightAtlasSize = 8192;

    void Start()
    {
        if (urpAsset != null)
        {
            urpAsset.mainLightShadowmapResolution = mainLightAtlasSize;
            //urpAsset.additionalLightsShadowAtlasResolution = additionalLightAtlasSize;
            Debug.Log($"Shadow atlas sizes updated: Main={mainLightAtlasSize}, Additional={additionalLightAtlasSize}");
        }
        else
        {
            Debug.LogWarning("URP Asset not assigned.");
        }
    }
}
