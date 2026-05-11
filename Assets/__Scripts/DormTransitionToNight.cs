using System.Collections.Generic;
using UnityEngine;

public class DormTransitionToNight : MonoBehaviour
{

    [SerializeField] Camera mainCamera;
    [SerializeField] List<Light> lightsToDim;
    [SerializeField] Color dimColor;
    [SerializeField] Color skyboxColor;
    [SerializeField] GameObject nightTimeDialogue;
    [SerializeField] float dialogueDuration = 3.5f;

    public void TransitionToNight()
    {
        // If there's a skybox material assigned, tint it. Otherwise fall back to camera background color.
        if (RenderSettings.skybox != null)
        {
            Material sky = RenderSettings.skybox;
            if (sky != null)
            {
                // Avoid editing the original asset: instantiate a runtime copy if we don't already have one.
                if (!sky.name.Contains("(Instance)"))
                {
                    Material runtimeSky = new Material(sky);
                    runtimeSky.name = sky.name + " (Instance)";
                    runtimeSky.hideFlags = HideFlags.DontSave;
                    RenderSettings.skybox = runtimeSky;
                    sky = runtimeSky;
                }

                // Optionally tint common sky properties (leave commented out if you don't want tinting)
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", skyboxColor);
                if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", skyboxColor);
                if (sky.HasProperty("_TopColor")) sky.SetColor("_TopColor", skyboxColor);

                // Decrease skybox exposure if the shader exposes an _Exposure property
                if (sky.HasProperty("_Exposure"))
                {
                    float cur = sky.GetFloat("_Exposure");
                    sky.SetFloat("_Exposure", Mathf.Max(0f, cur - 0.5f));
                }
            }
            DynamicGI.UpdateEnvironment();
        }
        else if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = skyboxColor;
        }
        foreach (Light light in lightsToDim)
        {
            light.color = dimColor;
        }
        //DynamicGI.UpdateEnvironment();
        Debug.Log("Transitioning to night...");

        // on next update, show the dialogue box and then hide it after a delay
        StartCoroutine(ShowNightTimeDialogue());
    }

    private System.Collections.IEnumerator ShowNightTimeDialogue()
    {
        yield return new WaitForEndOfFrame(); // wait until the end of the current frame to ensure all changes are applied
        nightTimeDialogue.SetActive(true);
        yield return new WaitForSeconds(dialogueDuration);
        nightTimeDialogue.SetActive(false);
    }
}
